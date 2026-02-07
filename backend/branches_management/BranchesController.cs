using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VehicleInsuranceAPI.Data;
using VehicleInsuranceAPI.Models;

namespace VehicleInsuranceAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BranchesController : ControllerBase
    {
        private readonly VehicleInsuranceContext _context;

        public BranchesController(VehicleInsuranceContext context)
        {
            _context = context;
        }

        // GET: api/branches
        // Lấy tất cả chi nhánh (cho customer xem hoặc admin quản lý)
        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> GetAllBranches()
        {
            try
            {
                var branches = await _context.Branches
                    .Where(b => b.IsActive)
                    .ToListAsync();

                var result = branches.Select(b => new
                {
                    b.BranchId,
                    b.BranchName,
                    b.ManagerName,
                    b.Address,
                    b.Hotline,
                    b.Email,
                    OperatingStartTime = b.OperatingStartTime.ToString("HH:mm"),
                    OperatingEndTime = b.OperatingEndTime.ToString("HH:mm"),
                    IsOnline = IsCurrentlyOpen(b.OperatingStartTime, b.OperatingEndTime)
                })
                .OrderBy(b => b.BranchName)
                .ToList();

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error loading branches: " + ex.Message });
            }
        }

        // GET: api/branches/{id}
        // Lấy chi tiết một chi nhánh
        [HttpGet("{id}")]
        public async Task<ActionResult<object>> GetBranchById(int id)
        {
            try
            {
                var branch = await _context.Branches.FindAsync(id);
                if (branch == null)
                {
                    return NotFound(new { message = "Branch not found" });
                }

                var result = new
                {
                    branch.BranchId,
                    branch.BranchName,
                    branch.ManagerName,
                    branch.Address,
                    branch.Hotline,
                    branch.Email,
                    OperatingStartTime = branch.OperatingStartTime.ToString("HH:mm"),
                    OperatingEndTime = branch.OperatingEndTime.ToString("HH:mm"),
                    IsOnline = IsCurrentlyOpen(branch.OperatingStartTime, branch.OperatingEndTime),
                    branch.IsActive,
                    branch.CreatedDate,
                    branch.UpdatedDate
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error: " + ex.Message });
            }
        }

        // POST: api/branches
        // Tạo chi nhánh mới (admin)
        [HttpPost]
        public async Task<ActionResult<Branch>> CreateBranch([FromBody] CreateBranchRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                // Validation
                if (string.IsNullOrWhiteSpace(request.BranchName) || 
                    string.IsNullOrWhiteSpace(request.ManagerName) ||
                    string.IsNullOrWhiteSpace(request.Address) ||
                    string.IsNullOrWhiteSpace(request.Hotline) ||
                    string.IsNullOrWhiteSpace(request.Email))
                {
                    return BadRequest(new { message = "All fields are required" });
                }

                // Validate email format
                if (!request.Email.Contains("@"))
                {
                    return BadRequest(new { message = "Invalid email format" });
                }

                var branch = new Branch
                {
                    BranchName = request.BranchName,
                    ManagerName = request.ManagerName,
                    Address = request.Address,
                    Hotline = request.Hotline,
                    Email = request.Email,
                    OperatingStartTime = TimeOnly.Parse(request.OperatingStartTime),
                    OperatingEndTime = TimeOnly.Parse(request.OperatingEndTime),
                    IsActive = true,
                    CreatedDate = DateTime.UtcNow
                };

                _context.Branches.Add(branch);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Branch created successfully!", branchId = branch.BranchId });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error creating branch: " + ex.Message });
            }
        }

        // PUT: api/branches/{id}
        // Cập nhật chi nhánh
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateBranch(int id, [FromBody] UpdateBranchRequest request)
        {
            try
            {
                var branch = await _context.Branches.FindAsync(id);
                if (branch == null)
                {
                    return NotFound(new { message = "Branch not found" });
                }

                if (!string.IsNullOrWhiteSpace(request.BranchName))
                    branch.BranchName = request.BranchName;

                if (!string.IsNullOrWhiteSpace(request.ManagerName))
                    branch.ManagerName = request.ManagerName;

                if (!string.IsNullOrWhiteSpace(request.Address))
                    branch.Address = request.Address;

                if (!string.IsNullOrWhiteSpace(request.Hotline))
                    branch.Hotline = request.Hotline;

                if (!string.IsNullOrWhiteSpace(request.Email))
                    branch.Email = request.Email;

                if (!string.IsNullOrWhiteSpace(request.OperatingStartTime))
                    branch.OperatingStartTime = TimeOnly.Parse(request.OperatingStartTime);

                if (!string.IsNullOrWhiteSpace(request.OperatingEndTime))
                    branch.OperatingEndTime = TimeOnly.Parse(request.OperatingEndTime);

                branch.UpdatedDate = DateTime.UtcNow;

                _context.Branches.Update(branch);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Branch updated successfully!" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error updating branch: " + ex.Message });
            }
        }

        // DELETE: api/branches/{id}
        // Xóa chi nhánh (soft delete)
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteBranch(int id)
        {
            try
            {
                var branch = await _context.Branches.FindAsync(id);
                if (branch == null)
                {
                    return NotFound(new { message = "Branch not found" });
                }

                branch.IsActive = false;
                branch.UpdatedDate = DateTime.UtcNow;

                _context.Branches.Update(branch);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Branch deleted successfully!" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error deleting branch: " + ex.Message });
            }
        }

        // Helper method: Check if branch is currently open
        private bool IsCurrentlyOpen(TimeOnly startTime, TimeOnly endTime)
        {
            var now = TimeOnly.FromDateTime(DateTime.Now);
            
            // Nếu giờ mở và giờ đóng cùng ngày
            if (startTime < endTime)
            {
                return now >= startTime && now < endTime;
            }
            else
            {
                // Nếu chi nhánh mở qua đêm (ví dụ 22:00 - 06:00)
                return now >= startTime || now < endTime;
            }
        }
    }

    // Request DTOs
    public class CreateBranchRequest
    {
        public string BranchName { get; set; } = null!;
        public string ManagerName { get; set; } = null!;
        public string Address { get; set; } = null!;
        public string Hotline { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string OperatingStartTime { get; set; } = null!; // Format: "HH:mm"
        public string OperatingEndTime { get; set; } = null!;   // Format: "HH:mm"
    }

    public class UpdateBranchRequest
    {
        public string? BranchName { get; set; }
        public string? ManagerName { get; set; }
        public string? Address { get; set; }
        public string? Hotline { get; set; }
        public string? Email { get; set; }
        public string? OperatingStartTime { get; set; }
        public string? OperatingEndTime { get; set; }
    }
}
