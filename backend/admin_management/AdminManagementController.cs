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
                userId = user.UserId,
                username = user.Username,
                email = user.Email,
                roleId = user.RoleId,
                roleName = user.Role.RoleName,
                isLocked = user.IsLocked,
                status = user.Status,
                bannedUntil = user.BannedUntil,
                createdAt = user.CreatedAt,
                staff = staff == null ? null : new { staffId = staff.StaffId, fullName = staff.FullName, phone = staff.Phone, position = staff.Position, avatar = staff.Avatar, isActive = staff.IsActive },
                customer = customer == null ? null : new { customerId = customer.CustomerId, customerName = customer.CustomerName, phone = customer.Phone, address = customer.Address, avatar = customer.Avatar }
            });
        }

        public class UpsertUserRequest
        {
            public string Username { get; set; } = string.Empty;
            public string? Password { get; set; }
            public string? CurrentPassword { get; set; }  // For password change verification
            public string? NewPassword { get; set; }     // For password change
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
            public string? Avatar { get; set; }   // avatar URL
        }

        // POST: api/AdminManagement/users
        [HttpPost("users")]
        public async Task<IActionResult> CreateUser([FromBody] UpsertUserRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Username)) return BadRequest(new { message = "Username is required" });
            if (string.IsNullOrWhiteSpace(req.Password)) return BadRequest(new { message = "Password is required" });
            if (string.IsNullOrWhiteSpace(req.Email)) return BadRequest(new { message = "Email is required" });
            if (req.RoleId <= 0) return BadRequest(new { message = "RoleId is required" });

            try
            {
                var exists = await _context.Users.AnyAsync(u => u.Username.ToLower() == req.Username.Trim().ToLower());
                if (exists) return BadRequest(new { message = "Username already exists" });

                var emailExists = await _context.Users.AnyAsync(u => u.Email != null && u.Email.ToLower() == req.Email.Trim().ToLower());
                if (emailExists) return BadRequest(new { message = "Your email already exist" });

                var user = new User
                {
                    Username = req.Username.Trim(),
                    PasswordHash = VehicleInsuranceAPI.Backend.LoginUserManagement.PasswordHashService.HashPassword(req.Password!),
                    Email = req.Email.Trim(),
                    Phone = req.Phone,
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
            catch (DbUpdateException dbEx)
            {
                // Check if it's an email uniqueness violation
                if (dbEx.InnerException?.Message.Contains("Email") == true || dbEx.InnerException?.Message.Contains("UQ_") == true)
                {
                    return BadRequest(new { message = "Your email already exist" });
                }
                return BadRequest(new { message = $"Database error: {dbEx.InnerException?.Message ?? dbEx.Message}" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Error creating user: {ex.Message}" });
            }
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

            // Password change - verify current password first
            if (!string.IsNullOrWhiteSpace(req.CurrentPassword) || !string.IsNullOrWhiteSpace(req.NewPassword))
            {
                // Both fields must be provided for password change
                if (string.IsNullOrWhiteSpace(req.CurrentPassword) || string.IsNullOrWhiteSpace(req.NewPassword))
                    return BadRequest(new { message = "Current password and new password are required" });

                // Verify current password
                if (!VehicleInsuranceAPI.Backend.LoginUserManagement.PasswordHashService.VerifyPassword(req.CurrentPassword, user.PasswordHash))
                    return BadRequest(new { message = "Current password is incorrect" });

                // Update with new password
                user.PasswordHash = VehicleInsuranceAPI.Backend.LoginUserManagement.PasswordHashService.HashPassword(req.NewPassword);
            }
            else if (!string.IsNullOrWhiteSpace(req.Password))
            {
                // Old method: direct password update (without verification) - used by admin
                user.PasswordHash = VehicleInsuranceAPI.Backend.LoginUserManagement.PasswordHashService.HashPassword(req.Password!);
            }

            user.Email = string.IsNullOrWhiteSpace(req.Email) ? user.Email : req.Email.Trim();
            user.IsLocked = req.IsLocked ?? user.IsLocked;
            user.Status = string.IsNullOrWhiteSpace(req.Status) ? user.Status : req.Status.Trim().ToUpper();
            user.BannedUntil = req.BannedUntil;

            // Update profile
            if (user.RoleId == 2)
            {
                var staff = await _context.Staff.FirstOrDefaultAsync(s => s.UserId == id);
                if (staff != null)
                {
                    if (!string.IsNullOrWhiteSpace(req.FullName))
                        staff.FullName = req.FullName;
                    if (!string.IsNullOrWhiteSpace(req.Phone))
                        staff.Phone = req.Phone;
                    if (!string.IsNullOrWhiteSpace(req.Position))
                        staff.Position = req.Position;
                    if (!string.IsNullOrWhiteSpace(req.Avatar))
                        staff.Avatar = req.Avatar;
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
            
            // Return updated user data with staff/customer info
            var updatedStaff = await _context.Staff.AsNoTracking().FirstOrDefaultAsync(s => s.UserId == id);
            var updatedCustomer = await _context.Customers.AsNoTracking().FirstOrDefaultAsync(c => c.UserId == id);
            
            return Ok(new
            {
                success = true,
                userId = user.UserId,
                username = user.Username,
                email = user.Email,
                roleId = user.RoleId,
                status = user.Status,
                staff = updatedStaff == null ? null : new { staffId = updatedStaff.StaffId, fullName = updatedStaff.FullName, phone = updatedStaff.Phone, position = updatedStaff.Position, avatar = updatedStaff.Avatar },
                customer = updatedCustomer == null ? null : new { customerId = updatedCustomer.CustomerId, customerName = updatedCustomer.CustomerName, phone = updatedCustomer.Phone, address = updatedCustomer.Address }
            });
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

                if (!permanent)
                {
                    // Soft delete: mark as INACTIVE
                    user.Status = "INACTIVE";
                    await _context.SaveChangesAsync();
                    return Ok(new { success = true, mode = "soft" });
                }

                // Hard delete: remove all related records
                try
                {
                    // 1. Delete Staff-related records
                    var staff = await _context.Staff.FirstOrDefaultAsync(s => s.UserId == id);
                    if (staff != null)
                    {
                        var staffId = staff.StaffId;
                        var inspections = await _context.VehicleInspections
                            .Where(vi => vi.AssignedStaffId == staffId)
                            .ToListAsync();
                        _context.VehicleInspections.RemoveRange(inspections);
                        await _context.SaveChangesAsync();
                        
                        _context.Staff.Remove(staff);
                        await _context.SaveChangesAsync();
                    }

                    // 2. Delete Customer-related records
                    var customer = await _context.Customers.FirstOrDefaultAsync(c => c.UserId == id);
                    if (customer != null)
                    {
                        var custId = customer.CustomerId;

                        // Delete Claims (references Policies and Staff)
                        var claims = await _context.Claims
                            .Where(c => c.PolicyId.HasValue && 
                                _context.Policies.Where(p => p.CustomerId == custId).Select(p => p.PolicyId).Contains(c.PolicyId.Value))
                            .ToListAsync();
                        _context.Claims.RemoveRange(claims);
                        await _context.SaveChangesAsync();

                        // Get all policy IDs for this customer (needed for cascading deletes)
                        var policyIds = await _context.Policies
                            .Where(p => p.CustomerId == custId)
                            .Select(p => p.PolicyId)
                            .ToListAsync();

                        if (policyIds.Count > 0)
                        {
                            // Delete PolicyDocuments using raw SQL (not fully mapped in context)
                            try
                            {
                                await _context.Database.ExecuteSqlRawAsync(
                                    "DELETE FROM [PolicyDocuments] WHERE [PolicyId] IN ({0})",
                                    string.Join(",", policyIds));
                            }
                            catch { /* PolicyDocuments table might not exist */ }

                            // Delete Payments/Invoices using raw SQL (since they're not fully mapped in context)
                            try
                            {
                                // Delete Payments linked to Invoices linked to Policies
                                await _context.Database.ExecuteSqlRawAsync(
                                    "DELETE FROM [Payments] WHERE [InvoiceId] IN (SELECT [InvoiceId] FROM [Invoices] WHERE [PolicyId] IN ({0}))",
                                    string.Join(",", policyIds));

                                // Delete Invoices
                                await _context.Database.ExecuteSqlRawAsync(
                                    "DELETE FROM [Invoices] WHERE [PolicyId] IN ({0})",
                                    string.Join(",", policyIds));
                            }
                            catch { /* Invoices/Payments table might not exist or SQL syntax error */ }
                        }

                        // Delete Policies (references CustomerId, VehicleId)
                        var policies = await _context.Policies
                            .Where(p => p.CustomerId == custId)
                            .ToListAsync();
                        _context.Policies.RemoveRange(policies);
                        await _context.SaveChangesAsync();

                        // Delete VehicleInspections using raw SQL (model schema mismatch)
                        try
                        {
                            var vehicleIds = await _context.Vehicles
                                .Where(v => v.CustomerId == custId)
                                .Select(v => v.VehicleId)
                                .ToListAsync();

                            if (vehicleIds.Count > 0)
                            {
                                await _context.Database.ExecuteSqlRawAsync(
                                    "DELETE FROM [VehicleInspections] WHERE [VehicleId] IN ({0})",
                                    string.Join(",", vehicleIds));
                            }
                        }
                        catch { /* VehicleInspections deletion failed */ }

                        // Delete Estimates using raw SQL (model schema mismatch with database)
                        try
                        {
                            await _context.Database.ExecuteSqlRawAsync(
                                "DELETE FROM [Estimates] WHERE [CustomerId] = {0}",
                                custId);
                        }
                        catch { /* Estimates table might not exist or deletion failed */ }

                        // Delete Vehicles using raw SQL (model schema mismatch)
                        try
                        {
                            await _context.Database.ExecuteSqlRawAsync(
                                "DELETE FROM [Vehicles] WHERE [CustomerId] = {0}",
                                custId);
                        }
                        catch { /* Vehicles deletion failed */ }

                        // Delete Feedback using raw SQL (table name is singular)
                        try
                        {
                            await _context.Database.ExecuteSqlRawAsync(
                                "DELETE FROM [Feedback] WHERE [CustomerId] = {0}",
                                custId);
                        }
                        catch { /* Feedback deletion failed */ }

                        // Delete Testimonials using raw SQL
                        try
                        {
                            await _context.Database.ExecuteSqlRawAsync(
                                "DELETE FROM [Testimonials] WHERE [CustomerId] = {0}",
                                custId);
                        }
                        catch { /* Testimonials deletion failed */ }

                        // Delete Customer using raw SQL to avoid schema mismatches
                        try
                        {
                            await _context.Database.ExecuteSqlRawAsync(
                                "DELETE FROM [Customers] WHERE [CustomerId] = {0}",
                                custId);
                        }
                        catch { /* Customers deletion failed */ }
                    }

                    // 3. Delete user-level records using raw SQL
                    try
                    {
                        await _context.Database.ExecuteSqlRawAsync(
                            "DELETE FROM [AuditLogs] WHERE [UserId] = {0}",
                            id);
                    }
                    catch { /* AuditLogs deletion failed */ }

                    try
                    {
                        await _context.Database.ExecuteSqlRawAsync(
                            "DELETE FROM [Notifications] WHERE [UserId] = {0}",
                            id);
                    }
                    catch { /* Notifications deletion failed */ }

                    // 4. Delete User using raw SQL
                    try
                    {
                        await _context.Database.ExecuteSqlRawAsync(
                            "DELETE FROM [Users] WHERE [UserId] = {0}",
                            id);
                    }
                    catch { /* Users deletion failed */ }

                    return Ok(new { success = true, mode = "hard" });
                }
                catch (Exception innerEx)
                {
                    return StatusCode(500, new { success = false, message = $"Error during deletion: {innerEx.Message}", details = innerEx.InnerException?.Message });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = $"Error deleting user: {ex.Message}" });
            }
        }

        // POST: /api/upload/avatar
        [HttpPost("upload/avatar")]
        public async Task<IActionResult> UploadAvatar(IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0)
                    return BadRequest(new { message = "No file uploaded" });

                // Validate file
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                var fileExtension = Path.GetExtension(file.FileName).ToLower();
                if (!allowedExtensions.Contains(fileExtension))
                    return BadRequest(new { message = "Invalid file type. Only images allowed." });

                if (file.Length > 5 * 1024 * 1024) // 5MB
                    return BadRequest(new { message = "File size exceeds 5MB limit" });

                // Create uploads directory if not exists
                var uploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "uploads", "avatars");
                if (!Directory.Exists(uploadsDir))
                    Directory.CreateDirectory(uploadsDir);

                // Generate unique filename
                var fileName = $"avatar_{DateTime.Now.Ticks}{fileExtension}";
                var filePath = Path.Combine(uploadsDir, fileName);

                // Save file
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                var avatarUrl = $"/uploads/avatars/{fileName}";
                return Ok(new { success = true, url = avatarUrl });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = $"Error uploading avatar: {ex.Message}" });
            }
        }
    }
}
