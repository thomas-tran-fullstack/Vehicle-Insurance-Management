using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VehicleInsuranceAPI.Data;

namespace VehicleInsuranceAPI.Backend.LoginUserManagement
{
    [ApiController]
    [Route("api/[controller]")]
    public class LoginUserManagementController : ControllerBase
    {
        private readonly VehicleInsuranceContext _context;
        private readonly EmailService _emailService;

        public LoginUserManagementController(VehicleInsuranceContext context, EmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        [HttpGet("demo")]
        public IActionResult Demo()
        {
            return Ok("Login & User Management demo endpoint");
        }

        [HttpGet("test-hash")]
        public IActionResult TestHash()
        {
            var pwd1 = PasswordHashService.HashPassword("Admin123!");
            var pwd2 = PasswordHashService.HashPassword("Staff123!");
            var pwd3 = PasswordHashService.HashPassword("Cust123!");
            
            return Ok(new 
            { 
                admin = pwd1,
                staff = pwd2,
                customer = pwd3
            });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            if (string.IsNullOrEmpty(request.Username) || string.IsNullOrEmpty(request.Password))
            {
                return BadRequest(new LoginResponse
                {
                    Success = false,
                    Message = "Username and password are required"
                });
            }

            try
            {
                var user = await _context.Users
                    .Include(u => u.Role)
                    .FirstOrDefaultAsync(u => u.Username == request.Username);

                if (user == null || !PasswordHashService.VerifyPassword(request.Password, user.PasswordHash))
                {
                    return Unauthorized(new LoginResponse
                    {
                        Success = false,
                        Message = "Invalid username or password"
                    });
                }

                if (user.IsLocked == true)
                {
                    return Unauthorized(new LoginResponse
                    {
                        Success = false,
                        Message = "User account is locked"
                    });
                }

                // Business rules: inactive/banned users cannot login
                var status = (user.Status ?? "ACTIVE").ToUpper();
                if (status != "ACTIVE")
                {
                    return Unauthorized(new LoginResponse
                    {
                        Success = false,
                        Message = "Your account had been banned"
                    });
                }

                if (user.BannedUntil != null && user.BannedUntil.Value > DateTime.Now)
                {
                    var until = user.BannedUntil.Value;
                    return Unauthorized(new LoginResponse
                    {
                        Success = false,
                        Message = $"Your account had been inactive (banned until {until:HH:mm} - {until:dd/MM/yyyy})"
                    });
                }

                var response = new LoginResponse
                {
                    Success = true,
                    Message = "Login successful",
                    UserId = user.UserId,
                    Username = user.Username,
                    RoleId = user.RoleId,
                    RoleName = user.Role?.RoleName
                };

                // Get additional user information based on role
                if (user.Role?.RoleName == "CUSTOMER")
                {
                    var customer = await _context.Customers
                        .FirstOrDefaultAsync(c => c.UserId == user.UserId);
                    if (customer != null)
                    {
                        response.CustomerName = customer.CustomerName;
                        response.FullName = customer.CustomerName;
                        response.Avatar = customer.Avatar;
                    }
                }
                else if (user.Role?.RoleName == "STAFF")
                {
                    var staff = await _context.Staff
                        .FirstOrDefaultAsync(s => s.UserId == user.UserId);
                    if (staff != null)
                    {
                        response.FullName = staff.FullName;
                        response.Avatar = staff.Avatar;
                    }
                }

                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new LoginResponse
                {
                    Success = false,
                    Message = $"An error occurred: {ex.Message}"
                });
            }
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            if (string.IsNullOrEmpty(request.Username) || string.IsNullOrEmpty(request.Password))
            {
                return BadRequest(new RegisterResponse
                {
                    Success = false,
                    Message = "Username and password are required"
                });
            }

            try
            {
                // Check if username already exists
                var existingUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.Username == request.Username);

                if (existingUser != null)
                {
                    return BadRequest(new RegisterResponse
                    {
                        Success = false,
                        Message = "Username already exists"
                    });
                }

                // Get CUSTOMER role
                var customerRole = await _context.Roles
                    .FirstOrDefaultAsync(r => r.RoleName == "CUSTOMER");

                if (customerRole == null)
                {
                    return StatusCode(500, new RegisterResponse
                    {
                        Success = false,
                        Message = "CUSTOMER role not found in database"
                    });
                }

                // Create new user
                var newUser = new VehicleInsuranceAPI.Models.User
                {
                    Username = request.Username,
                    PasswordHash = PasswordHashService.HashPassword(request.Password),
                    Email = request.Email,
                    RoleId = customerRole.RoleId,
                    IsLocked = false,
                    CreatedAt = DateTime.Now
                };

                _context.Users.Add(newUser);
                await _context.SaveChangesAsync();

                // Create customer record
                var newCustomer = new VehicleInsuranceAPI.Models.Customer
                {
                    UserId = newUser.UserId,
                    CustomerName = request.CustomerName,
                    Address = request.Address,
                    Phone = request.Phone,
                    Avatar = "/images/default-avatar.png"
                };

                _context.Customers.Add(newCustomer);
                await _context.SaveChangesAsync();

                return Ok(new RegisterResponse
                {
                    Success = true,
                    Message = "Registration successful",
                    UserId = newUser.UserId,
                    CustomerId = newCustomer.CustomerId
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new RegisterResponse
                {
                    Success = false,
                    Message = $"An error occurred: {ex.Message}"
                });
            }
        }

