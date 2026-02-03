using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VehicleInsuranceAPI.Data;
using VehicleInsuranceAPI.Models;

namespace VehicleInsuranceAPI.Backend.AgentStaffManagement
{
    [ApiController]
    [Route("api/[controller]")]
    public class AgentStaffManagementController : ControllerBase
    {
        private readonly VehicleInsuranceContext _context;

        public AgentStaffManagementController(VehicleInsuranceContext context)
        {
            _context = context;
        }

        [HttpGet("demo")]
        public IActionResult Demo()
        {
            return Ok("Agent/Staff Management demo endpoint");
        }

        // GET: api/AgentStaffManagement/staff
        [HttpGet("staff")]
        public async Task<IActionResult> GetAllStaff([FromQuery] string? search = null, [FromQuery] string? status = null)
        {
            var q = _context.Staff.AsNoTracking().Include(s => s.User).ThenInclude(u => u.Role).AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                q = q.Where(st => st.FullName!.ToLower().Contains(s) || st.Phone!.Contains(s));
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                var st = status.Trim().ToUpper();
                if (st == "ACTIVE")
                    q = q.Where(s => s.IsActive == true && s.User.Status == "ACTIVE");
                else if (st == "INACTIVE")
                    q = q.Where(s => s.IsActive == false || s.User.Status == "INACTIVE");
            }

            var staff = await q.OrderByDescending(s => s.StaffId)
                .Select(s => new
                {
                    s.StaffId,
                    s.UserId,
                    s.FullName,
                    s.Phone,
                    s.Position,
                    s.Avatar,
                    s.IsActive,
                    User = new
                    {
                        s.User.UserId,
                        s.User.Username,
                        s.User.Email,
                        s.User.RoleId,
                        RoleName = s.User.Role.RoleName,
                        s.User.Status,
                        s.User.BannedUntil,
                        s.User.CreatedAt
                    }
                })
                .ToListAsync();

            return Ok(staff);
        }

        // GET: api/AgentStaffManagement/staff/5
        [HttpGet("staff/{id:int}")]
        public async Task<IActionResult> GetStaff(int id)
        {
            var staff = await _context.Staff
                .AsNoTracking()
                .Include(s => s.User).ThenInclude(u => u.Role)
                .FirstOrDefaultAsync(s => s.StaffId == id);

            if (staff == null) return NotFound();

            return Ok(new
            {
                staff.StaffId,
                staff.UserId,
                staff.FullName,
                staff.Phone,
                staff.Position,
                staff.Avatar,
                staff.IsActive,
                User = new
                {
                    staff.User.UserId,
                    staff.User.Username,
                    staff.User.Email,
                    staff.User.RoleId,
                    RoleName = staff.User.Role.RoleName,
                    staff.User.IsLocked,
                    staff.User.Status,
                    staff.User.BannedUntil,
                    staff.User.CreatedAt
                }
            });
        }

        public class CreateStaffRequest
        {
            public string FullName { get; set; } = string.Empty;
            public string? Phone { get; set; }
            public string? Email { get; set; }
            public string? Position { get; set; }
            public string? Username { get; set; }
            public string? Password { get; set; }
        }

