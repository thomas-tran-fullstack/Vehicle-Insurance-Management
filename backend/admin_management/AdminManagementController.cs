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
        public async Task<IActionResult> GetUsers([FromQuery] int? roleId = null, [FromQuery] string? search = null, [FromQuery] string? status = null)
        {
            var q = _context.Users.AsNoTracking().Include(u => u.Role).AsQueryable();

            if (roleId.HasValue)
                q = q.Where(u => u.RoleId == roleId.Value);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                q = q.Where(u => u.Username.ToLower().Contains(s) || (u.Email != null && u.Email.ToLower().Contains(s)));
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                var st = status.Trim().ToUpper();
                q = q.Where(u => u.Status != null && u.Status.ToUpper() == st);
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

            // RoleId mapping: ADMIN=1, STAFF=2, CUSTOMER=3
            if (req.RoleId == 2)
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
            else if (req.RoleId == 3)
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
            if (user.RoleId == 2)
            {
                var staff = await _context.Staff.FirstOrDefaultAsync(s => s.UserId == id);
                if (staff != null)
                {
                    staff.FullName = req.FullName;
                    staff.Phone = req.Phone;
                    staff.Position = req.Position;
                }
            }
            else if (user.RoleId == 3)
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
            if (user.RoleId == 3)
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
            try
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

                // permanent delete - manually cascade delete all related records
                // First, try to delete ANY Staff record referencing this user
                var staff = await _context.Staff.FirstOrDefaultAsync(s => s.UserId == id);
                if (staff != null)
                {
                    var staffId = staff.StaffId;
                    var inspections = await _context.VehicleInspections
                        .Where(vi => vi.AssignedStaffId == staffId).ToListAsync();
                    _context.VehicleInspections.RemoveRange(inspections);
                    await _context.SaveChangesAsync();
                    _context.ChangeTracker.Clear();
                    
                    staff = await _context.Staff.FirstOrDefaultAsync(s => s.StaffId == staffId);
                    if (staff != null)
                    {
                        _context.Staff.Remove(staff);
                        await _context.SaveChangesAsync();
                        _context.ChangeTracker.Clear();
                    }
                }

                // Next, try to delete ANY Customer record referencing this user
                var customer = await _context.Customers.FirstOrDefaultAsync(c => c.UserId == id);
                if (customer != null)
                {
                    var customerId = customer.CustomerId;
                    
                    // Delete vehicle inspections and estimates for vehicles
                    var vehicles = await _context.Vehicles
                        .Where(v => v.CustomerId == customerId).ToListAsync();
                    var vehicleIds = vehicles.Select(v => v.VehicleId).ToList();
                    
                    if (vehicleIds.Count > 0)
                    {
                        var inspections = await _context.VehicleInspections
                            .Where(vi => vi.VehicleId.HasValue && vehicleIds.Contains(vi.VehicleId.Value))
                            .ToListAsync();
                        _context.VehicleInspections.RemoveRange(inspections);
                        await _context.SaveChangesAsync();
                        _context.ChangeTracker.Clear();
                        
                        var estimates = await _context.Estimates
                            .Where(e => e.VehicleId.HasValue && vehicleIds.Contains(e.VehicleId.Value))
                            .ToListAsync();
                        _context.Estimates.RemoveRange(estimates);
                        await _context.SaveChangesAsync();
                        _context.ChangeTracker.Clear();
                    }
                    
                    vehicles = await _context.Vehicles
                        .Where(v => v.CustomerId == customerId).ToListAsync();
                    _context.Vehicles.RemoveRange(vehicles);
                    await _context.SaveChangesAsync();
                    _context.ChangeTracker.Clear();
                    
                    // Delete policies and related
                    var policies = await _context.Policies
                        .Where(p => p.CustomerId == customerId).ToListAsync();
                    var policyIds = policies.Select(p => p.PolicyId).ToList();
                    
                    if (policyIds.Count > 0)
                    {
                        var bills = await _context.Bills
                            .Where(b => b.PolicyId.HasValue && policyIds.Contains(b.PolicyId.Value))
                            .ToListAsync();
                        _context.Bills.RemoveRange(bills);
                        await _context.SaveChangesAsync();
                        _context.ChangeTracker.Clear();
                        
                        var claims = await _context.Claims
                            .Where(c => c.PolicyId.HasValue && policyIds.Contains(c.PolicyId.Value))
                            .ToListAsync();
                        _context.Claims.RemoveRange(claims);
                        await _context.SaveChangesAsync();
                        _context.ChangeTracker.Clear();
                        
                        var penalties = await _context.Penalties
                            .Where(p => p.PolicyId.HasValue && policyIds.Contains(p.PolicyId.Value))
                            .ToListAsync();
                        _context.Penalties.RemoveRange(penalties);
                        await _context.SaveChangesAsync();
                        _context.ChangeTracker.Clear();
                        
                        var cancellations = await _context.InsuranceCancellations
                            .Where(ic => ic.PolicyId.HasValue && policyIds.Contains(ic.PolicyId.Value))
                            .ToListAsync();
                        _context.InsuranceCancellations.RemoveRange(cancellations);
                        await _context.SaveChangesAsync();
                        _context.ChangeTracker.Clear();
                    }
                    
                    policies = await _context.Policies
                        .Where(p => p.CustomerId == customerId).ToListAsync();
                    _context.Policies.RemoveRange(policies);
                    await _context.SaveChangesAsync();
                    _context.ChangeTracker.Clear();
                    
                    // Delete feedbacks, testimonials, estimates
                    var feedbacks = await _context.Feedbacks
                        .Where(f => f.CustomerId == customerId).ToListAsync();
                    _context.Feedbacks.RemoveRange(feedbacks);
                    await _context.SaveChangesAsync();
                    _context.ChangeTracker.Clear();
                    
                    var testimonials = await _context.Testimonials
                        .Where(t => t.CustomerId == customerId).ToListAsync();
                    _context.Testimonials.RemoveRange(testimonials);
                    await _context.SaveChangesAsync();
                    _context.ChangeTracker.Clear();
                    
                    var estimates2 = await _context.Estimates
                        .Where(e => e.CustomerId == customerId).ToListAsync();
                    _context.Estimates.RemoveRange(estimates2);
                    await _context.SaveChangesAsync();
                    _context.ChangeTracker.Clear();
                    
                    customer = await _context.Customers.FirstOrDefaultAsync(c => c.CustomerId == customerId);
                    if (customer != null)
                    {
                        _context.Customers.Remove(customer);
                        await _context.SaveChangesAsync();
                        _context.ChangeTracker.Clear();
                    }
                }

                // Remove user-related logs/notifications (PII)
                var logs = await _context.AuditLogs.Where(l => l.UserId == id).ToListAsync();
                _context.AuditLogs.RemoveRange(logs);
                await _context.SaveChangesAsync();
                _context.ChangeTracker.Clear();
                
                var notis = await _context.Notifications.Where(n => n.ToUserId == id).ToListAsync();
                _context.Notifications.RemoveRange(notis);
                await _context.SaveChangesAsync();
                _context.ChangeTracker.Clear();

                // Delete user
                user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == id);
                if (user != null)
                {
                    _context.Users.Remove(user);
                    await _context.SaveChangesAsync();
                    _context.ChangeTracker.Clear();
                }
                
                return Ok(new { success = true, mode = "hard" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = $"Error deleting user: {ex.Message}" });
            }
        }
    }
}
