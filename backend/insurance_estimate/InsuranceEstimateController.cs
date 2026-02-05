using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VehicleInsuranceAPI.Data;
using VehicleInsuranceAPI.Models;

namespace VehicleInsuranceAPI.Backend.InsuranceEstimate
{
    [ApiController]
    [Route("api/[controller]")]
    public class InsuranceEstimateController : ControllerBase
    {
        private const decimal VatRate = 0.10m;
        private readonly VehicleInsuranceContext _context;

        public InsuranceEstimateController(VehicleInsuranceContext context)
        {
            _context = context;
        }

        [HttpGet("demo")]
        public IActionResult Demo()
        {
            return Ok("Insurance Estimate demo endpoint");
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

        [HttpGet("customers")]
        public async Task<IActionResult> GetCustomers()
        {
            var customers = await _context.Customers
                .AsNoTracking()
                .OrderBy(c => c.CustomerName)
                .Select(c => new
                {
                    c.CustomerId,
                    c.CustomerName,
                    c.Phone
                })
                .ToListAsync();

            return Ok(new { success = true, data = customers });
        }

        [HttpGet("vehicles")]
        public async Task<IActionResult> GetVehicles([FromQuery] int customerId)
        {
            var vehicles = await _context.Vehicles
                .AsNoTracking()
                .Include(v => v.Model)
                .Where(v => v.CustomerId == customerId)
                .Select(v => new
                {
                    v.VehicleId,
                    v.VehicleName,
                    v.VehicleNumber,
                    v.VehicleVersion,
                    v.VehicleRate,
                    v.BodyNumber,
                    v.EngineNumber,
                    VehicleModel = v.Model != null ? v.Model.ModelName : null
                })
                .ToListAsync();

            return Ok(new { success = true, data = vehicles });
        }

        [HttpGet]
        public async Task<IActionResult> GetEstimates([FromQuery] int? customerId, [FromQuery] string? status, [FromQuery] string? q)
        {
            var query = _context.Estimates.AsNoTracking().AsQueryable();

            if (customerId.HasValue)
            {
                query = query.Where(e => e.CustomerId == customerId.Value);
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                var s = status.Trim().ToUpperInvariant();
                query = query.Where(e => e.Status != null && e.Status.ToUpper() == s);
            }

            if (!string.IsNullOrWhiteSpace(q))
            {
                var term = q.Trim().ToLowerInvariant();
                query = query.Where(e =>
                    e.EstimateNumber.ToString().Contains(term) ||
                    (e.CustomerNameSnapshot != null && e.CustomerNameSnapshot.ToLower().Contains(term)) ||
                    (e.VehicleNameSnapshot != null && e.VehicleNameSnapshot.ToLower().Contains(term)));
            }

            var estimates = await query
                .OrderByDescending(e => e.CreatedAt)
                .Select(e => new
                {
                    e.EstimateId,
                    e.EstimateNumber,
                    e.CustomerId,
                    CustomerName = e.CustomerNameSnapshot,
                    CustomerPhone = e.CustomerPhoneSnapshot,
                    e.VehicleId,
                    VehicleName = e.VehicleNameSnapshot,
                    VehicleModel = e.VehicleModelSnapshot,
                    e.PolicyTypeSnapshot,
                    e.VehicleRate,
                    e.Warranty,
                    e.Status,
                    e.BasePremium,
                    e.Surcharge,
                    e.TaxAmount,
                    e.EstimatedPremium,
                    e.ValidUntil,
                    e.CreatedAt,
                    e.DecisionNote,
                    e.DecisionAt
                })
                .ToListAsync();

            return Ok(new { success = true, data = estimates });
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetEstimate(int id)
        {
            var estimate = await _context.Estimates
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.EstimateId == id);

            if (estimate == null)
            {
                return NotFound(new { success = false, message = "Estimate not found" });
            }

            return Ok(new
            {
                success = true,
                data = new
                {
                    estimate.EstimateId,
                    estimate.EstimateNumber,
                    estimate.CustomerId,
                    CustomerName = estimate.CustomerNameSnapshot,
                    CustomerPhone = estimate.CustomerPhoneSnapshot,
                    estimate.VehicleId,
                    VehicleName = estimate.VehicleNameSnapshot,
                    VehicleModel = estimate.VehicleModelSnapshot,
                    estimate.PolicyTypeSnapshot,
                    estimate.VehicleRate,
                    estimate.Warranty,
                    estimate.Status,
                    estimate.BasePremium,
                    estimate.Surcharge,
                    estimate.TaxAmount,
                    estimate.EstimatedPremium,
                    estimate.ValidUntil,
                    estimate.CreatedAt,
                    estimate.Notes,
                    estimate.DecisionNote,
                    estimate.DecisionAt
                }
            });
        }

        [HttpPost]
        public async Task<IActionResult> CreateEstimate([FromBody] CreateEstimateRequest request)
        {
            try
            {
                if (request.CustomerId <= 0 || request.VehicleId <= 0)
                {
                    return BadRequest(new { success = false, message = "CustomerId and VehicleId are required" });
                }

                if (!request.InsuranceTypeId.HasValue || request.InsuranceTypeId.Value <= 0)
                {
                    return BadRequest(new { success = false, message = "Insurance type is required" });
                }

                if (string.IsNullOrWhiteSpace(request.Warranty))
                {
                    return BadRequest(new { success = false, message = "Vehicle warranty is required" });
                }

                var durationMonths = request.DurationMonths ?? 12;
                if (durationMonths <= 0)
                {
                    return BadRequest(new { success = false, message = "DurationMonths must be greater than 0" });
                }

                var customer = await _context.Customers.AsNoTracking()
                    .FirstOrDefaultAsync(c => c.CustomerId == request.CustomerId);
                if (customer == null)
                {
                    return BadRequest(new { success = false, message = "Customer not found" });
                }

                var vehicle = await _context.Vehicles.AsNoTracking()
                    .Include(v => v.Model)
                    .FirstOrDefaultAsync(v => v.VehicleId == request.VehicleId);
                if (vehicle == null)
                {
                    return BadRequest(new { success = false, message = "Vehicle not found" });
                }

                if (vehicle.CustomerId != request.CustomerId)
                {
                    return BadRequest(new { success = false, message = "Vehicle does not belong to customer" });
                }

                var insuranceType = await _context.InsuranceTypes.AsNoTracking()
                    .FirstOrDefaultAsync(t => t.InsuranceTypeId == request.InsuranceTypeId.Value);
                if (insuranceType == null)
                {
                    return BadRequest(new { success = false, message = "Insurance type not found" });
                }

                var vehicleRate = request.VehicleRate ?? vehicle.VehicleRate ?? 0m;
                if (vehicleRate < 0)
                {
                    return BadRequest(new { success = false, message = "VehicleRate must be >= 0" });
                }

                var breakdown = CalculatePremiumBreakdown(vehicleRate, insuranceType, vehicle, durationMonths);
                var now = DateTime.Now;

                var estimate = new Estimate
                {
                    EstimateNumber = await GenerateEstimateNumberAsync(),
                    CustomerId = request.CustomerId,
                    CustomerNameSnapshot = customer.CustomerName,
                    CustomerPhoneSnapshot = customer.Phone,
                    VehicleId = request.VehicleId,
                    VehicleNameSnapshot = vehicle.VehicleName,
                    VehicleModelSnapshot = vehicle.Model != null ? vehicle.Model.ModelName : null,
                    InsuranceTypeId = request.InsuranceTypeId,
                    PolicyTypeSnapshot = insuranceType.TypeCode,
                    VehicleRate = vehicleRate,
                    BasePremium = breakdown.BasePremium,
                    Surcharge = breakdown.Surcharge,
                    TaxAmount = breakdown.TaxAmount,
                    EstimatedPremium = breakdown.TotalPremium,
                    Warranty = request.Warranty.Trim(),
                    Status = request.Submit ? "SUBMITTED" : "DRAFT",
                    ValidUntil = now.AddDays(7),
                    Notes = request.Notes,
                    CreatedByStaffId = request.CreatedByStaffId,
                    CreatedAt = now
                };

                _context.Estimates.Add(estimate);
                await _context.SaveChangesAsync();
                await AddAuditLog(request.ActorUserId, "ESTIMATE_CREATED", estimate.EstimateId.ToString());

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        estimate.EstimateId,
                        estimate.EstimateNumber,
                        estimate.Status,
                        estimate.BasePremium,
                        estimate.Surcharge,
                        estimate.TaxAmount,
                        estimate.EstimatedPremium,
                        estimate.ValidUntil
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpPatch("{id:int}/submit")]
        public async Task<IActionResult> SubmitEstimate(int id, [FromBody] SubmitEstimateRequest request)
        {
            var estimate = await _context.Estimates.FirstOrDefaultAsync(e => e.EstimateId == id);
            if (estimate == null)
            {
                return NotFound(new { success = false, message = "Estimate not found" });
            }

            if (estimate.ValidUntil.HasValue && estimate.ValidUntil.Value < DateTime.Now)
            {
                return BadRequest(new { success = false, message = "Estimate expired" });
            }

            estimate.Status = "SUBMITTED";
            await _context.SaveChangesAsync();
            await AddAuditLog(request.ActorUserId, "ESTIMATE_SUBMITTED", estimate.EstimateId.ToString());

            return Ok(new { success = true, message = "Estimate submitted" });
        }

        [HttpPatch("{id:int}/approve")]
        public async Task<IActionResult> ApproveEstimate(int id, [FromBody] DecisionRequest request)
        {
            var estimate = await _context.Estimates.FirstOrDefaultAsync(e => e.EstimateId == id);
            if (estimate == null)
            {
                return NotFound(new { success = false, message = "Estimate not found" });
            }

            if (string.IsNullOrWhiteSpace(request.Note))
            {
                return BadRequest(new { success = false, message = "Approval note is required" });
            }

            if (estimate.ValidUntil.HasValue && estimate.ValidUntil.Value < DateTime.Now)
            {
                return BadRequest(new { success = false, message = "Estimate expired" });
            }

            estimate.Status = "APPROVED";
            estimate.DecisionNote = request.Note.Trim();
            estimate.DecisionAt = DateTime.Now;
            estimate.ApprovedByStaffId = await ResolveStaffId(request.StaffUserId);

            await _context.SaveChangesAsync();
            await AddAuditLog(request.ActorUserId, "ESTIMATE_APPROVED", estimate.EstimateId.ToString());

            return Ok(new { success = true, message = "Estimate approved" });
        }

        [HttpPatch("{id:int}/reject")]
        public async Task<IActionResult> RejectEstimate(int id, [FromBody] DecisionRequest request)
        {
            var estimate = await _context.Estimates.FirstOrDefaultAsync(e => e.EstimateId == id);
            if (estimate == null)
            {
                return NotFound(new { success = false, message = "Estimate not found" });
            }

            if (string.IsNullOrWhiteSpace(request.Note))
            {
                return BadRequest(new { success = false, message = "Rejection note is required" });
            }

            estimate.Status = "REJECTED";
            estimate.DecisionNote = request.Note.Trim();
            estimate.DecisionAt = DateTime.Now;
            estimate.ApprovedByStaffId = await ResolveStaffId(request.StaffUserId);

            await _context.SaveChangesAsync();
            await AddAuditLog(request.ActorUserId, "ESTIMATE_REJECTED", estimate.EstimateId.ToString());

            return Ok(new { success = true, message = "Estimate rejected" });
        }

        private PremiumBreakdown CalculatePremiumBreakdown(
            decimal vehicleRate,
            InsuranceType insuranceType,
            Vehicle vehicle,
            int durationMonths)
        {
            var baseRate = insuranceType.BaseRatePercent <= 0m ? 2.5m : insuranceType.BaseRatePercent;
            var basePremium = Math.Round(vehicleRate * (baseRate / 100m), 2);

            var durationFactor = durationMonths / 12m;
            var typeFactor = GetTypeFactor(insuranceType.TypeCode);
            var modelFactor = GetModelFactor(vehicle.Model?.VehicleClass);

            var compositeFactor = durationFactor * typeFactor * modelFactor;
            var surcharge = Math.Round(basePremium * Math.Max(compositeFactor - 1m, 0m), 2);
            var subTotal = basePremium + surcharge;
            var taxAmount = Math.Round(subTotal * VatRate, 2);
            var total = subTotal + taxAmount;

            return new PremiumBreakdown(basePremium, surcharge, taxAmount, total);
        }

        private static decimal GetTypeFactor(string? typeCode)
        {
            if (string.IsNullOrWhiteSpace(typeCode))
            {
                return 1m;
            }

            var normalized = typeCode.Trim().ToUpperInvariant();
            if (normalized.Contains("PLUS")) return 1.20m;
            if (normalized.Contains("COMM")) return 1.30m;
            return 1.00m;
        }

        private static decimal GetModelFactor(string? vehicleClass)
        {
            if (string.IsNullOrWhiteSpace(vehicleClass))
            {
                return 1m;
            }

            var normalized = vehicleClass.Trim().ToUpperInvariant();
            if (normalized.Contains("SUV") || normalized.Contains("TRUCK")) return 1.15m;
            if (normalized.Contains("MOTORBIKE")) return 0.90m;
            return 1.00m;
        }

        private async Task<long> GenerateEstimateNumberAsync()
        {
            var rand = new Random();
            for (var i = 0; i < 5; i++)
            {
                var candidate = long.Parse($"{DateTime.Now:yyyyMMdd}{rand.Next(0, 999999):D6}");
                var exists = await _context.Estimates.AnyAsync(e => e.EstimateNumber == candidate);
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

    public class CreateEstimateRequest
    {
        public int CustomerId { get; set; }
        public int VehicleId { get; set; }
        public int? InsuranceTypeId { get; set; }
        public decimal? VehicleRate { get; set; }
        public int? DurationMonths { get; set; }
        public string? Warranty { get; set; }
        public string? Notes { get; set; }
        public int? CreatedByStaffId { get; set; }
        public bool Submit { get; set; } = true;
        public int? ActorUserId { get; set; }
    }

    public class SubmitEstimateRequest
    {
        public int? ActorUserId { get; set; }
    }

    public class DecisionRequest
    {
        public string? Note { get; set; }
        public int? StaffUserId { get; set; }
        public int? ActorUserId { get; set; }
    }

    public record PremiumBreakdown(decimal BasePremium, decimal Surcharge, decimal TaxAmount, decimal TotalPremium);
}

