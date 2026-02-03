using Microsoft.AspNetCore.Mvc;
using VehicleInsuranceAPI.Data;
using VehicleInsuranceAPI.Models;
using Microsoft.EntityFrameworkCore;
using System.IO;
using System.Threading.Tasks;

namespace VehicleInsuranceAPI.Backend.CustomerInformation
{
    [ApiController]
    [Route("api/[controller]")]
    public class CustomerInformationController : ControllerBase
    {
        private readonly VehicleInsuranceContext _context;

        public CustomerInformationController(VehicleInsuranceContext context)
        {
            _context = context;
        }

        [HttpGet("demo")]
        public IActionResult Demo()
        {
            return Ok("Customer Information demo endpoint");
        }

        // GET: api/CustomerInformation/all
        [HttpGet("all")]
        public async Task<IActionResult> GetAllCustomers([FromQuery] string? search = null, [FromQuery] string? status = null)
        {
            var q = _context.Customers.AsNoTracking().Include(c => c.User).AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                q = q.Where(c => c.CustomerName!.ToLower().Contains(s) || c.Phone!.Contains(s));
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                var st = status.Trim().ToUpper();
                if (st == "ACTIVE")
                    q = q.Where(c => c.User == null || c.User.Status == "ACTIVE");
                else if (st == "INACTIVE")
                    q = q.Where(c => c.User != null && c.User.Status == "INACTIVE");
            }

            var customers = await q.OrderByDescending(c => c.CustomerId)
                .ToListAsync();

            var result = customers.Select(c => new
            {
                c.CustomerId,
                c.UserId,
                c.CustomerName,
                c.Phone,
                c.Address,
                c.Avatar,
                User = c.User == null ? null : new
                {
                    c.User.UserId,
                    c.User.Username,
                    c.User.Email,
                    c.User.RoleId,
                    RoleName = c.User.Role?.RoleName,
                    c.User.Status,
                    c.User.BannedUntil,
                    c.User.CreatedAt
                }
            }).ToList();

            return Ok(result);
        }

