using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VehicleInsuranceAPI.Data;
using VehicleInsuranceAPI.Models;

namespace VehicleInsuranceAPI.Backend.PolicyManagement
{
    [ApiController]
    [Route("api/[controller]")]
    public class PolicyManagementController : ControllerBase
    {
        private const int WaitingPaymentDays = 7;
        private const int RenewalGraceDays = 7;
        private static readonly int[] ReminderDays = { 30, 15, 7 };
        private readonly VehicleInsuranceContext _context;

        public PolicyManagementController(VehicleInsuranceContext context)
        {
            _context = context;
        }

        [HttpGet("demo")]
        public IActionResult Demo()
        {
            return Ok("Policy Management demo endpoint");
        }

        [HttpGet("insurance-types")]
        public async Task<IActionResult> GetInsuranceTypes()
        {
            var types = await _context.InsuranceTypes
                .AsNoTracking()
                .Where(t => t.IsActive == null || t.IsActive == true)
                .OrderBy(t => t.TypeName)
                .Select(t => new
                {
                    t.InsuranceTypeId,
                    t.TypeCode,
                    t.TypeName,
                    t.Description,
                    t.BaseRatePercent
                })
                .ToListAsync();

            return Ok(new { success = true, data = types });
        }

        [HttpGet]
        public async Task<IActionResult> GetPolicies(
            [FromQuery] int? customerId,
            [FromQuery] string? status,
            [FromQuery] string? q,
            [FromQuery] bool includeHidden = false)
        {
            await RunLifecycleUpdateAsync();

            var query = _context.Policies
                .AsNoTracking()
                .Include(p => p.InsuranceType)
                .AsQueryable();

            if (!includeHidden)
            {
                query = query.Where(p => p.IsHidden == null || p.IsHidden == false);
            }

            if (customerId.HasValue)
            {
                query = query.Where(p => p.CustomerId == customerId.Value);
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                var s = status.Trim().ToUpperInvariant();
                query = query.Where(p => p.Status != null && p.Status.ToUpper() == s);
            }

            if (!string.IsNullOrWhiteSpace(q))
            {
                var term = q.Trim().ToLowerInvariant();
                query = query.Where(p =>
                    p.PolicyNumber.ToString().Contains(term) ||
                    (p.CustomerNameSnapshot != null && p.CustomerNameSnapshot.ToLower().Contains(term)) ||
                    (p.VehicleNameSnapshot != null && p.VehicleNameSnapshot.ToLower().Contains(term)) ||
                    (p.VehicleNumberSnapshot != null && p.VehicleNumberSnapshot.ToLower().Contains(term)));
            }

            var policies = await query
                .OrderByDescending(p => p.CreatedAt)
                .Select(p => new
                {
                    p.PolicyId,
                    p.PolicyNumber,
                    p.CustomerId,
                    CustomerName = p.CustomerNameSnapshot,
                    CustomerPhone = p.CustomerPhoneSnapshot,
                    CustomerAddress = p.CustomerAddressSnapshot,
                    p.VehicleId,
                    VehicleName = p.VehicleNameSnapshot,
                    VehicleNumber = p.VehicleNumberSnapshot,
                    VehicleModel = p.VehicleModelSnapshot,
                    VehicleVersion = p.VehicleVersionSnapshot,
                    p.PolicyStartDate,
                    p.PolicyEndDate,
                    p.DurationMonths,
                    p.Warranty,
                    p.AddressProofPath,
                    p.PremiumAmount,
                    p.PaymentDueDate,
                    p.Status,
                    p.IsHidden,
                    p.PendingRenewalMonths,
                    p.PendingRenewalStartDate,
                    p.PendingRenewalEndDate,
                    p.CancelEffectiveDate,
                    p.CancellationReason,
                    InsuranceType = p.InsuranceType != null
                        ? new
                        {
                            p.InsuranceType.InsuranceTypeId,
                            p.InsuranceType.TypeName,
                            p.InsuranceType.TypeCode
                        }
                        : null
                })
                .ToListAsync();

            return Ok(new { success = true, data = policies });
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetPolicy(int id)
        {
            await RunLifecycleUpdateAsync();

            var policy = await _context.Policies
                .AsNoTracking()
                .Include(p => p.InsuranceType)
                .FirstOrDefaultAsync(p => p.PolicyId == id);

            if (policy == null)
            {
                return NotFound(new { success = false, message = "Policy not found" });
            }

            var bills = await _context.Bills
                .AsNoTracking()
                .Where(b => b.PolicyId == policy.PolicyId)
                .OrderByDescending(b => b.BillDate)
                .Select(b => new
                {
                    b.BillId,
                    b.BillDate,
                    b.DueDate,
                    b.BillType,
                    b.Amount,
                    b.Paid,
                    b.Status,
                    b.PaidAt
                })
                .ToListAsync();

            return Ok(new
            {
                success = true,
                data = new
                {
                    policy.PolicyId,
                    policy.PolicyNumber,
                    policy.CustomerId,
                    policy.CustomerNameSnapshot,
                    policy.CustomerAddressSnapshot,
                    policy.CustomerPhoneSnapshot,
                    policy.VehicleId,
                    policy.VehicleNumberSnapshot,
                    policy.VehicleNameSnapshot,
                    policy.VehicleModelSnapshot,
                    policy.VehicleVersionSnapshot,
                    policy.VehicleRateSnapshot,
                    policy.VehicleWarrantySnapshot,
                    policy.VehicleBodyNumberSnapshot,
                    policy.VehicleEngineNumberSnapshot,
                    policy.PolicyTypeSnapshot,
                    policy.PolicyStartDate,
                    policy.PolicyEndDate,
                    policy.DurationMonths,
                    policy.Warranty,
                    policy.AddressProofPath,
                    policy.PremiumAmount,
                    policy.PaymentDueDate,
                    policy.Status,
                    policy.PendingRenewalMonths,
                    policy.PendingRenewalStartDate,
                    policy.PendingRenewalEndDate,
                    policy.CancelEffectiveDate,
                    policy.CancellationReason,
                    PolicyPdfUrl = $"/api/PolicyManagement/{policy.PolicyId}/policy-pdf",
                    InsuranceType = policy.InsuranceType != null
                        ? new
                        {
                            policy.InsuranceType.InsuranceTypeId,
                            policy.InsuranceType.TypeName,
                            policy.InsuranceType.TypeCode
                        }
                        : null,
                    Bills = bills
                }
            });
        }

        [HttpGet("{id:int}/policy-pdf")]
        public async Task<IActionResult> DownloadPolicyPdf(int id)
        {
            var policy = await _context.Policies
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.PolicyId == id);
            if (policy == null)
            {
                return NotFound(new { success = false, message = "Policy not found" });
            }

            await EnsurePolicyDocumentAsync(policy.PolicyId, policy.PolicyNumber);

            var fileName = $"policy_{policy.PolicyNumber}.pdf";
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "uploads", "policy-documents", fileName);
            if (!System.IO.File.Exists(filePath))
            {
                return NotFound(new { success = false, message = "Policy PDF is not generated yet. Complete successful payment first." });
            }

