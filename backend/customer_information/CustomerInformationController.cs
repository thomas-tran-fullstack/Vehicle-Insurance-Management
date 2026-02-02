using Microsoft.AspNetCore.Mvc;
using VehicleInsuranceAPI.Data;
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

        [HttpGet("{userId}")]
        public async Task<IActionResult> GetCustomerByUserId(int userId)
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

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        customerId = customer.CustomerId,
                        userId = customer.UserId,
                        fullName = customer.CustomerName,
                        customerName = customer.CustomerName,
                        address = customer.Address,
                        phone = customer.Phone,
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
    }

    public class UpdateAvatarRequest
    {
        public string? Avatar { get; set; }
    }
}
