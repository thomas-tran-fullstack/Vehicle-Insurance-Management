using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VehicleInsuranceAPI.Data;
using VehicleInsuranceAPI.Models;

namespace VehicleInsuranceAPI.Backend.InsuranceManagement
{
    [ApiController]
    [Route("api/[controller]")]
    public class InsuranceManagementController : ControllerBase
    {
        private readonly VehicleInsuranceContext _context;

        public InsuranceManagementController(VehicleInsuranceContext context)
        {
            _context = context;
        }

        [HttpGet("demo")]
        public IActionResult Demo()
        {
            return Ok("Insurance Management demo endpoint");
        }

        // GET: api/InsuranceManagement/all
        [HttpGet("all")]
        public async Task<IActionResult> GetAllInsurances([FromQuery] string? search = null, [FromQuery] string? status = null)
        {
            var query = _context.InsuranceTypes.AsNoTracking().AsQueryable();

            // Search by TypeCode or TypeName
            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                query = query.Where(i => i.TypeCode!.ToLower().Contains(s) || i.TypeName!.ToLower().Contains(s));
            }

            // Filter by Active/Inactive status
            if (!string.IsNullOrWhiteSpace(status))
            {
                var st = status.Trim().ToUpper();
                if (st == "ACTIVE")
                    query = query.Where(i => i.IsActive != false);
                else if (st == "INACTIVE")
                    query = query.Where(i => i.IsActive == false);
            }

            var insurances = await query
                .OrderByDescending(i => i.InsuranceTypeId)
                .ToListAsync();

            var result = insurances.Select(i => new
            {
                i.InsuranceTypeId,
                i.TypeCode,
                i.TypeName,
                i.Description,
                i.BaseRatePercent,
                Status = i.IsActive != false ? "ACTIVE" : "INACTIVE",
                i.IsActive,
                i.CreatedAt
            }).ToList();

            return Ok(result);
        }

        // GET: api/InsuranceManagement/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetInsuranceById(int id)
        {
            var insurance = await _context.InsuranceTypes
                .AsNoTracking()
                .FirstOrDefaultAsync(i => i.InsuranceTypeId == id);

            if (insurance == null)
            {
                return NotFound(new { success = false, message = "Insurance type not found" });
            }

            return Ok(new
            {
                success = true,
                data = new
                {
                    insurance.InsuranceTypeId,
                    insurance.TypeCode,
                    insurance.TypeName,
                    insurance.Description,
                    insurance.BaseRatePercent,
                    Status = insurance.IsActive != false ? "ACTIVE" : "INACTIVE",
                    insurance.IsActive,
                    insurance.CreatedAt
                }
            });
        }

        // POST: api/InsuranceManagement
        [HttpPost]
        public async Task<IActionResult> CreateInsurance([FromBody] CreateInsuranceRequest request)
        {
            if (string.IsNullOrWhiteSpace(request?.TypeCode) || string.IsNullOrWhiteSpace(request?.TypeName))
            {
                return BadRequest(new { success = false, message = "TypeCode and TypeName are required" });
            }

            // Check if TypeCode already exists
            var existing = await _context.InsuranceTypes
                .FirstOrDefaultAsync(i => i.TypeCode == request.TypeCode);

            if (existing != null)
            {
                return BadRequest(new { success = false, message = "Insurance Code already exists" });
            }

            var insurance = new InsuranceType
            {
                TypeCode = request.TypeCode,
                TypeName = request.TypeName,
                Description = request.Description,
                BaseRatePercent = request.BaseRatePercent ?? 2.50m,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.InsuranceTypes.Add(insurance);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                message = "Insurance type created successfully",
                data = new
                {
                    insurance.InsuranceTypeId,
                    insurance.TypeCode,
                    insurance.TypeName,
                    insurance.Description,
                    insurance.BaseRatePercent,
                    Status = "ACTIVE"
                }
            });
        }

        // PUT: api/InsuranceManagement/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateInsurance(int id, [FromBody] UpdateInsuranceRequest request)
        {
            var insurance = await _context.InsuranceTypes
                .FirstOrDefaultAsync(i => i.InsuranceTypeId == id);

            if (insurance == null)
            {
                return NotFound(new { success = false, message = "Insurance type not found" });
            }

            // Check if TypeCode is being changed and if new code already exists
            if (!string.IsNullOrWhiteSpace(request?.TypeCode) && request.TypeCode != insurance.TypeCode)
            {
                var existing = await _context.InsuranceTypes
                    .FirstOrDefaultAsync(i => i.TypeCode == request.TypeCode && i.InsuranceTypeId != id);

                if (existing != null)
                {
                    return BadRequest(new { success = false, message = "Insurance Code already exists" });
                }
            }

            // Update fields
            if (!string.IsNullOrWhiteSpace(request?.TypeCode))
                insurance.TypeCode = request.TypeCode;

            if (!string.IsNullOrWhiteSpace(request?.TypeName))
                insurance.TypeName = request.TypeName;

            if (!string.IsNullOrWhiteSpace(request?.Description))
                insurance.Description = request.Description;

            if (request?.BaseRatePercent.HasValue == true)
                insurance.BaseRatePercent = request.BaseRatePercent.Value;

            if (request?.IsActive.HasValue == true)
                insurance.IsActive = request.IsActive.Value;

            _context.InsuranceTypes.Update(insurance);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                message = "Insurance type updated successfully",
                data = new
                {
                    insurance.InsuranceTypeId,
                    insurance.TypeCode,
                    insurance.TypeName,
                    insurance.Description,
                    insurance.BaseRatePercent,
                    Status = insurance.IsActive != false ? "ACTIVE" : "INACTIVE"
                }
            });
        }

        // PUT: api/InsuranceManagement/{id}/deactivate
        [HttpPut("{id}/deactivate")]
        public async Task<IActionResult> DeactivateInsurance(int id)
        {
            var insurance = await _context.InsuranceTypes
                .FirstOrDefaultAsync(i => i.InsuranceTypeId == id);

            if (insurance == null)
            {
                return NotFound(new { success = false, message = "Insurance type not found" });
            }

            insurance.IsActive = false;
            _context.InsuranceTypes.Update(insurance);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                message = "Insurance type deactivated successfully"
            });
        }

        // DELETE: api/InsuranceManagement/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteInsurance(int id)
        {
            var insurance = await _context.InsuranceTypes
                .FirstOrDefaultAsync(i => i.InsuranceTypeId == id);

            if (insurance == null)
            {
                return NotFound(new { success = false, message = "Insurance type not found" });
            }

            _context.InsuranceTypes.Remove(insurance);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                message = "Insurance type deleted successfully"
            });
        }
    }

    public class CreateInsuranceRequest
    {
        public string? TypeCode { get; set; }
        public string? TypeName { get; set; }
        public string? Description { get; set; }
        public decimal? BaseRatePercent { get; set; }
    }

    public class UpdateInsuranceRequest
    {
        public string? TypeCode { get; set; }
        public string? TypeName { get; set; }
        public string? Description { get; set; }
        public decimal? BaseRatePercent { get; set; }
        public bool? IsActive { get; set; }
    }
}