            var bytes = await System.IO.File.ReadAllBytesAsync(filePath);
            return File(bytes, "application/pdf", fileName);
        }

        [HttpPost("from-estimate")]
        [RequestSizeLimit(6 * 1024 * 1024)]
        public async Task<IActionResult> CreateFromEstimate([FromForm] CreatePolicyFromEstimateRequest request)
        {
            if (request.EstimateId <= 0)
            {
                return BadRequest(new { success = false, message = "EstimateId is required" });
            }

            if (request.AddressProof == null)
            {
                return BadRequest(new { success = false, message = "Address proof is required" });
            }

            var estimate = await _context.Estimates.FirstOrDefaultAsync(e => e.EstimateId == request.EstimateId);
            if (estimate == null)
            {
                return BadRequest(new { success = false, message = "Estimate not found" });
            }

            if (!string.Equals(estimate.Status, "APPROVED", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(new { success = false, message = "Estimate must be approved and not yet converted to policy" });
            }

            if (estimate.ValidUntil.HasValue && estimate.ValidUntil.Value < DateTime.Now)
            {
                return BadRequest(new { success = false, message = "Estimate expired" });
            }

            if (!estimate.InsuranceTypeId.HasValue)
            {
                return BadRequest(new { success = false, message = "Estimate missing insurance type" });
            }

            var customer = await _context.Customers.AsNoTracking().FirstOrDefaultAsync(c => c.CustomerId == estimate.CustomerId);
            var vehicle = await _context.Vehicles.AsNoTracking().Include(v => v.Model).FirstOrDefaultAsync(v => v.VehicleId == estimate.VehicleId);
            var insuranceType = await _context.InsuranceTypes.AsNoTracking().FirstOrDefaultAsync(t => t.InsuranceTypeId == estimate.InsuranceTypeId.Value);
            if (customer == null || vehicle == null || insuranceType == null)
            {
                return BadRequest(new { success = false, message = "Estimate references invalid customer/vehicle/type" });
            }

            var policyStart = request.PolicyStartDate ?? DateTime.Now;
            var durationMonths = request.DurationMonths.HasValue && request.DurationMonths.Value > 0 ? request.DurationMonths.Value : 12;
            var policyEnd = policyStart.AddMonths(durationMonths).AddDays(-1);

            string? addressProofPath;
            try
            {
                addressProofPath = await SaveAddressProofAsync(request.AddressProof);
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }

            var policy = new Policy
            {
                PolicyNumber = await GeneratePolicyNumberAsync(),
                CustomerId = estimate.CustomerId,
                CustomerNameSnapshot = estimate.CustomerNameSnapshot ?? customer.CustomerName,
                CustomerAddressSnapshot = customer.Address,
                CustomerPhoneSnapshot = estimate.CustomerPhoneSnapshot ?? customer.Phone,
                VehicleId = estimate.VehicleId,
                VehicleNumberSnapshot = vehicle.VehicleNumber,
                VehicleNameSnapshot = estimate.VehicleNameSnapshot ?? vehicle.VehicleName,
                VehicleModelSnapshot = estimate.VehicleModelSnapshot ?? vehicle.Model?.ModelName,
                VehicleVersionSnapshot = vehicle.VehicleVersion,
                VehicleRateSnapshot = estimate.VehicleRate ?? vehicle.VehicleRate,
                VehicleWarrantySnapshot = estimate.Warranty,
                VehicleBodyNumberSnapshot = vehicle.BodyNumber,
                VehicleEngineNumberSnapshot = vehicle.EngineNumber,
                InsuranceTypeId = estimate.InsuranceTypeId,
                PolicyTypeSnapshot = estimate.PolicyTypeSnapshot ?? insuranceType.TypeCode,
                PolicyStartDate = DateOnly.FromDateTime(policyStart),
                PolicyEndDate = DateOnly.FromDateTime(policyEnd),
                DurationMonths = durationMonths,
                Warranty = estimate.Warranty,
                AddressProofPath = addressProofPath,
                PremiumAmount = estimate.EstimatedPremium ?? 0m,
                PaymentDueDate = DateOnly.FromDateTime(DateTime.Now.AddDays(WaitingPaymentDays)),
                Status = "WAITING_PAYMENT",
                CreatedByStaffId = await ResolveStaffId(request.StaffUserId),
                CreatedAt = DateTime.Now
            };

            _context.Policies.Add(policy);
            await _context.SaveChangesAsync();
            await CreateBillAsync(policy.PolicyId, policy.PremiumAmount ?? 0m, "INITIAL", policy.PaymentDueDate);

            estimate.Status = "CONVERTED";
            estimate.DecisionNote = string.IsNullOrWhiteSpace(estimate.DecisionNote)
                ? $"Converted to policy #{policy.PolicyNumber}"
                : $"{estimate.DecisionNote} | Converted to policy #{policy.PolicyNumber}";
            estimate.DecisionAt = DateTime.Now;
            await _context.SaveChangesAsync();

            await AddAuditLog(request.ActorUserId, "POLICY_CREATED_FROM_ESTIMATE", policy.PolicyId.ToString());

            return Ok(new
            {
                success = true,
                data = new { policy.PolicyId, policy.PolicyNumber, policy.Status, policy.PaymentDueDate }
            });
        }

        [HttpPost]
        [RequestSizeLimit(6 * 1024 * 1024)]
        public async Task<IActionResult> CreatePolicy([FromForm] CreatePolicyRequest request)
        {
            if (request.CustomerId <= 0 || request.VehicleId <= 0)
            {
                return BadRequest(new { success = false, message = "CustomerId and VehicleId are required" });
            }
            if (!request.InsuranceTypeId.HasValue || request.InsuranceTypeId.Value <= 0)
            {
                return BadRequest(new { success = false, message = "InsuranceTypeId is required" });
            }
            if (request.AddressProof == null)
            {
                return BadRequest(new { success = false, message = "Address proof is required" });
            }

            var customer = await _context.Customers.AsNoTracking().FirstOrDefaultAsync(c => c.CustomerId == request.CustomerId);
            if (customer == null) return BadRequest(new { success = false, message = "Customer not found" });

            var vehicle = await _context.Vehicles.AsNoTracking().Include(v => v.Model).FirstOrDefaultAsync(v => v.VehicleId == request.VehicleId);
            if (vehicle == null) return BadRequest(new { success = false, message = "Vehicle not found" });
            if (vehicle.CustomerId != request.CustomerId) return BadRequest(new { success = false, message = "Vehicle does not belong to customer" });

            var insuranceType = await _context.InsuranceTypes.AsNoTracking().FirstOrDefaultAsync(t => t.InsuranceTypeId == request.InsuranceTypeId.Value);
            if (insuranceType == null) return BadRequest(new { success = false, message = "Insurance type not found" });

            var policyStart = request.PolicyStartDate ?? DateTime.Now;
            var durationMonths = request.DurationMonths.HasValue && request.DurationMonths.Value > 0 ? request.DurationMonths.Value : 12;
            var policyEnd = policyStart.AddMonths(durationMonths).AddDays(-1);
            var vehicleRate = vehicle.VehicleRate ?? 0m;
            if (vehicleRate < 0m) return BadRequest(new { success = false, message = "Vehicle rate must be >= 0" });
            var premiumAmount = request.PremiumAmount ?? Math.Round(vehicleRate * (insuranceType.BaseRatePercent / 100m) * (durationMonths / 12m), 2);

            string? addressProofPath;
            try
            {
                addressProofPath = await SaveAddressProofAsync(request.AddressProof);
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }

            var policy = new Policy
            {
                PolicyNumber = await GeneratePolicyNumberAsync(),
                CustomerId = customer.CustomerId,
                CustomerNameSnapshot = customer.CustomerName,
                CustomerAddressSnapshot = customer.Address,
                CustomerPhoneSnapshot = customer.Phone,
                VehicleId = vehicle.VehicleId,
                VehicleNumberSnapshot = vehicle.VehicleNumber,
                VehicleNameSnapshot = vehicle.VehicleName,
                VehicleModelSnapshot = vehicle.Model?.ModelName,
                VehicleVersionSnapshot = vehicle.VehicleVersion,
                VehicleRateSnapshot = vehicle.VehicleRate,
                VehicleWarrantySnapshot = request.Warranty,
                VehicleBodyNumberSnapshot = vehicle.BodyNumber,
                VehicleEngineNumberSnapshot = vehicle.EngineNumber,
                InsuranceTypeId = insuranceType.InsuranceTypeId,
                PolicyTypeSnapshot = insuranceType.TypeCode,
                PolicyStartDate = DateOnly.FromDateTime(policyStart),
                PolicyEndDate = DateOnly.FromDateTime(policyEnd),
                DurationMonths = durationMonths,
                Warranty = request.Warranty,
                AddressProofPath = addressProofPath,
                PremiumAmount = premiumAmount,
                PaymentDueDate = DateOnly.FromDateTime(DateTime.Now.AddDays(WaitingPaymentDays)),
                Status = "WAITING_PAYMENT",
                CreatedByStaffId = await ResolveStaffId(request.StaffUserId),
                CreatedAt = DateTime.Now
            };

            _context.Policies.Add(policy);
            await _context.SaveChangesAsync();
            if (request.CreateBill)
            {
                await CreateBillAsync(policy.PolicyId, policy.PremiumAmount ?? 0m, "INITIAL", policy.PaymentDueDate);
            }
            await AddAuditLog(request.ActorUserId, "POLICY_CREATED", policy.PolicyId.ToString());

            return Ok(new
            {
                success = true,
                data = new { policy.PolicyId, policy.PolicyNumber, policy.Status, policy.PaymentDueDate }
            });
        }

        [HttpPatch("{id:int}/mark-paid")]
        public async Task<IActionResult> MarkPaid(int id, [FromBody] MarkPaidRequest request)
        {
            return await ApplyPaymentResult(id, new PaymentDecisionRequest
            {
                Success = true,
                ActorUserId = request.ActorUserId,
                Note = "Marked paid manually"
            });
        }

        [HttpPost("{id:int}/payment")]
        public async Task<IActionResult> ApplyPaymentResult(int id, [FromBody] PaymentDecisionRequest request)
        {
            var policy = await _context.Policies.FirstOrDefaultAsync(p => p.PolicyId == id);
            if (policy == null) return NotFound(new { success = false, message = "Policy not found" });

            var bill = await _context.Bills
                .Where(b => b.PolicyId == id && (b.Paid == null || b.Paid == false))
                .OrderBy(b => b.BillDate)
                .FirstOrDefaultAsync();

            if (request.Success)
            {
                if (bill != null)
                {
                    bill.Paid = true;
                    bill.PaidAt = DateTime.Now;
                    bill.Status = "PAID";
                }

                if (policy.PendingRenewalStartDate.HasValue && policy.PendingRenewalEndDate.HasValue)
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
                await EnsurePolicyDocumentAsync(policy.PolicyId, policy.PolicyNumber);
                await AddAuditLog(request.ActorUserId, "POLICY_PAYMENT_SUCCESS", policy.PolicyId.ToString());
                return Ok(new { success = true, message = "Payment successful. Policy is active." });
            }

            if (bill != null)
            {
                bill.Status = "UNPAID";
                bill.Paid = false;
            }
            policy.Status = "WAITING_PAYMENT";
            if (!policy.PaymentDueDate.HasValue)
            {
                policy.PaymentDueDate = DateOnly.FromDateTime(DateTime.Now.AddDays(WaitingPaymentDays));
            }

            if (policy.PaymentDueDate.HasValue && policy.PaymentDueDate.Value.ToDateTime(TimeOnly.MinValue).Date < DateTime.Now.Date)
            {
                if (policy.PendingRenewalStartDate.HasValue)
                {
                    policy.Status = "LAPSED";
                    policy.PendingRenewalMonths = null;
                    policy.PendingRenewalStartDate = null;
                    policy.PendingRenewalEndDate = null;
                }
                else
                {
                    policy.Status = "CANCELLED";
                    policy.CancellationReason = "Payment overdue";
                    policy.CancelEffectiveDate = DateOnly.FromDateTime(DateTime.Now);
                }
            }

            await _context.SaveChangesAsync();
            await AddAuditLog(request.ActorUserId, "POLICY_PAYMENT_FAILED", policy.PolicyId.ToString());
            return Ok(new { success = true, message = "Payment recorded as failed. Policy remains waiting payment." });
        }

        [HttpPost("{id:int}/renew")]
        public async Task<IActionResult> RenewPolicy(int id, [FromBody] RenewPolicyRequest request)
        {
            var policy = await _context.Policies.FirstOrDefaultAsync(p => p.PolicyId == id);
            if (policy == null) return NotFound(new { success = false, message = "Policy not found" });

            if (!string.Equals(policy.Status, "ACTIVE", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(policy.Status, "LAPSED", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(new { success = false, message = "Only ACTIVE/LAPSED policy can request renewal" });
            }

            var durationMonths = request.DurationMonths.HasValue && request.DurationMonths.Value > 0
                ? request.DurationMonths.Value
                : (policy.DurationMonths ?? 12);

            var effectiveStart = policy.PolicyEndDate.HasValue
                ? policy.PolicyEndDate.Value.ToDateTime(TimeOnly.MinValue).AddDays(1)
                : DateTime.Now;
            var effectiveEnd = effectiveStart.AddMonths(durationMonths).AddDays(-1);

            var baseDuration = policy.DurationMonths.HasValue && policy.DurationMonths.Value > 0 ? policy.DurationMonths.Value : 12;
            var basePremium = policy.PremiumAmount ?? 0m;
            var renewalAmount = Math.Round(basePremium * (durationMonths / (decimal)baseDuration), 2);

            policy.PendingRenewalMonths = durationMonths;
            policy.PendingRenewalStartDate = DateOnly.FromDateTime(effectiveStart);
            policy.PendingRenewalEndDate = DateOnly.FromDateTime(effectiveEnd);
            policy.PaymentDueDate = DateOnly.FromDateTime(DateTime.Now.AddDays(WaitingPaymentDays));
            policy.Status = "WAITING_PAYMENT";

            await _context.SaveChangesAsync();
            if (request.CreateBill)
            {
                await CreateBillAsync(policy.PolicyId, renewalAmount, "RENEWAL", policy.PaymentDueDate);
            }

            await AddAuditLog(request.ActorUserId, "POLICY_RENEWAL_REQUESTED", policy.PolicyId.ToString());
            return Ok(new
            {
                success = true,
                message = "Renewal bill created. Complete payment to activate renewal.",
                data = new { policy.PendingRenewalStartDate, policy.PendingRenewalEndDate, policy.PaymentDueDate }
            });
        }

        [HttpPatch("{id:int}/cancel")]
        public async Task<IActionResult> CancelPolicy(int id, [FromBody] CancelPolicyRequest request)
        {
            var policy = await _context.Policies.FirstOrDefaultAsync(p => p.PolicyId == id);
            if (policy == null) return NotFound(new { success = false, message = "Policy not found" });
            if (string.IsNullOrWhiteSpace(request.Reason)) return BadRequest(new { success = false, message = "Cancellation reason is required" });

            var cancelDate = DateOnly.FromDateTime(request.CancelDate ?? DateTime.Now);
            var hasBlockedClaim = await _context.Claims.AnyAsync(c =>
                c.PolicyId == id &&
                c.AccidentDate >= cancelDate &&
                (c.Status != null && (c.Status.ToUpper() == "APPROVED" || c.Status.ToUpper() == "PAID")));

            if (hasBlockedClaim)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Cannot cancel policy because approved/paid claim exists on or after cancellation date."
                });
            }

            policy.Status = "CANCELLED";
            policy.CancelEffectiveDate = cancelDate;
            policy.CancellationReason = request.Reason.Trim();
            policy.PendingRenewalMonths = null;
            policy.PendingRenewalStartDate = null;
            policy.PendingRenewalEndDate = null;

            _context.InsuranceCancellations.Add(new VehicleInsuranceAPI.Models.InsuranceCancellation
            {
                PolicyId = policy.PolicyId,
                CancelDate = cancelDate,
                RefundAmount = request.RefundAmount
            });

            await _context.SaveChangesAsync();
            await AddAuditLog(request.ActorUserId, "POLICY_CANCELLED", policy.PolicyId.ToString());
            return Ok(new { success = true, message = "Policy cancelled" });
        }

