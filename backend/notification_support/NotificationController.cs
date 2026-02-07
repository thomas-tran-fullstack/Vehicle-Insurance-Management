using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VehicleInsuranceAPI.Data;
using VehicleInsuranceAPI.Models;
using System;
using System.Threading.Tasks;
using System.Linq;

namespace VehicleInsuranceAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class NotificationController : ControllerBase
    {
        private readonly VehicleInsuranceContext _context;

        public NotificationController(VehicleInsuranceContext context)
        {
            _context = context;
        }

        // GET: Lấy thông báo của người dùng hiện tại
        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetUserNotifications(int userId, int pageSize = 20, int pageNumber = 1)
        {
            try
            {
                var total = await _context.Notifications
                    .Where(n => n.UserId == userId || n.UserId == null)
                    .CountAsync();

                var notifications = await _context.Notifications
                    .Where(n => n.UserId == userId || n.UserId == null)
                    .OrderByDescending(n => n.CreatedDate)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .Select(n => new
                    {
                        n.NotificationId,
                        n.Title,
                        n.Message,
                        n.Type,
                        n.Channel,
                        n.Status,
                        n.CreatedDate,
                        n.SentAt,
                        n.IsRead
                    })
                    .ToListAsync();

                return Ok(new { total, notifications });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error: " + ex.Message });
            }
        }

        // GET: Lấy số thông báo chưa đọc
        [HttpGet("unread/{userId}")]
        public async Task<IActionResult> GetUnreadCount(int userId)
        {
            try
            {
                var unreadCount = await _context.Notifications
                    .Where(n => (n.UserId == userId || n.UserId == null) && !n.IsRead)
                    .CountAsync();

                return Ok(new { unreadCount });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error: " + ex.Message });
            }
        }

        // PUT: Đánh dấu thông báo đã đọc
        [HttpPut("{id}/mark-read")]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            try
            {
                var notification = await _context.Notifications.FindAsync(id);
                if (notification == null)
                    return NotFound(new { message = "Notification not found" });

                notification.IsRead = true;
                _context.Notifications.Update(notification);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Notification marked as read" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error: " + ex.Message });
            }
        }

        // PUT: Đánh dấu tất cả thông báo của user đã đọc
        [HttpPut("user/{userId}/mark-all-read")]
        public async Task<IActionResult> MarkAllAsRead(int userId)
        {
            try
            {
                var notifications = await _context.Notifications
                    .Where(n => (n.UserId == userId || n.UserId == null) && !n.IsRead)
                    .ToListAsync();

                foreach (var notification in notifications)
                {
                    notification.IsRead = true;
                }

                _context.Notifications.UpdateRange(notifications);
                await _context.SaveChangesAsync();

                return Ok(new { message = $"{notifications.Count} notifications marked as read" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error: " + ex.Message });
            }
        }

        // DELETE: Xóa thông báo
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteNotification(int id)
        {
            try
            {
                var notification = await _context.Notifications.FindAsync(id);
                if (notification == null)
                    return NotFound(new { message = "Notification not found" });

                _context.Notifications.Remove(notification);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Notification deleted" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error: " + ex.Message });
            }
        }

        // POST: Tạo thông báo (cho staff và admin)
        [HttpPost]
        public async Task<IActionResult> CreateNotification([FromBody] CreateNotificationRequest request)
        {
            if (string.IsNullOrEmpty(request.Title) || string.IsNullOrEmpty(request.Message))
            {
                return BadRequest(new { message = "Title and message are required" });
            }

            try
            {
                var notification = new Notification
                {
                    UserId = request.UserId,
                    Title = request.Title,
                    Message = request.Message,
                    Type = request.Type ?? "system",
                    Channel = request.Channel ?? "IN_APP",
                    Status = "SENT",
                    CreatedDate = DateTime.UtcNow,
                    SentAt = DateTime.UtcNow,
                    IsRead = false
                };

                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Notification created successfully", notificationId = notification.NotificationId });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error: " + ex.Message });
            }
        }

        // GET: Lấy danh sách users (exclude admin users)
        [HttpGet("users")]
        public async Task<IActionResult> GetUsers()
        {
            try
            {
                // Get all users except ADMIN role
                var users = await _context.Users
                    .Include(u => u.Role)
                    .Where(u => u.Role.RoleName != "ADMIN" && u.Role.RoleName != null)
                    .Select(u => new
                    {
                        userId = u.UserId,
                        username = u.Username,
                        email = u.Email,
                        phone = u.Phone,
                        role = u.Role.RoleName
                    })
                    .ToListAsync();

                return Ok(users);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error: " + ex.Message });
            }
        }

        // GET: Get user by ID
        [HttpGet("users/{userId}")]
        public async Task<IActionResult> GetUserById(int userId)
        {
            try
            {
                var user = await _context.Users
                    .Include(u => u.Role)
                    .FirstOrDefaultAsync(u => u.UserId == userId);

                if (user == null)
                {
                    return NotFound(new { message = "User not found" });
                }

                var userData = new
                {
                    userId = user.UserId,
                    username = user.Username,
                    email = user.Email,
                    phone = user.Phone,
                    role = user.Role?.RoleName,
                    fullName = user.Username // Fallback to username if no explicit fullName
                };

                return Ok(userData);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error: " + ex.Message });
            }
        }

        // ===== ADMIN NOTIFICATION ENDPOINTS =====
        
        // GET: Lấy danh sách customers
        [HttpGet("admin/customers")]
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
        [HttpGet("admin/staff")]
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

        // POST: Gửi thông báo (admin send)
        [HttpPost("admin/send")]
        public async Task<IActionResult> AdminSendNotification([FromBody] AdminNotificationRequest request)
        {
            if (string.IsNullOrEmpty(request.Title) || string.IsNullOrEmpty(request.Message))
            {
                return BadRequest(new { success = false, message = "Title and message are required" });
            }

            try
            {
                List<int> userIds = new List<int>();

                // Xác định danh sách người nhận dựa trên RecipientType
                if (request.RecipientType == "all_users")
                {
                    userIds = await _context.Users
                        .Where(u => u.Status == "ACTIVE" || u.Status == null)
                        .Select(u => u.UserId)
                        .ToListAsync();
                }
                else if (request.RecipientType == "all_customers")
                {
                    userIds = await _context.Customers
                        .Include(c => c.User)
                        .Where(c => c.User!.Status == "ACTIVE" || c.User!.Status == null)
                        .Select(c => c.User!.UserId)
                        .Distinct()
                        .ToListAsync();
                }
                else if (request.RecipientType == "all_staff")
                {
                    userIds = await _context.Staff
                        .Include(s => s.User)
                        .Where(s => (s.User.Status == "ACTIVE" || s.User.Status == null) && (s.IsActive == true || s.IsActive == null))
                        .Select(s => s.User.UserId)
                        .Distinct()
                        .ToListAsync();
                }
                else if (request.RecipientType == "specific" && request.UserId.HasValue)
                {
                    userIds.Add(request.UserId.Value);
                }
                else
                {
                    userIds = new List<int>();
                }

                int createdCount = 0;

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

        // GET: Lấy notification history (admin)
        [HttpGet("admin/history")]
        public async Task<IActionResult> GetAdminHistory(int pageSize = 20, int pageNumber = 1)
        {
            try
            {
                var query = _context.Notifications.AsQueryable();
                var total = await query.CountAsync();
                var notifications = await query
                    .OrderByDescending(n => n.CreatedDate)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .Include(n => n.User)
                    .ToListAsync();

                return Ok(new { total, notifications });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error: " + ex.Message });
            }
        }
    }

    public class CreateNotificationRequest
    {
        public int? UserId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string? Type { get; set; }
        public string? Channel { get; set; }
    }

    public class AdminNotificationRequest
    {
        public string? RecipientType { get; set; } = "all_users";
        public int? UserId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string? Type { get; set; }
        public string? Channel { get; set; }
    }
}