        // POST: api/AgentStaffManagement/staff
        [HttpPost("staff")]
        public async Task<IActionResult> CreateStaff([FromBody] CreateStaffRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.FullName)) 
                return BadRequest(new { message = "Full Name is required" });

            // Generate random username if not provided
            var username = req.Username ?? GenerateRandomUsername();
            var password = req.Password ?? GenerateRandomPassword();

            var userExists = await _context.Users.AnyAsync(u => u.Username.ToLower() == username.ToLower());
            if (userExists) return BadRequest(new { message = "Username already exists" });

            // RoleId 3 = STAFF
            var user = new User
            {
                Username = username,
                PasswordHash = VehicleInsuranceAPI.Backend.LoginUserManagement.PasswordHashService.HashPassword(password),
                Email = string.IsNullOrWhiteSpace(req.Email) ? null : req.Email.Trim(),
                RoleId = 3,
                IsLocked = false,
                Status = "ACTIVE",
                CreatedAt = DateTime.Now
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var staff = new Staff
            {
                UserId = user.UserId,
                FullName = req.FullName.Trim(),
                Phone = string.IsNullOrWhiteSpace(req.Phone) ? null : req.Phone.Trim(),
                Position = string.IsNullOrWhiteSpace(req.Position) ? null : req.Position.Trim(),
                Avatar = "/images/default-avatar.png",
                IsActive = true
            };

            _context.Staff.Add(staff);
            await _context.SaveChangesAsync();

            return Ok(new { 
                success = true, 
                staffId = staff.StaffId,
                userId = user.UserId,
                generatedUsername = username,
                generatedPassword = password
            });
        }

        public class UpdateStaffRequest
        {
            public string? FullName { get; set; }
            public string? Phone { get; set; }
            public string? Email { get; set; }
            public string? Position { get; set; }
            public string? Username { get; set; }
            public string? Password { get; set; }
            public bool? IsActive { get; set; }
            public string? Status { get; set; }
            public DateTime? BannedUntil { get; set; }
        }

        // PUT: api/AgentStaffManagement/staff/5
        [HttpPut("staff/{id:int}")]
        public async Task<IActionResult> UpdateStaff(int id, [FromBody] UpdateStaffRequest req)
        {
            var staff = await _context.Staff
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.StaffId == id);

            if (staff == null) return NotFound();

            var user = staff.User;

            // Update staff profile
            if (!string.IsNullOrWhiteSpace(req.FullName))
                staff.FullName = req.FullName.Trim();

            if (req.Phone != null)
                staff.Phone = string.IsNullOrWhiteSpace(req.Phone) ? null : req.Phone.Trim();

            if (req.Position != null)
                staff.Position = string.IsNullOrWhiteSpace(req.Position) ? null : req.Position.Trim();

            if (req.IsActive.HasValue)
            {
                staff.IsActive = req.IsActive.Value;
                // When reactivating staff (IsActive = true), also update User.Status to ACTIVE
                if (req.IsActive.Value && user != null)
                    user.Status = "ACTIVE";
            }

            // Update user credentials
            if (!string.IsNullOrWhiteSpace(req.Username) && req.Username.Trim() != user.Username)
            {
                var userExists = await _context.Users.AnyAsync(u => u.UserId != user.UserId && u.Username.ToLower() == req.Username.Trim().ToLower());
                if (userExists) return BadRequest(new { message = "Username already exists" });
                user.Username = req.Username.Trim();
            }

            if (!string.IsNullOrWhiteSpace(req.Password))
                user.PasswordHash = VehicleInsuranceAPI.Backend.LoginUserManagement.PasswordHashService.HashPassword(req.Password!);

            if (req.Email != null)
                user.Email = string.IsNullOrWhiteSpace(req.Email) ? null : req.Email.Trim();

            if (!string.IsNullOrWhiteSpace(req.Status))
                user.Status = req.Status.Trim().ToUpper();

            if (req.BannedUntil.HasValue)
                user.BannedUntil = req.BannedUntil.Value;

            await _context.SaveChangesAsync();
            return Ok(new { success = true });
        }

        // DELETE: api/AgentStaffManagement/staff/5?permanent=false
        [HttpDelete("staff/{id:int}")]
        public async Task<IActionResult> DeleteStaff(int id, [FromQuery] bool permanent = false)
        {
            try
            {
                var staff = await _context.Staff
                    .Include(s => s.User)
                    .FirstOrDefaultAsync(s => s.StaffId == id);

                if (staff == null) return NotFound();

                var user = staff.User;

                if (!permanent)
                {
                    // Soft delete - deactivate
                    if (user != null)
                    {
                        user.Status = "INACTIVE";
                    }
                    staff.IsActive = false;
                    await _context.SaveChangesAsync();
                    return Ok(new { success = true, mode = "soft" });
                }

                // Hard delete
                _context.Staff.Remove(staff);
                
                // Only remove user-related logs/notifications if user exists
                if (user != null)
                {
                    var logs = await _context.AuditLogs.Where(l => l.UserId == user.UserId).ToListAsync();
                    if (logs.Count > 0) _context.AuditLogs.RemoveRange(logs);
                    var notis = await _context.Notifications.Where(n => n.ToUserId == user.UserId).ToListAsync();
                    if (notis.Count > 0) _context.Notifications.RemoveRange(notis);
                    _context.Users.Remove(user);
                }
                
                await _context.SaveChangesAsync();
                return Ok(new { success = true, mode = "hard" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = $"Error deleting staff: {ex.Message}" });
            }
        }

        // Helper functions
        private static string GenerateRandomUsername()
        {
            var random = new Random();
            var chars = "abcdefghijklmnopqrstuvwxyz0123456789";
            var sb = new System.Text.StringBuilder();
            sb.Append("staff_");
            for (int i = 0; i < 8; i++)
                sb.Append(chars[random.Next(chars.Length)]);
            return sb.ToString();
        }

        private static string GenerateRandomPassword()
        {
            var random = new Random();
            var chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%";
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < 12; i++)
                sb.Append(chars[random.Next(chars.Length)]);
            return sb.ToString();
        }
    }
}