        [HttpPost("maintenance/reminders")]
        public async Task<IActionResult> GenerateRenewalReminders([FromBody] MaintenanceActorRequest? request)
        {
            var today = DateOnly.FromDateTime(DateTime.Now);
            var candidates = await _context.Policies
                .Include(p => p.Customer)
                .Where(p => p.Status != null && p.Status.ToUpper() == "ACTIVE" && p.PolicyEndDate.HasValue)
                .ToListAsync();

            var created = 0;
            foreach (var policy in candidates)
            {
                var daysLeft = policy.PolicyEndDate!.Value.DayNumber - today.DayNumber;
                if (!ReminderDays.Contains(daysLeft)) continue;

                var userId = policy.Customer?.UserId;
                if (!userId.HasValue) continue;

                var title = $"Renewal reminder ({daysLeft} days)";
                var message = $"Policy {policy.PolicyNumber} will expire on {policy.PolicyEndDate:yyyy-MM-dd}. Please renew.";
                var exists = await _context.Notifications.AnyAsync(n =>
                    n.UserId == userId &&
                    n.Title == title &&
                    EF.Functions.DateDiffDay(n.CreatedDate, DateTime.Now) == 0);
                if (exists) continue;

                _context.Notifications.Add(new Notification
                {
                    UserId = userId,
                    Title = title,
                    Message = message,
                    IsRead = false,
                    CreatedDate = DateTime.Now
                });
                created++;
            }

            await _context.SaveChangesAsync();
            await AddAuditLog(request?.ActorUserId, "POLICY_RENEWAL_REMINDER_BATCH", created.ToString());
            return Ok(new { success = true, message = "Reminder job completed", created });
        }