        [HttpGet("{userId}")]
        public async Task<IActionResult> GetCustomerByUserId(int userId)
        {
            try
            {
                var customer = await _context.Customers
                    .AsNoTracking()
                    .Include(c => c.User)
                    .FirstOrDefaultAsync(c => c.UserId == userId);

                if (customer == null)
                {
                    return Ok(new
                    {
                        success = false,
                        message = "Customer not found"
                    });
                }

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        customerId = customer.CustomerId,
                        userId = customer.UserId,
                        fullName = customer.CustomerName,
                        customerName = customer.CustomerName,
                        email = customer.User?.Email ?? string.Empty,
                        phone = customer.Phone,
                        address = customer.Address,
                        avatar = customer.Avatar
                    }
                });
            }
            catch (Exception ex)
            {
                return Ok(new
                {
                    success = false,
                    message = "Error retrieving customer information: " + ex.Message
                });
            }
        }

        // GET: api/CustomerInformation/by-id/5
        [HttpGet("by-id/{id:int}")]
        public async Task<IActionResult> GetCustomer(int id)
        {
            var customer = await _context.Customers
                .AsNoTracking()
                .Include(c => c.User)
                .FirstOrDefaultAsync(c => c.CustomerId == id);

            if (customer == null) return NotFound();

            return Ok(new
            {
                customer.CustomerId,
                customer.UserId,
                customer.CustomerName,
                customer.Phone,
                customer.Address,
                customer.Avatar,
                User = customer.User == null ? null : new
                {
                    customer.User.UserId,
                    customer.User.Username,
                    customer.User.Email,
                    customer.User.RoleId,
                    RoleName = customer.User.Role?.RoleName,
                    customer.User.IsLocked,
                    customer.User.Status,
                    customer.User.BannedUntil,
                    customer.User.CreatedAt
                }
            });
        }

        public class CreateCustomerRequest
        {
            public string CustomerName { get; set; } = string.Empty;
            public string? Phone { get; set; }
            public string? Email { get; set; }
            public string? Address { get; set; }
            public string? Username { get; set; }
            public string? Password { get; set; }
        }

        // POST: api/CustomerInformation
        [HttpPost]
        public async Task<IActionResult> CreateCustomer([FromBody] CreateCustomerRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.CustomerName))
                return BadRequest(new { message = "Customer Name is required" });

            // Validate phone if provided
            if (!string.IsNullOrWhiteSpace(req.Phone))
            {
                if (req.Phone.Length < 10 || req.Phone.Length > 11)
                    return BadRequest(new { message = "Phone must be 10-11 digits" });

                var phoneExists = await _context.Customers.AnyAsync(c => c.Phone == req.Phone);
                if (phoneExists) return BadRequest(new { message = "Phone number already in use" });
            }

            // Generate random username and password if not provided
            var username = req.Username ?? GenerateRandomUsername();
            var password = req.Password ?? GenerateRandomPassword();

            var userExists = await _context.Users.AnyAsync(u => u.Username.ToLower() == username.ToLower());
            if (userExists) return BadRequest(new { message = "Username already exists" });

            // RoleId 2 = CUSTOMER
            var user = new User
            {
                Username = username,
                PasswordHash = VehicleInsuranceAPI.Backend.LoginUserManagement.PasswordHashService.HashPassword(password),
                Email = string.IsNullOrWhiteSpace(req.Email) ? null : req.Email.Trim(),
                RoleId = 2,
                IsLocked = false,
                Status = "ACTIVE",
                CreatedAt = DateTime.Now
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var customer = new Customer
            {
                UserId = user.UserId,
                CustomerName = req.CustomerName.Trim(),
                Phone = string.IsNullOrWhiteSpace(req.Phone) ? null : req.Phone.Trim(),
                Address = string.IsNullOrWhiteSpace(req.Address) ? null : req.Address.Trim(),
                Avatar = "/images/default-avatar.png"
            };

            _context.Customers.Add(customer);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                customerId = customer.CustomerId,
                userId = user.UserId,
                generatedUsername = username,
                generatedPassword = password
            });
        }

        public class UpdateCustomerRequest
        {
            public string? FullName { get; set; }
            public string? CustomerName { get; set; }
            public string? Phone { get; set; }
            public string? Email { get; set; }
            public string? Address { get; set; }
            public string? Username { get; set; }
            public string? Password { get; set; }
            public string? Status { get; set; }
            public DateTime? BannedUntil { get; set; }
        }

        // PUT: api/CustomerInformation/5
        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateCustomer(int id, [FromBody] UpdateCustomerRequest req)
        {
            // Try to find customer by CustomerId first, then by UserId
            var customer = await _context.Customers
                .Include(c => c.User)
                .FirstOrDefaultAsync(c => c.CustomerId == id || c.UserId == id);

            if (customer == null) return NotFound();

            // Update customer profile
            if (!string.IsNullOrWhiteSpace(req.FullName) || !string.IsNullOrWhiteSpace(req.CustomerName))
            {
                var fullName = req.FullName ?? req.CustomerName;
                if (!string.IsNullOrWhiteSpace(fullName))
                    customer.CustomerName = fullName.Trim();
            }

            if (req.Phone != null)
            {
                if (!string.IsNullOrWhiteSpace(req.Phone))
                {
                    if (req.Phone.Length < 10 || req.Phone.Length > 11)
                        return BadRequest(new { success = false, message = "Phone must be 10-11 digits" });

                    var phoneExists = await _context.Customers.AnyAsync(c => c.CustomerId != customer.CustomerId && c.Phone == req.Phone);
                    if (phoneExists) return BadRequest(new { success = false, message = "Phone number already in use" });
                }
                customer.Phone = string.IsNullOrWhiteSpace(req.Phone) ? null : req.Phone.Trim();
            }

            if (req.Address != null)
                customer.Address = string.IsNullOrWhiteSpace(req.Address) ? null : req.Address.Trim();

            // Update user if linked
            if (customer.UserId.HasValue)
            {
                var user = customer.User;
                if (user != null)
                {
                    if (!string.IsNullOrWhiteSpace(req.Username) && req.Username.Trim() != user.Username)
                    {
                        var userExists = await _context.Users.AnyAsync(u => u.UserId != user.UserId && u.Username.ToLower() == req.Username.Trim().ToLower());
                        if (userExists) return BadRequest(new { success = false, message = "Username already exists" });
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
                }
            }

            await _context.SaveChangesAsync();
            return Ok(new { success = true });
        }

        // DELETE: api/CustomerInformation/5?permanent=false
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteCustomer(int id, [FromQuery] bool permanent = false)
        {
            try
            {
                var customer = await _context.Customers
                    .Include(c => c.User)
                    .FirstOrDefaultAsync(c => c.CustomerId == id);

                if (customer == null) return NotFound();

                if (!permanent)
                {
                    if (customer.User != null)
                        customer.User.Status = "INACTIVE";

                    // Hide customer policies
                    var policies = await _context.Policies.Where(p => p.CustomerId == id).ToListAsync();
                    foreach (var p in policies) p.IsHidden = true;

                    await _context.SaveChangesAsync();
                    return Ok(new { success = true, mode = "soft" });
                }

                // Hard delete
                var policies2 = await _context.Policies.Where(p => p.CustomerId == id).ToListAsync();
                foreach (var p in policies2) p.IsHidden = true;

                customer.CustomerName = "[DELETED]";
                customer.Phone = null;
                customer.Address = null;
                customer.Avatar = null;
                customer.UserId = null;

                if (customer.User != null)
                {
                    var logs = await _context.AuditLogs.Where(l => l.UserId == customer.User.UserId).ToListAsync();
                    if (logs.Count > 0) _context.AuditLogs.RemoveRange(logs);
                    var notis = await _context.Notifications.Where(n => n.ToUserId == customer.User.UserId).ToListAsync();
                    if (notis.Count > 0) _context.Notifications.RemoveRange(notis);
                    _context.Users.Remove(customer.User);
                }

                await _context.SaveChangesAsync();
                return Ok(new { success = true, mode = "hard" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = $"Error deleting customer: {ex.Message}" });
            }
        }

        [HttpPut("{userId}/avatar")]
        public async Task<IActionResult> UpdateAvatar(int userId, [FromBody] UpdateAvatarRequest request)
        {
            try
            {
                var customer = await _context.Customers
                    .FirstOrDefaultAsync(c => c.UserId == userId);

                if (customer == null)
                {
                    return Ok(new
                    {
                        success = false,
                        message = "Customer not found"
                    });
                }

                customer.Avatar = request.Avatar;
                _context.Customers.Update(customer);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    message = "Avatar updated successfully",
                    data = new
                    {
                        customerId = customer.CustomerId,
                        userId = customer.UserId,
                        avatar = customer.Avatar
                    }
                });
            }
            catch (Exception ex)
            {
                return Ok(new
                {
                    success = false,
                    message = "Error updating avatar: " + ex.Message
                });
            }
        }

        [HttpPost("{userId}/upload-avatar")]
        public async Task<IActionResult> UploadAvatar(int userId, IFormFile file)
        {
            try
            {
                var customer = await _context.Customers
                    .FirstOrDefaultAsync(c => c.UserId == userId);

                if (customer == null)
                {
                    return Ok(new { success = false, message = "Customer not found" });
                }

                if (file == null || file.Length == 0)
                {
                    return Ok(new { success = false, message = "No file uploaded" });
                }

                // Validate file type
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
                var fileExtension = Path.GetExtension(file.FileName).ToLower();
                if (!allowedExtensions.Contains(fileExtension))
                {
                    return Ok(new { success = false, message = "Invalid file type. Only images are allowed." });
                }

                // Create uploads directory if it doesn't exist
                var uploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "uploads", "avatars");
                Directory.CreateDirectory(uploadsDir);

                // Generate unique filename
                var fileName = $"{userId}_{DateTime.Now.Ticks}{fileExtension}";
                var filePath = Path.Combine(uploadsDir, fileName);

                // Save file
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                // Save relative path to database
                var avatarPath = $"/uploads/avatars/{fileName}";
                customer.Avatar = avatarPath;
                _context.Customers.Update(customer);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    message = "Avatar uploaded successfully",
                    data = new
                    {
                        customerId = customer.CustomerId,
                        userId = customer.UserId,
                        avatar = customer.Avatar
                    }
                });
            }
            catch (Exception ex)
            {
                return Ok(new
                {
                    success = false,
                    message = "Error uploading avatar: " + ex.Message
                });
            }
        }

        // Helper functions
        private static string GenerateRandomUsername()
        {
            var random = new Random();
            var chars = "abcdefghijklmnopqrstuvwxyz0123456789";
            var sb = new System.Text.StringBuilder();
            sb.Append("cust_");
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

    public class UpdateAvatarRequest
    {
        public string? Avatar { get; set; }
    }
}

