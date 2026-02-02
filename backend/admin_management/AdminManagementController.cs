using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VehicleInsuranceAPI.Data;
using VehicleInsuranceAPI.Models;

namespace VehicleInsuranceAPI.Backend.AdminManagement
{
    [ApiController]
    [Route("api/[controller]")]
    public class AdminManagementController : ControllerBase
    {
        private readonly VehicleInsuranceContext _context;

        public AdminManagementController(VehicleInsuranceContext context)
        {
            _context = context;
        }

        // GET: api/AdminManagement/users?roleId=2&search=abc
        [HttpGet("users")]
        public async Task<IActionResult> GetUsers([FromQuery] int? roleId = null, [FromQuery] string? search = null)
        {
            var q = _context.Users.AsNoTracking().Include(u => u.Role).AsQueryable();

            if (roleId.HasValue)
                q = q.Where(u => u.RoleId == roleId.Value);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                q = q.Where(u => u.Username.ToLower().Contains(s) || (u.Email != null && u.Email.ToLower().Contains(s)));
            }

            var users = await q.OrderByDescending(u => u.UserId)
                .Select(u => new
                {
                    u.UserId,
                    u.Username,
                    u.Email,
                    u.RoleId,
                    RoleName = u.Role.RoleName,
                    u.IsLocked,
                    u.Status,
                    u.BannedUntil,
                    u.CreatedAt
                })
                .ToListAsync();

            return Ok(users);
        }

        // GET: api/AdminManagement/users/5
        [HttpGet("users/{id:int}")]
        public async Task<IActionResult> GetUser(int id)
        {
            var user = await _context.Users
                .AsNoTracking()
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.UserId == id);

            if (user == null) return NotFound();

            var staff = await _context.Staff.AsNoTracking().FirstOrDefaultAsync(s => s.UserId == id);
            var customer = await _context.Customers.AsNoTracking().FirstOrDefaultAsync(c => c.UserId == id);