        [HttpPost("maintenance/lifecycle")]
        public async Task<IActionResult> RunLifecycleMaintenance([FromBody] MaintenanceActorRequest? request)
        {
            var updated = await RunLifecycleUpdateAsync();
            await AddAuditLog(request?.ActorUserId, "POLICY_LIFECYCLE_BATCH", updated.ToString());
            return Ok(new { success = true, message = "Lifecycle maintenance completed", updated });
        }

        private async Task<int> RunLifecycleUpdateAsync()
        {
            var nowDate = DateOnly.FromDateTime(DateTime.Now);
            var changed = 0;
            var policies = await _context.Policies.ToListAsync();

            foreach (var policy in policies)
            {
                var status = policy.Status?.ToUpperInvariant() ?? string.Empty;

                if (status == "WAITING_PAYMENT" && policy.PaymentDueDate.HasValue && policy.PaymentDueDate.Value < nowDate)
                {
                    if (policy.PendingRenewalStartDate.HasValue)
                    {
                        policy.Status = "LAPSED";
                        policy.PendingRenewalMonths = null;
                        policy.PendingRenewalStartDate = null;
                        policy.PendingRenewalEndDate = null;
                    }
                    else
                    {
                        policy.Status = "CANCELLED";
                        policy.CancellationReason = policy.CancellationReason ?? "Payment overdue";
                        policy.CancelEffectiveDate = policy.CancelEffectiveDate ?? nowDate;
                    }
                    changed++;
                    continue;
                }

                if (status == "ACTIVE" && policy.PolicyEndDate.HasValue)
                {
                    var lapseDate = policy.PolicyEndDate.Value.AddDays(RenewalGraceDays);
                    if (nowDate > lapseDate)
                    {
                        policy.Status = "LAPSED";
                        changed++;
                    }
                }
            }

            if (changed > 0)
            {
                await _context.SaveChangesAsync();
            }

            return changed;
        }