        [HttpGet("check-username")]
        public async Task<IActionResult> CheckUsername([FromQuery] string username)
        {
            if (string.IsNullOrEmpty(username))
            {
                return BadRequest(new { exists = false });
            }

            var exists = await _context.Users
                .AnyAsync(u => u.Username.ToLower() == username.ToLower());
            
            return Ok(new { exists = exists });
        }

        [HttpGet("check-email")]
        public async Task<IActionResult> CheckEmail([FromQuery] string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                return BadRequest(new { exists = false });
            }

            var exists = await _context.Users
                .AnyAsync(u => u.Email != null && u.Email.ToLower() == email.ToLower());
            
            return Ok(new { exists = exists });
        }

        [HttpGet("check-phone")]
        public async Task<IActionResult> CheckPhone([FromQuery] string phone)
        {
            if (string.IsNullOrEmpty(phone))
            {
                return BadRequest(new { exists = false });
            }

            var exists = await _context.Customers
                .AnyAsync(c => c.Phone != null && c.Phone == phone);
            
            return Ok(new { exists = exists });
        }

        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            if (string.IsNullOrEmpty(request.Username) || string.IsNullOrEmpty(request.CurrentPassword) || string.IsNullOrEmpty(request.NewPassword))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Username, current password, and new password are required"
                });
            }

            try
            {
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Username == request.Username);

                if (user == null)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "User not found"
                    });
                }

                // Verify current password
                if (!PasswordHashService.VerifyPassword(request.CurrentPassword, user.PasswordHash))
                {
                    return Unauthorized(new
                    {
                        success = false,
                        message = "Current password is incorrect"
                    });
                }

                // Hash new password and update
                user.PasswordHash = PasswordHashService.HashPassword(request.NewPassword);
                _context.Users.Update(user);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    message = "Password changed successfully"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = $"An error occurred: {ex.Message}"
                });
            }
        }

        // ==================== GOOGLE SIGN-IN ENDPOINTS ====================

        [HttpPost("google-signin")]
        public async Task<IActionResult> GoogleSignIn([FromBody] GoogleSignInRequest request)
        {
            try
            {
                // Validate token and extract email (in production, verify with Google API)
                // For now, we trust the token from frontend

                var email = request.Email?.Trim().ToLower();
                if (string.IsNullOrWhiteSpace(email))
                    return BadRequest(new { success = false, message = "Email is required" });

                // Check if user exists
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email != null && u.Email.ToLower() == email);

                if (user != null)
                {
                    // User exists - get customer name if customer record exists
                    var customer = await _context.Customers
                        .FirstOrDefaultAsync(c => c.UserId == user.UserId);
                    var fullName = customer?.CustomerName ?? request.Name ?? user.Username;
                    return Ok(new
                    {
                        success = true,
                        userExists = true,
                        userId = user.UserId,
                        username = user.Username,
                        email = user.Email,
                        roleId = user.RoleId,
                        roleName = user.Role?.RoleName ?? "CUSTOMER",
                        fullName = fullName
                    });
                }

                // User doesn't exist - generate and send OTP
                var otp = GenerateOTP();
                
                // Store OTP in session/cache (in production, use Redis or database with TTL)
                var otpKey = $"otp_{email}";
                // For demo, store in memory with 5 minute expiry
                OtpStorage.Store(otpKey, otp, TimeSpan.FromMinutes(5));

                // Send OTP via email (implement email service)
                await SendOTPEmail(email, otp, request.Name ?? "User");

                return Ok(new
                {
                    success = true,
                    userExists = false,
                    message = "OTP sent to email. Please verify."
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        [HttpPost("verify-otp")]
        public async Task<IActionResult> VerifyOTP([FromBody] VerifyOTPRequest request)
        {
            try
            {
                var email = request.Email?.Trim().ToLower();
                var otp = request.OTP?.Trim();

                if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(otp))
                    return BadRequest(new { success = false, message = "Email and OTP are required" });

                // Verify OTP
                var otpKey = $"otp_{email}";
                if (!OtpStorage.Verify(otpKey, otp))
                    return BadRequest(new { success = false, message = "Invalid or expired OTP" });

                // Create new user (customer)
                var fullName = request.FullName ?? email.Split('@')[0];
                var username = GenerateUsername(fullName);
                var password = GenerateRandomPassword();
                var passwordHash = PasswordHashService.HashPassword(password);

                var newUser = new Models.User
                {
                    Username = username,
                    Email = email,
                    PasswordHash = passwordHash,
                    RoleId = 2, // Customer role
                    Status = "ACTIVE",
                    CreatedAt = DateTime.Now
                };

                _context.Users.Add(newUser);
                await _context.SaveChangesAsync();

                // Create customer record
                var customer = new Models.Customer
                {
                    UserId = newUser.UserId,
                    CustomerName = fullName,
                    Phone = null,
                    Address = null
                };

                _context.Customers.Add(customer);
                await _context.SaveChangesAsync();

                // Clear OTP
                OtpStorage.Remove(otpKey);

                return Ok(new
                {
                    success = true,
                    userId = newUser.UserId,
                    username = username,
                    email = newUser.Email,
                    roleId = newUser.RoleId,
                    roleName = "CUSTOMER",
                    fullName = fullName,
                    message = "Account created successfully"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        [HttpPost("resend-otp")]
        public async Task<IActionResult> ResendOTP([FromBody] ResendOTPRequest request)
        {
            try
            {
                var email = request.Email?.Trim().ToLower();
                if (string.IsNullOrWhiteSpace(email))
                    return BadRequest(new { success = false, message = "Email is required" });

                // Generate new OTP
                var otp = GenerateOTP();
                var otpKey = $"otp_{email}";
                OtpStorage.Store(otpKey, otp, TimeSpan.FromMinutes(5));

                // Send OTP
                await SendOTPEmail(email, otp, "User");

                return Ok(new { success = true, message = "OTP sent successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        // ==================== HELPER FUNCTIONS ====================

        private string GenerateOTP()
        {
            var random = new Random();
            return random.Next(1000, 9999).ToString();
        }

        private string GenerateUsername(string fullName)
        {
            var baseName = fullName?.Replace(" ", "").ToLower() ?? "user";
            var random = new Random();
            return $"{baseName}_{random.Next(100, 9999)}";
        }

        private string GenerateRandomPassword()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%";
            var random = new Random();
            var password = new System.Text.StringBuilder();
            for (int i = 0; i < 12; i++)
                password.Append(chars[random.Next(chars.Length)]);
            return password.ToString();
        }

        private async Task SendOTPEmail(string email, string otp, string name)
        {
            try
            {
                // Use EmailService to send OTP
                await _emailService.SendOTPAsync(email, otp, name);
                Console.WriteLine($"[EMAIL SUCCESS] OTP sent to {email}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[EMAIL ERROR] Failed to send OTP to {email}: {ex.Message}");
                // Don't throw - let the OTP verification continue even if email fails
                // In production, might want to log this and notify admin
            }
        }
    }

    // ==================== OTP STORAGE (In-Memory) ====================
    public static class OtpStorage
    {
        private static Dictionary<string, (string otp, DateTime expiry)> _storage = new();

        public static void Store(string key, string otp, TimeSpan duration)
        {
            _storage[key] = (otp, DateTime.UtcNow.Add(duration));
        }

        public static bool Verify(string key, string otp)
        {
            if (!_storage.ContainsKey(key))
                return false;

            var (storedOtp, expiry) = _storage[key];
            if (DateTime.UtcNow > expiry)
            {
                _storage.Remove(key);
                return false;
            }

            return storedOtp == otp;
        }

        public static void Remove(string key)
        {
            _storage.Remove(key);
        }
    }

    // ==================== REQUEST/RESPONSE MODELS ====================

    public class GoogleSignInRequest
    {
        public string? Token { get; set; }
        public string? Email { get; set; }
        public string? Name { get; set; }
    }

    public class VerifyOTPRequest
    {
        public string? Email { get; set; }
        public string? OTP { get; set; }
        public string? FullName { get; set; }
        public string? Token { get; set; }
    }

    public class ResendOTPRequest
    {
        public string? Email { get; set; }
    }

    // Request model for change password
    public class ChangePasswordRequest
    {
        public string? Username { get; set; }
        public string? CurrentPassword { get; set; }
        public string? NewPassword { get; set; }
    }
}