            return Ok(new
            {
                user.UserId,
                user.Username,
                user.Email,
                user.RoleId,
                RoleName = user.Role.RoleName,
                user.IsLocked,
                user.Status,
                user.BannedUntil,
                user.CreatedAt,
                Staff = staff == null ? null : new { staff.StaffId, staff.FullName, staff.Phone, staff.Position, staff.Avatar, staff.IsActive },
                Customer = customer == null ? null : new { customer.CustomerId, customer.CustomerName, customer.Phone, customer.Address, customer.Avatar }
            });
        }

        public class UpsertUserRequest
        {
            public string Username { get; set; } = string.Empty;
            public string? Password { get; set; }
            public string? Email { get; set; }
            public int RoleId { get; set; }
            public string? Status { get; set; } // ACTIVE / INACTIVE
            public DateTime? BannedUntil { get; set; }
            public bool? IsLocked { get; set; }

            // Profile fields
            public string? FullName { get; set; }
            public string? Phone { get; set; }
            public string? Position { get; set; } // staff
            public string? Address { get; set; }  // customer
        }

        // POST: api/AdminManagement/users
        [HttpPost("users")]
        public async Task<IActionResult> CreateUser([FromBody] UpsertUserRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Username)) return BadRequest(new { message = "Username is required" });
            if (string.IsNullOrWhiteSpace(req.Password)) return BadRequest(new { message = "Password is required" });
            if (req.RoleId <= 0) return BadRequest(new { message = "RoleId is required" });

            var exists = await _context.Users.AnyAsync(u => u.Username.ToLower() == req.Username.Trim().ToLower());
            if (exists) return BadRequest(new { message = "Username already exists" });

            var user = new User
            {
                Username = req.Username.Trim(),
                PasswordHash = VehicleInsuranceAPI.Backend.LoginUserManagement.PasswordHashService.HashPassword(req.Password!),
                Email = string.IsNullOrWhiteSpace(req.Email) ? null : req.Email.Trim(),
                RoleId = req.RoleId,
                IsLocked = req.IsLocked ?? false,
                Status = string.IsNullOrWhiteSpace(req.Status) ? "ACTIVE" : req.Status.Trim().ToUpper(),
                BannedUntil = req.BannedUntil,
                CreatedAt = DateTime.Now
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // RoleId mapping: ADMIN=1, CUSTOMER=2, STAFF=3
            if (req.RoleId == 3)
            {
                var staff = new Staff
                {
                    UserId = user.UserId,
                    FullName = req.FullName,
                    Phone = req.Phone,
                    Position = req.Position,
                    Avatar = "/images/default-avatar.png",
                    IsActive = true
                };
                _context.Staff.Add(staff);
                await _context.SaveChangesAsync();
            }
            else if (req.RoleId == 2)
            {
                var customer = new Customer
                {
                    UserId = user.UserId,
                    CustomerName = req.FullName ?? req.Username,
                    Phone = req.Phone,
                    Address = req.Address,
                    Avatar = "/images/default-avatar.png"
                };
                _context.Customers.Add(customer);
                await _context.SaveChangesAsync();
            }

            return Ok(new { success = true, userId = user.UserId });
        }

        // PUT: api/AdminManagement/users/5
        [HttpPut("users/{id:int}")]
        public async Task<IActionResult> UpdateUser(int id, [FromBody] UpsertUserRequest req)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == id);
            if (user == null) return NotFound();

            // username change (optional)
            if (!string.IsNullOrWhiteSpace(req.Username) && req.Username.Trim() != user.Username)
            {
                var exists = await _context.Users.AnyAsync(u => u.UserId != id && u.Username.ToLower() == req.Username.Trim().ToLower());
                if (exists) return BadRequest(new { message = "Username already exists" });
                user.Username = req.Username.Trim();
            }

            if (!string.IsNullOrWhiteSpace(req.Password))
            {
                user.PasswordHash = VehicleInsuranceAPI.Backend.LoginUserManagement.PasswordHashService.HashPassword(req.Password!);
            }

            user.Email = string.IsNullOrWhiteSpace(req.Email) ? null : req.Email.Trim();
            user.IsLocked = req.IsLocked ?? user.IsLocked;
            user.Status = string.IsNullOrWhiteSpace(req.Status) ? user.Status : req.Status.Trim().ToUpper();
            user.BannedUntil = req.BannedUntil;

            // Update profile
            if (user.RoleId == 3)
            {
                var staff = await _context.Staff.FirstOrDefaultAsync(s => s.UserId == id);
                if (staff != null)
                {
                    staff.FullName = req.FullName;
                    staff.Phone = req.Phone;
                    staff.Position = req.Position;
                }
            }
            else if (user.RoleId == 2)
            {
                var customer = await _context.Customers.FirstOrDefaultAsync(c => c.UserId == id);
                if (customer != null)
                {
                    customer.CustomerName = req.FullName ?? customer.CustomerName;
                    customer.Phone = req.Phone;
                    customer.Address = req.Address;
                }
            }

            // If user becomes inactive/banned -> hide policies
            if (user.RoleId == 2)
            {
                var customer = await _context.Customers.FirstOrDefaultAsync(c => c.UserId == id);
                if (customer != null)
                {
                    var shouldHide = (user.Status != null && user.Status.ToUpper() != "ACTIVE")
                                     || (user.BannedUntil != null && user.BannedUntil.Value > DateTime.Now);
                    if (shouldHide)
                    {
                        var policies = await _context.Policies.Where(p => p.CustomerId == customer.CustomerId).ToListAsync();
                        foreach (var p in policies) p.IsHidden = true;
                    }
                }
            }

            await _context.SaveChangesAsync();
            return Ok(new { success = true });
        }

        // DELETE: api/AdminManagement/users/5?permanent=false
        // - permanent=false: soft delete -> INACTIVE + hide policies
        // - permanent=true : remove user + profile PII (customer) but keep business records; policies/claims/bills remain hidden
        [HttpDelete("users/{id:int}")]
        public async Task<IActionResult> DeleteUser(int id, [FromQuery] bool permanent = false)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == id);
            if (user == null) return NotFound();

            // Helper: hide customer policies
            async Task HideCustomerPolicies(int userId)
            {
                var customer = await _context.Customers.FirstOrDefaultAsync(c => c.UserId == userId);
                if (customer == null) return;
                var policies = await _context.Policies.Where(p => p.CustomerId == customer.CustomerId).ToListAsync();
                foreach (var p in policies) p.IsHidden = true;
            }

            if (!permanent)
            {
                user.Status = "INACTIVE";
                await HideCustomerPolicies(id);
                await _context.SaveChangesAsync();
                return Ok(new { success = true, mode = "soft" });
            }

            // permanent delete
            if (user.RoleId == 3)
            {
                var staff = await _context.Staff.FirstOrDefaultAsync(s => s.UserId == id);
                if (staff != null) _context.Staff.Remove(staff);
            }
            else if (user.RoleId == 2)
            {
                // Keep business records (Option A) but remove PII & credentials
                await HideCustomerPolicies(id);

                var customer = await _context.Customers.FirstOrDefaultAsync(c => c.UserId == id);
                if (customer != null)
                {
                    customer.UserId = null; // detach from deleted user
                    customer.CustomerName = "[DELETED]";
                    customer.Phone = null;
                    customer.Address = null;
                    customer.Avatar = null;
                }
            }

            // Remove user-related logs/notifications (PII)
            var logs = await _context.AuditLogs.Where(l => l.UserId == id).ToListAsync();
            if (logs.Count > 0) _context.AuditLogs.RemoveRange(logs);
            var notis = await _context.Notifications.Where(n => n.UserId == id).ToListAsync();
            if (notis.Count > 0) _context.Notifications.RemoveRange(notis);

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
            return Ok(new { success = true, mode = "hard" });
        }
    }
}