        private async Task CreateBillAsync(int policyId, decimal amount, string billType, DateOnly? dueDate)
        {
            _context.Bills.Add(new Bill
            {
                PolicyId = policyId,
                BillDate = DateOnly.FromDateTime(DateTime.Now),
                DueDate = dueDate,
                BillType = billType,
                Amount = amount,
                Paid = false,
                Status = "UNPAID"
            });
            await _context.SaveChangesAsync();
        }

        private async Task EnsurePolicyDocumentAsync(int policyId, long policyNumber)
        {
            try
            {
                var policy = await _context.Policies
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.PolicyId == policyId);
                if (policy == null) return;

                var docsDir = Path.Combine(Directory.GetCurrentDirectory(), "uploads", "policy-documents");
                Directory.CreateDirectory(docsDir);
                var fileName = $"policy_{policyNumber}.pdf";
                var filePath = Path.Combine(docsDir, fileName);
                var pdfBytes = BuildPolicyPdf(policy);
                await System.IO.File.WriteAllBytesAsync(filePath, pdfBytes);
            }
            catch
            {
            }
        }

        private static byte[] BuildPolicyPdf(Policy policy)
        {
            var page1 = BuildPolicyPage1(policy);
            var page2 = BuildPolicyPage2(policy);

            var objects = new List<string>();
            AddObject(objects, "<< /Type /Catalog /Pages 2 0 R >>"); // 1
            AddObject(objects, "<< /Type /Pages /Count 2 /Kids [6 0 R 8 0 R] >>"); // 2
            AddObject(objects, "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>"); // 3
            AddObject(objects, "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica-Bold >>"); // 4
            AddObject(objects, BuildStreamObject(page1)); // 5
            AddObject(objects, "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] /Resources << /Font << /F1 3 0 R /F2 4 0 R >> >> /Contents 5 0 R >>"); // 6
            AddObject(objects, BuildStreamObject(page2)); // 7
            AddObject(objects, "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] /Resources << /Font << /F1 3 0 R /F2 4 0 R >> >> /Contents 7 0 R >>"); // 8

            return BuildPdfBinary(objects);
        }

        private static string BuildPolicyPage1(Policy policy)
        {
            var policyStart = policy.PolicyStartDate?.ToString("yyyy-MM-dd") ?? "-";
            var policyEnd = policy.PolicyEndDate?.ToString("yyyy-MM-dd") ?? "-";
            var premium = policy.PremiumAmount?.ToString("N2") ?? "-";
            var issuedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            var lines = new List<string>
            {
                "q 0.07 0.49 0.93 rg 40 780 110 34 re f Q",
                "BT /F2 16 Tf 56 792 Td (AVIMS) Tj ET",
                "BT /F2 20 Tf 170 792 Td (INSURANCE POLICY CONTRACT) Tj ET",
                "BT /F1 10 Tf 170 775 Td (Issued at: " + EscapePdfText(issuedAt) + ") Tj ET",
                "q 0.85 0.88 0.92 RG 1 w 40 744 515 1 re S Q",
                "BT /F2 13 Tf 40 722 Td (1. Policy Information) Tj ET",
                "BT /F1 11 Tf 40 700 Td (Policy Number: " + EscapePdfText(policy.PolicyNumber.ToString()) + ") Tj ET",
                "BT /F1 11 Tf 40 684 Td (Policy Type: " + EscapePdfText(policy.PolicyTypeSnapshot ?? "-") + ") Tj ET",
                "BT /F1 11 Tf 40 668 Td (Status: " + EscapePdfText(policy.Status ?? "-") + ") Tj ET",
                "BT /F1 11 Tf 40 652 Td (Coverage Period: " + EscapePdfText(policyStart) + " to " + EscapePdfText(policyEnd) + ") Tj ET",
                "BT /F1 11 Tf 40 636 Td (Duration: " + EscapePdfText((policy.DurationMonths?.ToString() ?? "-")) + " month\\(s\\)) Tj ET",
                "BT /F1 11 Tf 40 620 Td (Premium Amount: " + EscapePdfText(premium) + " VND) Tj ET",
                "BT /F2 13 Tf 40 592 Td (2. Insured Party) Tj ET",
                "BT /F1 11 Tf 40 572 Td (Customer Name: " + EscapePdfText(policy.CustomerNameSnapshot ?? "-") + ") Tj ET",
                "BT /F1 11 Tf 40 556 Td (Phone: " + EscapePdfText(policy.CustomerPhoneSnapshot ?? "-") + ") Tj ET",
                "BT /F1 11 Tf 40 540 Td (Address: " + EscapePdfText(policy.CustomerAddressSnapshot ?? "-") + ") Tj ET",
                "BT /F2 13 Tf 40 512 Td (3. Vehicle Information) Tj ET",
                "BT /F1 11 Tf 40 492 Td (Vehicle Name: " + EscapePdfText(policy.VehicleNameSnapshot ?? "-") + ") Tj ET",
                "BT /F1 11 Tf 40 476 Td (Vehicle Number: " + EscapePdfText(policy.VehicleNumberSnapshot ?? "-") + ") Tj ET",
                "BT /F1 11 Tf 40 460 Td (Model / Version: " + EscapePdfText(policy.VehicleModelSnapshot ?? "-") + " / " + EscapePdfText(policy.VehicleVersionSnapshot ?? "-") + ") Tj ET",
                "BT /F1 11 Tf 40 444 Td (Body / Engine: " + EscapePdfText(policy.VehicleBodyNumberSnapshot ?? "-") + " / " + EscapePdfText(policy.VehicleEngineNumberSnapshot ?? "-") + ") Tj ET",
                "BT /F1 11 Tf 40 428 Td (Insured Value: " + EscapePdfText(policy.VehicleRateSnapshot?.ToString("N2") ?? "-") + " VND) Tj ET",
                "BT /F1 11 Tf 40 412 Td (Warranty: " + EscapePdfText(policy.VehicleWarrantySnapshot ?? policy.Warranty ?? "-") + ") Tj ET",
                "q 0.85 0.88 0.92 RG 1 w 40 386 515 1 re S Q",
                "BT /F2 12 Tf 40 365 Td (4. Coverage Note) Tj ET",
                "BT /F1 10 Tf 40 348 Td (This contract is generated electronically by AVIMS and is valid when payment is completed.) Tj ET",
                "BT /F1 10 Tf 40 334 Td (Detailed terms and obligations are listed on page 2.) Tj ET",
                "BT /F1 9 Tf 40 40 Td (Page 1/2) Tj ET"
            };

            return string.Join("\n", lines) + "\n";
        }

        private static string BuildPolicyPage2(Policy policy)
        {
            var clauses = new[]
            {
                "Clause 1: The insured must provide accurate information at all times.",
                "Clause 2: The insurer is liable only within the approved policy scope.",
                "Clause 3: Claims must be reported within the required notification period.",
                "Clause 4: Fraudulent claims lead to immediate policy termination.",
                "Clause 5: Policy renewal requires successful payment before due date.",
                "Clause 6: If payment is overdue, policy status may become Lapsed or Cancelled.",
                "Clause 7: The insurer may request additional documents for investigation.",
                "Clause 8: Vehicle modifications must be declared to remain covered.",
                "Clause 9: Exclusions include intentional damage and illegal operations.",
                "Clause 10: Disputes are resolved under applicable insurance regulations."
            };

            var content = new List<string>
            {
                "BT /F2 18 Tf 40 790 Td (TERMS, CONDITIONS AND SIGNATURE) Tj ET",
                "q 0.85 0.88 0.92 RG 1 w 40 775 515 1 re S Q",
                "BT /F2 13 Tf 40 748 Td (5. Standard Terms) Tj ET"
            };

            var y = 726;
            foreach (var clause in clauses)
            {
                content.Add($"BT /F1 10 Tf 40 {y} Td ({EscapePdfText(clause)}) Tj ET");
                y -= 18;
            }

            content.Add("BT /F2 13 Tf 40 510 Td (6. Signature) Tj ET");
            content.Add("q 0.70 0.70 0.70 RG 1 w 40 430 m 240 430 l S Q");
            content.Add("q 0.70 0.70 0.70 RG 1 w 330 430 m 530 430 l S Q");
            content.Add("BT /F1 10 Tf 40 414 Td (Insured Party Signature) Tj ET");
            content.Add("BT /F1 10 Tf 330 414 Td (AVIMS Authorized Signature) Tj ET");
            content.Add("BT /F1 10 Tf 40 390 Td (Name: " + EscapePdfText(policy.CustomerNameSnapshot ?? "-") + ") Tj ET");
            content.Add("BT /F1 10 Tf 40 372 Td (Date: " + EscapePdfText(DateTime.Now.ToString("yyyy-MM-dd")) + ") Tj ET");
            content.Add("BT /F1 9 Tf 40 40 Td (Page 2/2) Tj ET");

            return string.Join("\n", content) + "\n";
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

        private async Task<long> GeneratePolicyNumberAsync()
        {
            var rand = new Random();
            for (var i = 0; i < 5; i++)
            {
                var candidate = long.Parse($"{DateTime.Now:yyyyMMdd}{rand.Next(0, 999999):D6}");
                var exists = await _context.Policies.AnyAsync(p => p.PolicyNumber == candidate);
                if (!exists) return candidate;
            }

            return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }

        private async Task<int?> ResolveStaffId(int? staffUserId)
        {
            if (!staffUserId.HasValue) return null;
            var staff = await _context.Staff.AsNoTracking().FirstOrDefaultAsync(s => s.UserId == staffUserId.Value);
            return staff?.StaffId;
        }

        private async Task<string?> SaveAddressProofAsync(IFormFile? file)
        {
            if (file == null || file.Length == 0) return null;
            if (file.Length > 5 * 1024 * 1024) throw new InvalidOperationException("File too large. Max 5MB.");

            var allowedExtensions = new[] { ".pdf", ".jpg", ".jpeg", ".png" };
            var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(fileExtension))
            {
                throw new InvalidOperationException("Invalid file type. Only pdf/jpg/png are allowed.");
            }

            var uploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "uploads", "address-proofs");
            Directory.CreateDirectory(uploadsDir);
            var fileName = $"{DateTime.Now:yyyyMMddHHmmss}_{Guid.NewGuid().ToString("N").Substring(0, 6)}{fileExtension}";
            var filePath = Path.Combine(uploadsDir, fileName);

            await using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return $"/uploads/address-proofs/{fileName}";
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

    public class CreatePolicyFromEstimateRequest
    {
        public int EstimateId { get; set; }
        public DateTime? PolicyStartDate { get; set; }
        public int? DurationMonths { get; set; }
        public IFormFile? AddressProof { get; set; }
        public int? StaffUserId { get; set; }
        public int? ActorUserId { get; set; }
    }

    public class CreatePolicyRequest
    {
        public int CustomerId { get; set; }
        public int VehicleId { get; set; }
        public int? InsuranceTypeId { get; set; }
        public DateTime? PolicyStartDate { get; set; }
        public int? DurationMonths { get; set; }
        public string? Warranty { get; set; }
        public decimal? PremiumAmount { get; set; }
        public IFormFile? AddressProof { get; set; }
        public int? StaffUserId { get; set; }
        public bool CreateBill { get; set; } = true;
        public int? ActorUserId { get; set; }
    }

    public class MarkPaidRequest
    {
        public int? ActorUserId { get; set; }
    }

    public class PaymentDecisionRequest
    {
        public bool Success { get; set; }
        public string? Method { get; set; }
        public string? TransactionRef { get; set; }
        public string? Note { get; set; }
        public int? ActorUserId { get; set; }
    }

    public class CancelPolicyRequest
    {
        public DateTime? CancelDate { get; set; }
        public decimal? RefundAmount { get; set; }
        public string? Reason { get; set; }
        public int? ActorUserId { get; set; }
    }

    public class RenewPolicyRequest
    {
        public int? DurationMonths { get; set; }
        public bool CreateBill { get; set; } = true;
        public int? ActorUserId { get; set; }
    }

    public class MaintenanceActorRequest
    {
        public int? ActorUserId { get; set; }
    }
}

