using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VehicleInsuranceAPI.Data;
using VehicleInsuranceAPI.Models;
using System;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;

namespace VehicleInsuranceAPI.Controllers
{
    [Route("api/admin-notification")]
    [ApiController]
    public class AdminNotificationController : ControllerBase
    {
        private readonly VehicleInsuranceContext _context;

        public AdminNotificationController(VehicleInsuranceContext context)
        {
            _context = context;
        }

        // GET: Lấy danh sách users để gửi thông báo
        [HttpGet("users")]
        public async Task<IActionResult> GetUsers()
        {
            try
            {
                var users = await _context.Users
                    .Select(u => new
                    {
                        userId = u.UserId,
                        username = u.Username,
                        email = u.Email,
                        phone = u.Phone
                    })
                    .ToListAsync();

                return Ok(users);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error: " + ex.Message });
            }
        }

        // GET: Lấy danh sách customers
        [HttpGet("customers")]
        public async Task<IActionResult> GetCustomers()
        {
            try
            {
                var customers = await _context.Customers
                    .Include(c => c.User)
                    .Select(c => new
                    {
                        userId = c.User!.UserId,
                        customerName = c.CustomerName,
                        email = c.User.Email,
                        phone = c.Phone
                    })
                    .Where(c => c.userId > 0)
                    .ToListAsync();

                return Ok(customers);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error: " + ex.Message });
            }
        }

        // GET: Lấy danh sách staff
        [HttpGet("staff")]
        public async Task<IActionResult> GetStaff()
        {
            try
            {
                var staff = await _context.Staff
                    .Include(s => s.User)
                    .Select(s => new
                    {
                        userId = s.User.UserId,
                        fullName = s.FullName,
                        email = s.User.Email,
                        phone = s.Phone,
                        position = s.Position
                    })
                    .Where(s => s.userId > 0)
                    .ToListAsync();

                return Ok(staff);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error: " + ex.Message });
            }
        }

        // POST: Gửi thông báo
        [HttpPost("send")]
        public async Task<IActionResult> SendNotification([FromBody] NotificationRequest request)
        {
            if (string.IsNullOrEmpty(request.Title) || string.IsNullOrEmpty(request.Message))
            {
                return BadRequest(new { message = "Title and message are required" });
            }

            try
            {
                List<int> userIds = new List<int>();

                // Xác định danh sách người nhận dựa trên RecipientType
                if (request.RecipientType == "all_users")
                {
                    // Gửi cho tất cả users
                    userIds = await _context.Users
                        .Where(u => u.Status == "ACTIVE")
                        .Select(u => u.UserId)
                        .ToListAsync();
                }
                else if (request.RecipientType == "all_customers")
                {
                    // Gửi cho tất cả customers
                    userIds = await _context.Customers
                        .Include(c => c.User)
                        .Where(c => c.User!.Status == "ACTIVE")
                        .Select(c => c.User!.UserId)
                        .Distinct()
                        .ToListAsync();
                }
                else if (request.RecipientType == "all_staff")
                {
                    // Gửi cho tất cả staff
                    userIds = await _context.Staff
                        .Include(s => s.User)
                        .Where(s => s.User.Status == "ACTIVE" && s.IsActive == true)
                        .Select(s => s.User.UserId)
                        .Distinct()
                        .ToListAsync();
                }
                else if (request.RecipientType == "specific" && request.UserId.HasValue)
                {
                    // Gửi cho người cụ thể
                    userIds.Add(request.UserId.Value);
                }
                else
                {
                    // Mặc định: gửi broadcast (UserId = null)
                    userIds = new List<int>();
                }

                int createdCount = 0;

                // Nếu có danh sách người nhận cụ thể, tạo notification cho từng người
                if (userIds.Count > 0)
                {
                    foreach (var userId in userIds)
                    {
                        var notification = new Notification
                        {
                            UserId = userId,
                            Title = request.Title,
                            Message = request.Message,
                            Type = request.Type ?? "INFO",
                            Channel = request.Channel ?? "IN_APP",
                            Status = "SENT",
                            CreatedDate = DateTime.UtcNow,
                            SentAt = DateTime.UtcNow,
                            IsRead = false
                        };
                        _context.Notifications.Add(notification);
                        createdCount++;
                    }
                }
                else
                {
                    // Gửi broadcast - UserId = null
                    var notification = new Notification
                    {
                        UserId = null,
                        Title = request.Title,
                        Message = request.Message,
                        Type = request.Type ?? "INFO",
                        Channel = request.Channel ?? "IN_APP",
                        Status = "SENT",
                        CreatedDate = DateTime.UtcNow,
                        SentAt = DateTime.UtcNow,
                        IsRead = false
                    };
                    _context.Notifications.Add(notification);
                    createdCount = 1;
                }

                await _context.SaveChangesAsync();

                return Ok(new { 
                    success = true,
                    message = $"Notification sent successfully to {createdCount} recipient(s)", 
                    notificationCount = createdCount 
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Error: " + ex.Message });
            }
        }

        // GET: Lấy notification history
        [HttpGet("history")]
        public async Task<IActionResult> GetHistory(int? userId = null, int pageSize = 20, int pageNumber = 1)
        {
            try
            {
                var query = _context.Notifications.AsQueryable();

                if (userId.HasValue)
                    query = query.Where(n => n.UserId == userId || n.UserId == null);

                var total = await query.CountAsync();
                var notifications = await query
                    .OrderByDescending(n => n.CreatedDate)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                return Ok(new { total, notifications });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error: " + ex.Message });
            }
        }

        // GET: Lấy số lượng thông báo chưa đọc cho user
        [HttpGet("unread-count/{userId}")]
        public async Task<IActionResult> GetUnreadCount(int userId)
        {
            try
            {
                var unreadCount = await _context.Notifications
                    .Where(n => n.UserId == userId && n.IsRead == false)
                    .CountAsync();

                return Ok(new { unreadCount });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error: " + ex.Message });
            }
        }
    }

    public class NotificationRequest
    {
        public string? RecipientType { get; set; } = "all_users"; // all_users, all_customers, all_staff, specific, broadcast
        public int? UserId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string? Type { get; set; }
        public string? Channel { get; set; }
    }
}
