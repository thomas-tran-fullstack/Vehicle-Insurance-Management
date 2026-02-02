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

        public LoginUserManagementController(VehicleInsuranceContext context)
        {
            _context = context;
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
                        Message = "Your account had been inactive"
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
    }

    // Request model for change password
    public class ChangePasswordRequest
    {
        public string Username { get; set; }
        public string CurrentPassword { get; set; }
        public string NewPassword { get; set; }
    }
}
