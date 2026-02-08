using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VehicleInsuranceAPI.Data;
using VehicleInsuranceAPI.Models;

namespace VehicleInsuranceAPI.Backend.BillingPayment
{
    [ApiController]
    [Route("api/[controller]")]
    public class BillingPaymentController : ControllerBase
    {
        private readonly VehicleInsuranceContext _context;

        public BillingPaymentController(VehicleInsuranceContext context)
        {
            _context = context;
        }

        [HttpGet("demo")]
        public IActionResult Demo()
        {
            return Ok("Billing & Payment demo endpoint");
        }

        [HttpGet]
        public async Task<IActionResult> GetBills(
            [FromQuery] int? customerId,
            [FromQuery] int? policyId,
            [FromQuery] string? status,
            [FromQuery] string? q)
        {
            var query = _context.Bills
                .AsNoTracking()
                .Include(b => b.Policy)
                .AsQueryable();

            if (policyId.HasValue)
            {
                query = query.Where(b => b.PolicyId == policyId.Value);
            }

            if (customerId.HasValue)
            {
                query = query.Where(b =>
                    b.CustomerId == customerId.Value ||
                    (b.Policy != null && b.Policy.CustomerId == customerId.Value));
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                var s = status.Trim().ToUpperInvariant();
                query = query.Where(b => b.Status != null && b.Status.ToUpper() == s);
            }

            if (!string.IsNullOrWhiteSpace(q))
            {
                var term = q.Trim().ToLowerInvariant();
                query = query.Where(b =>
                    b.BillId.ToString().Contains(term) ||
                    (b.PolicyNumberSnapshot != null && b.PolicyNumberSnapshot.ToString()!.Contains(term)) ||
                    (b.CustomerNameSnapshot != null && b.CustomerNameSnapshot.ToLower().Contains(term)) ||
                    (b.VehicleNameSnapshot != null && b.VehicleNameSnapshot.ToLower().Contains(term)) ||
                    (b.Policy != null && b.Policy.PolicyNumber.ToString().Contains(term)));
            }

            var bills = await query
                .OrderByDescending(b => b.BillDate)
                .Select(b => new
                {
                    b.BillId,
                    b.PolicyId,
                    b.CustomerId,
                    b.BillDate,
                    b.DueDate,
                    b.BillType,
                    b.Amount,
                    b.Paid,
                    b.Status,
                    b.PaidAt,
                    PolicyNumber = b.PolicyNumberSnapshot ?? (b.Policy != null ? b.Policy.PolicyNumber : (long?)null),
                    CustomerName = b.CustomerNameSnapshot ?? (b.Policy != null ? b.Policy.CustomerNameSnapshot : null),
                    CustomerPhone = b.CustomerPhoneSnapshot ?? (b.Policy != null ? b.Policy.CustomerPhoneSnapshot : null),
                    CustomerAddressProof = b.CustomerAddressProofSnapshot ?? (b.Policy != null ? b.Policy.AddressProofPath : null),
                    VehicleName = b.VehicleNameSnapshot ?? (b.Policy != null ? b.Policy.VehicleNameSnapshot : null),
                    VehicleModel = b.VehicleModelSnapshot ?? (b.Policy != null ? b.Policy.VehicleModelSnapshot : null),
                    VehicleRate = b.VehicleRateSnapshot ?? (b.Policy != null ? b.Policy.VehicleRateSnapshot : null),
                    VehicleBodyNumber = b.VehicleBodyNumberSnapshot ?? (b.Policy != null ? b.Policy.VehicleBodyNumberSnapshot : null),
                    VehicleEngineNumber = b.VehicleEngineNumberSnapshot ?? (b.Policy != null ? b.Policy.VehicleEngineNumberSnapshot : null),
                    AmountPaid = _context.BillPayments
                        .Where(p => p.BillId == b.BillId && p.Status == "SUCCESS")
                        .Sum(p => (decimal?)p.Amount) ?? 0m,
                    LastPaymentMethod = _context.BillPayments
                        .Where(p => p.BillId == b.BillId && p.Status == "SUCCESS")
                        .OrderByDescending(p => p.ProcessedAt ?? p.UpdatedAt ?? p.CreatedAt)
                        .Select(p => p.Method)
                        .FirstOrDefault()
                })
                .ToListAsync();

            var data = bills.Select(b => new
            {
                b.BillId,
                b.PolicyId,
                b.CustomerId,
                b.BillDate,
                b.DueDate,
                b.BillType,
                b.Amount,
                b.Paid,
                b.Status,
                b.PaidAt,
                b.PolicyNumber,
                b.CustomerName,
                b.CustomerPhone,
                b.CustomerAddressProof,
                b.VehicleName,
                b.VehicleModel,
                b.VehicleRate,
                b.VehicleBodyNumber,
                b.VehicleEngineNumber,
                b.AmountPaid,
                RemainingAmount = Math.Max(0m, (b.Amount ?? 0m) - b.AmountPaid),
                b.LastPaymentMethod
            });

            return Ok(new { success = true, data });
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetBill(int id)
        {
            var bill = await _context.Bills
                .AsNoTracking()
                .Include(b => b.Policy)
                .FirstOrDefaultAsync(b => b.BillId == id);

            if (bill == null)
            {
                return NotFound(new { success = false, message = "Bill not found" });
            }

            var payments = await _context.BillPayments
                .AsNoTracking()
                .Where(p => p.BillId == id)
                .OrderByDescending(p => p.CreatedAt)
                .Select(p => new
                {
                    p.BillPaymentId,
                    p.Amount,
                    p.Method,
                    p.Status,
                    p.TransactionRef,
                    p.Note,
                    p.CreatedAt,
                    p.UpdatedAt,
                    p.ProcessedAt
                })
                .ToListAsync();

            var paidTotal = payments
                .Where(p => string.Equals(p.Status, "SUCCESS", StringComparison.OrdinalIgnoreCase))
                .Sum(p => p.Amount);

            var amount = bill.Amount ?? 0m;

            return Ok(new
            {
                success = true,
                data = new
                {
                    bill.BillId,
                    bill.PolicyId,
                    bill.CustomerId,
                    bill.BillDate,
                    bill.DueDate,
                    bill.BillType,
                    bill.Amount,
                    bill.Paid,
                    bill.Status,
                    bill.PaidAt,
                    PolicyNumber = bill.PolicyNumberSnapshot ?? bill.Policy?.PolicyNumber,
                    CustomerName = bill.CustomerNameSnapshot ?? bill.Policy?.CustomerNameSnapshot,
                    CustomerPhone = bill.CustomerPhoneSnapshot ?? bill.Policy?.CustomerPhoneSnapshot,
                    CustomerAddressProof = bill.CustomerAddressProofSnapshot ?? bill.Policy?.AddressProofPath,
                    VehicleName = bill.VehicleNameSnapshot ?? bill.Policy?.VehicleNameSnapshot,
                    VehicleModel = bill.VehicleModelSnapshot ?? bill.Policy?.VehicleModelSnapshot,
                    VehicleRate = bill.VehicleRateSnapshot ?? bill.Policy?.VehicleRateSnapshot,
                    VehicleBodyNumber = bill.VehicleBodyNumberSnapshot ?? bill.Policy?.VehicleBodyNumberSnapshot,
                    VehicleEngineNumber = bill.VehicleEngineNumberSnapshot ?? bill.Policy?.VehicleEngineNumberSnapshot,
                    AmountPaid = paidTotal,
                    RemainingAmount = Math.Max(0m, amount - paidTotal),
                    Payments = payments
                }
            });
        }

        [HttpPost("{id:int}/payments")]
        public async Task<IActionResult> CreatePayment(int id, [FromBody] CreateBillPaymentRequest request)
        {
            var bill = await _context.Bills.FirstOrDefaultAsync(b => b.BillId == id);
            if (bill == null)
            {
                return NotFound(new { success = false, message = "Bill not found" });
            }

            if (bill.Paid == true || string.Equals(bill.Status, "PAID", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(new { success = false, message = "Bill is already paid" });
            }

            var paidTotal = await GetPaidAmountAsync(bill.BillId);
            var amount = bill.Amount ?? 0m;
            var remaining = Math.Max(0m, amount - paidTotal);

            if (remaining <= 0m)
            {
                return BadRequest(new { success = false, message = "No remaining amount for this bill" });
            }

            var paymentAmount = request.Amount ?? remaining;
            if (paymentAmount <= 0m)
            {
                return BadRequest(new { success = false, message = "Amount must be greater than 0" });
            }

            if (paymentAmount > remaining)
            {
                return BadRequest(new { success = false, message = "Amount exceeds remaining balance" });
            }

            var payment = new BillPayment
            {
                BillId = bill.BillId,
                Amount = paymentAmount,
                Method = string.IsNullOrWhiteSpace(request.Method) ? "ONLINE" : request.Method.Trim().ToUpperInvariant(),
                Status = "PENDING",
                TransactionRef = string.IsNullOrWhiteSpace(request.TransactionRef)
                    ? $"BILL-{bill.BillId}-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}"
                    : request.TransactionRef.Trim(),
                Note = request.Note,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            _context.BillPayments.Add(payment);
            await _context.SaveChangesAsync();
            await AddAuditLog(request.ActorUserId, "BILL_PAYMENT_CREATED", $"{bill.BillId}:{payment.BillPaymentId}:{payment.TransactionRef}");

            return Ok(new
            {
                success = true,
                data = new
                {
                    payment.BillPaymentId,
                    payment.Status,
                    payment.TransactionRef,
                    payment.Amount,
                    payment.Method
                }
            });
        }

        [HttpPost("{id:int}/payments/{paymentId:int}/confirm")]
        public async Task<IActionResult> ConfirmPayment(int id, int paymentId, [FromBody] ConfirmBillPaymentRequest request)
        {
            var bill = await _context.Bills.FirstOrDefaultAsync(b => b.BillId == id);
            if (bill == null)
            {
                return NotFound(new { success = false, message = "Bill not found" });
            }

            var payment = await _context.BillPayments.FirstOrDefaultAsync(p => p.BillPaymentId == paymentId && p.BillId == id);
            if (payment == null)
            {
                return NotFound(new { success = false, message = "Payment not found" });
            }

            if (string.Equals(payment.Status, "SUCCESS", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(new { success = false, message = "Payment already marked as success" });
            }

            if (string.Equals(payment.Status, "FAILED", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(new { success = false, message = "Payment already marked as failed" });
            }

            payment.Status = request.Success ? "SUCCESS" : "FAILED";
            payment.Note = request.Note ?? payment.Note;
            payment.UpdatedAt = DateTime.Now;
            payment.ProcessedAt = DateTime.Now;

            await _context.SaveChangesAsync();
            await UpdateBillStatusAsync(bill);
            await _context.SaveChangesAsync();

            await AddAuditLog(request.ActorUserId, "BILL_PAYMENT_UPDATED", $"{bill.BillId}:{payment.BillPaymentId}:{payment.TransactionRef}:{payment.Status}");

            if (request.Success && string.Equals(bill.Status, "PAID", StringComparison.OrdinalIgnoreCase))
            {
                await ActivatePolicyAfterBillPaidAsync(bill, request.ActorUserId);
            }

            return Ok(new { success = true, message = request.Success ? "Payment confirmed" : "Payment failed" });
        }

        [HttpPost("{id:int}/mark-paid")]
        public async Task<IActionResult> MarkPaid(int id, [FromBody] MarkBillPaidRequest request)
        {
            var bill = await _context.Bills.FirstOrDefaultAsync(b => b.BillId == id);
            if (bill == null)
            {
                return NotFound(new { success = false, message = "Bill not found" });
            }

            if (bill.Paid == true || string.Equals(bill.Status, "PAID", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(new { success = false, message = "Bill is already paid" });
            }

            var paidTotal = await GetPaidAmountAsync(bill.BillId);
            var amount = bill.Amount ?? 0m;
            var remaining = Math.Max(0m, amount - paidTotal);
            if (remaining <= 0m)
            {
                return BadRequest(new { success = false, message = "No remaining amount for this bill" });
            }

            var paymentAmount = request.Amount ?? remaining;
            if (paymentAmount <= 0m || paymentAmount > remaining)
            {
                return BadRequest(new { success = false, message = "Invalid amount for manual payment" });
            }

            var payment = new BillPayment
            {
                BillId = bill.BillId,
                Amount = paymentAmount,
                Method = string.IsNullOrWhiteSpace(request.Method) ? "MANUAL" : request.Method.Trim().ToUpperInvariant(),
                Status = "SUCCESS",
                TransactionRef = string.IsNullOrWhiteSpace(request.TransactionRef)
                    ? $"MANUAL-{bill.BillId}-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}"
                    : request.TransactionRef.Trim(),
                Note = request.Note,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now,
                ProcessedAt = DateTime.Now
            };

            _context.BillPayments.Add(payment);
            await _context.SaveChangesAsync();
            await UpdateBillStatusAsync(bill);
            await _context.SaveChangesAsync();
            await AddAuditLog(request.ActorUserId, "BILL_PAYMENT_MANUAL", $"{bill.BillId}:{payment.BillPaymentId}:{payment.TransactionRef}:SUCCESS");

            if (string.Equals(bill.Status, "PAID", StringComparison.OrdinalIgnoreCase))
            {
                await ActivatePolicyAfterBillPaidAsync(bill, request.ActorUserId);
            }

            return Ok(new { success = true, message = "Manual payment recorded" });
        }

        [HttpGet("{id:int}/invoice-pdf")]
        public async Task<IActionResult> DownloadInvoicePdf(int id)
        {
            var bill = await _context.Bills
                .AsNoTracking()
                .Include(b => b.Policy)
                .FirstOrDefaultAsync(b => b.BillId == id);
            if (bill == null)
            {
                return NotFound(new { success = false, message = "Bill not found" });
            }

            if (!string.Equals(bill.Status, "PAID", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(new { success = false, message = "Invoice PDF is available only after payment" });
            }

            var paidTotal = await GetPaidAmountAsync(bill.BillId);
            await EnsureInvoiceDocumentAsync(bill, paidTotal);

            var fileName = $"bill_{bill.BillId}.pdf";
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "uploads", "billing-documents", fileName);
            if (!System.IO.File.Exists(filePath))
            {
                return NotFound(new { success = false, message = "Invoice PDF not found" });
            }

            var bytes = await System.IO.File.ReadAllBytesAsync(filePath);
            return File(bytes, "application/pdf", fileName);
        }

        private async Task<decimal> GetPaidAmountAsync(int billId)
        {
            return await _context.BillPayments
                .Where(p => p.BillId == billId && p.Status == "SUCCESS")
                .SumAsync(p => (decimal?)p.Amount) ?? 0m;
        }

        private async Task UpdateBillStatusAsync(Bill bill)
        {
            var totalPaid = await GetPaidAmountAsync(bill.BillId);
            var amount = bill.Amount ?? 0m;

            if (amount <= 0m)
            {
                bill.Status = "PAID";
                bill.Paid = true;
                bill.PaidAt = bill.PaidAt ?? DateTime.Now;
                return;
            }

            if (totalPaid >= amount)
            {
                bill.Status = "PAID";
                bill.Paid = true;
                bill.PaidAt = DateTime.Now;
            }
            else if (totalPaid > 0m)
            {
                bill.Status = "PARTIALLY_PAID";
                bill.Paid = false;
                bill.PaidAt = null;
            }
            else
            {
                bill.Status = "UNPAID";
                bill.Paid = false;
                bill.PaidAt = null;
            }
        }

        private async Task ActivatePolicyAfterBillPaidAsync(Bill bill, int? actorUserId)
        {
            if (!bill.PolicyId.HasValue)
            {
                return;
            }

            var policy = await _context.Policies.FirstOrDefaultAsync(p => p.PolicyId == bill.PolicyId.Value);
            if (policy == null)
            {
                return;
            }

            var status = policy.Status?.ToUpperInvariant() ?? string.Empty;
            var hasPendingRenewal = policy.PendingRenewalStartDate.HasValue && policy.PendingRenewalEndDate.HasValue;

            if (!hasPendingRenewal && status != "WAITING_PAYMENT")
            {
                return;
            }

            if (hasPendingRenewal)
            {
                policy.PolicyStartDate = policy.PendingRenewalStartDate;
                policy.PolicyEndDate = policy.PendingRenewalEndDate;
                policy.DurationMonths = policy.PendingRenewalMonths;
                policy.PendingRenewalStartDate = null;
                policy.PendingRenewalEndDate = null;
                policy.PendingRenewalMonths = null;
            }

            policy.Status = "ACTIVE";
            policy.PaymentDueDate = null;
            await _context.SaveChangesAsync();

            await AddAuditLog(actorUserId, "POLICY_PAYMENT_SUCCESS", policy.PolicyId.ToString());
        }

        private async Task EnsureInvoiceDocumentAsync(Bill bill, decimal paidTotal)
        {
            try
            {
                var docsDir = Path.Combine(Directory.GetCurrentDirectory(), "uploads", "billing-documents");
                Directory.CreateDirectory(docsDir);
                var fileName = $"bill_{bill.BillId}.pdf";
                var filePath = Path.Combine(docsDir, fileName);
                if (System.IO.File.Exists(filePath))
                {
                    return;
                }

                var pdfBytes = BuildInvoicePdf(bill, paidTotal);
                await System.IO.File.WriteAllBytesAsync(filePath, pdfBytes);
            }
            catch
            {
            }
        }

        private static byte[] BuildInvoicePdf(Bill bill, decimal paidTotal)
        {
            var issueDate = bill.BillDate?.ToString("yyyy-MM-dd") ?? "-";
            var dueDate = bill.DueDate?.ToString("yyyy-MM-dd") ?? "-";
            var amount = bill.Amount?.ToString("N2") ?? "-";
            var paid = paidTotal.ToString("N2");
            var customerName = bill.CustomerNameSnapshot ?? "-";
            var policyNo = bill.PolicyNumberSnapshot?.ToString() ?? "-";
            var vehicleName = bill.VehicleNameSnapshot ?? "-";
            var vehicleModel = bill.VehicleModelSnapshot ?? "-";
            var vehicleRate = bill.VehicleRateSnapshot?.ToString("N2") ?? "-";

            var lines = new List<string>
            {
                "BT /F2 18 Tf 40 790 Td (AVIMS BILLING INVOICE) Tj ET",
                "q 0.85 0.88 0.92 RG 1 w 40 775 515 1 re S Q",
                "BT /F1 11 Tf 40 748 Td (Bill ID: " + EscapePdfText(bill.BillId.ToString()) + ") Tj ET",
                "BT /F1 11 Tf 40 732 Td (Policy Number: " + EscapePdfText(policyNo) + ") Tj ET",
                "BT /F1 11 Tf 40 716 Td (Issue Date: " + EscapePdfText(issueDate) + ") Tj ET",
                "BT /F1 11 Tf 40 700 Td (Due Date: " + EscapePdfText(dueDate) + ") Tj ET",
                "BT /F1 11 Tf 40 684 Td (Customer: " + EscapePdfText(customerName) + ") Tj ET",
                "BT /F1 11 Tf 40 668 Td (Vehicle: " + EscapePdfText(vehicleName) + " / " + EscapePdfText(vehicleModel) + ") Tj ET",
                "BT /F1 11 Tf 40 652 Td (Vehicle Rate: " + EscapePdfText(vehicleRate) + ") Tj ET",
                "q 0.85 0.88 0.92 RG 1 w 40 630 515 1 re S Q",
                "BT /F2 13 Tf 40 608 Td (Billing Summary) Tj ET",
                "BT /F1 11 Tf 40 588 Td (Amount Due: " + EscapePdfText(amount) + ") Tj ET",
                "BT /F1 11 Tf 40 572 Td (Paid Total: " + EscapePdfText(paid) + ") Tj ET",
                "BT /F1 10 Tf 40 80 Td (Thank you for your payment. This invoice is issued electronically.) Tj ET"
            };

            var objects = new List<string>();
            AddObject(objects, "<< /Type /Catalog /Pages 2 0 R >>");
            AddObject(objects, "<< /Type /Pages /Count 1 /Kids [4 0 R] >>");
            AddObject(objects, "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>");
            AddObject(objects, "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] /Resources << /Font << /F1 3 0 R /F2 3 0 R >> >> /Contents 5 0 R >>");
            AddObject(objects, BuildStreamObject(string.Join("\n", lines) + "\n"));
            return BuildPdfBinary(objects);
        }

        private static int AddObject(List<string> objects, string content)
        {
            objects.Add(content);
            return objects.Count;
        }

        private static string BuildStreamObject(string content)
        {
            var bytes = System.Text.Encoding.ASCII.GetByteCount(content);
            return $"<< /Length {bytes} >>\nstream\n{content}endstream";
        }

        private static byte[] BuildPdfBinary(List<string> objects)
        {
            var sb = new System.Text.StringBuilder();
            sb.Append("%PDF-1.4\n");

            var offsets = new List<int> { 0 };
            for (var i = 0; i < objects.Count; i++)
            {
                offsets.Add(System.Text.Encoding.ASCII.GetByteCount(sb.ToString()));
                sb.Append($"{i + 1} 0 obj\n{objects[i]}\nendobj\n");
            }

            var xrefOffset = System.Text.Encoding.ASCII.GetByteCount(sb.ToString());
            sb.Append($"xref\n0 {objects.Count + 1}\n");
            sb.Append("0000000000 65535 f \n");
            for (var i = 1; i <= objects.Count; i++)
            {
                sb.Append($"{offsets[i].ToString("D10")} 00000 n \n");
            }

            sb.Append($"trailer\n<< /Size {objects.Count + 1} /Root 1 0 R >>\n");
            sb.Append($"startxref\n{xrefOffset}\n%%EOF");
            return System.Text.Encoding.ASCII.GetBytes(sb.ToString());
        }

        private static string EscapePdfText(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return "-";
            }

            var sanitized = new string(value.Select(ch => ch <= 126 ? ch : '?').ToArray());
            return sanitized.Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)");
        }

        private async Task AddAuditLog(int? userId, string action, string? entityId)
        {
            if (!userId.HasValue) return;

            _context.AuditLogs.Add(new AuditLog
            {
                UserId = userId,
                Action = $"{action}:{entityId}",
                LogDate = DateTime.Now
            });
            await _context.SaveChangesAsync();
        }
    }

    public class CreateBillPaymentRequest
    {
        public decimal? Amount { get; set; }
        public string? Method { get; set; }
        public string? TransactionRef { get; set; }
        public string? Note { get; set; }
        public int? ActorUserId { get; set; }
    }

    public class ConfirmBillPaymentRequest
    {
        public bool Success { get; set; }
        public string? Note { get; set; }
        public int? ActorUserId { get; set; }
    }

    public class MarkBillPaidRequest
    {
        public decimal? Amount { get; set; }
        public string? Method { get; set; }
        public string? TransactionRef { get; set; }
        public string? Note { get; set; }
        public int? ActorUserId { get; set; }
    }
}
