using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VehicleInsuranceAPI.Data;
using VehicleInsuranceAPI.Models;

namespace VehicleInsuranceAPI.Backend.FeedbackManagement
{
    [Route("api/[controller]")]
    [ApiController]
    public class FeedbackController : ControllerBase
    {
        private readonly VehicleInsuranceContext _context;

        public FeedbackController(VehicleInsuranceContext context)
        {
            _context = context;
        }

        // ===== CUSTOMER ENDPOINTS =====

        // POST: api/Feedback/submit - Customer gửi feedback
        [HttpPost("submit")]
        public async Task<IActionResult> SubmitFeedback([FromBody] FeedbackSubmitDto dto)
        {
            try
            {
                // Validate input
                if (dto == null || dto.CustomerId <= 0)
                {
                    return BadRequest(new { message = "Invalid customer ID" });
                }

                if (string.IsNullOrWhiteSpace(dto.Content))
                {
                    return BadRequest(new { message = "Feedback content is required" });
                }

                if (dto.Rating < 1 || dto.Rating > 5)
                {
                    return BadRequest(new { message = "Rating must be between 1 and 5" });
                }

                // Check if customer exists
                var customer = await _context.Customers.FindAsync(dto.CustomerId);
                if (customer == null)
                {
                    return BadRequest(new { message = "Customer not found" });
                }

                // Kiểm tra customer đã có feedback chưa
                var existingFeedback = await _context.Feedbacks
                    .FirstOrDefaultAsync(f => f.CustomerId == dto.CustomerId);

                if (existingFeedback != null)
                {
                    return BadRequest(new { message = "You already have a feedback. Delete the existing one to submit a new feedback." });
                }

                // Tạo feedback mới
                var feedback = new Feedback
                {
                    CustomerId = dto.CustomerId,
                    Content = dto.Content,
                    Rating = dto.Rating,
                    CreatedDate = DateTime.Now,
                    IsPinned = false
                };

                _context.Feedbacks.Add(feedback);
                await _context.SaveChangesAsync();

                // AUTO-CREATE TESTIMONIAL IF RATING >= 4
                if (dto.Rating >= 4)
                {
                    var testimonial = new Testimonial
                    {
                        CustomerId = dto.CustomerId,
                        Content = dto.Content,
                        Rating = dto.Rating,
                        Status = "Pending", // Will be reviewed by admin/staff
                        CreatedDate = DateTime.Now
                    };
                    _context.Testimonials.Add(testimonial);
                    await _context.SaveChangesAsync();
                }

                return Ok(new { message = "Feedback submitted successfully", feedbackId = feedback.FeedbackId });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error submitting feedback: " + ex.Message });
            }
        }

        // GET: api/Feedback/my-feedback/by-user/{userId} - Customer lấy feedback của mình dựa vào UserId
        [HttpGet("my-feedback/by-user/{userId}")]
        public async Task<IActionResult> GetMyFeedbackByUserId(int userId)
        {
            try
            {
                // Tìm customer dựa vào userId
                var customer = await _context.Customers
                    .FirstOrDefaultAsync(c => c.UserId == userId);

                if (customer == null)
                {
                    return Ok(new
                    {
                        feedbackId = (int?)null,
                        customerId = (int?)null,
                        customerName = (string?)null,
                        content = (string?)null,
                        rating = (int?)null,
                        createdDate = (DateTime?)null,
                        isPinned = false,
                        message = "Customer not found"
                    });
                }

                // Tìm feedback của customer
                var feedback = await _context.Feedbacks
                    .Include(f => f.Customer)
                    .FirstOrDefaultAsync(f => f.CustomerId == customer.CustomerId);

                if (feedback == null)
                {
                    return Ok(new
                    {
                        feedbackId = (int?)null,
                        customerId = customer.CustomerId,
                        customerName = customer.CustomerName,
                        content = (string?)null,
                        rating = (int?)null,
                        createdDate = (DateTime?)null,
                        isPinned = false,
                        message = "No feedback found"
                    });
                }

                return Ok(new
                {
                    feedbackId = feedback.FeedbackId,
                    customerId = feedback.CustomerId,
                    customerName = feedback.Customer?.CustomerName,
                    content = feedback.Content,
                    rating = feedback.Rating,
                    createdDate = feedback.CreatedDate,
                    isPinned = feedback.IsPinned
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error fetching feedback: " + ex.Message });
            }
        }

        // GET: api/Feedback/my-feedback/{customerId} - Customer lấy feedback của mình
        [HttpGet("my-feedback/{customerId}")]
        public async Task<IActionResult> GetMyFeedback(int customerId)
        {
            try
            {
                var feedback = await _context.Feedbacks
                    .Include(f => f.Customer)
                    .FirstOrDefaultAsync(f => f.CustomerId == customerId);

                if (feedback == null)
                {
                    return Ok(new
                    {
                        feedbackId = (int?)null,
                        customerId = customerId,
                        customerName = (string?)null,
                        content = (string?)null,
                        rating = (int?)null,
                        createdDate = (DateTime?)null,
                        isPinned = false,
                        message = "No feedback found"
                    });
                }

                return Ok(new
                {
                    feedbackId = feedback.FeedbackId,
                    customerId = feedback.CustomerId,
                    customerName = feedback.Customer?.CustomerName,
                    content = feedback.Content,
                    rating = feedback.Rating,
                    createdDate = feedback.CreatedDate,
                    isPinned = feedback.IsPinned
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error fetching feedback: " + ex.Message });
            }
        }

        // DELETE: api/Feedback/{feedbackId} - Customer xóa feedback của mình
        [HttpDelete("{feedbackId}")]
        public async Task<IActionResult> DeleteFeedback(int feedbackId)
        {
            try
            {
                var feedback = await _context.Feedbacks.FindAsync(feedbackId);

                if (feedback == null)
                {
                    return NotFound(new { message = "Feedback not found" });
                }

                // Nếu feedback này đang được hiển thị ở testimonials, xóa nó cũng
                var testimonial = await _context.Testimonials
                    .FirstOrDefaultAsync(t => t.CustomerId == feedback.CustomerId);
                if (testimonial != null)
                {
                    _context.Testimonials.Remove(testimonial);
                }

                _context.Feedbacks.Remove(feedback);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Feedback deleted successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error deleting feedback: " + ex.Message });
            }
        }

        // ===== ADMIN/STAFF ENDPOINTS =====

        // GET: api/Feedback/list - Admin/Staff xem tất cả feedback
        [HttpGet("list")]
        public async Task<IActionResult> GetAllFeedbacks([FromQuery] int? ratingFilter = null, [FromQuery] string search = "", [FromQuery] bool pinnedOnly = false)
        {
            try
            {
                var query = _context.Feedbacks
                    .Include(f => f.Customer)
                    .AsQueryable();

                // Filter by rating
                if (ratingFilter.HasValue)
                {
                    query = query.Where(f => f.Rating == ratingFilter);
                }

                // Search by customer name or content
                if (!string.IsNullOrEmpty(search))
                {
                    query = query.Where(f =>
                        (f.Customer != null && f.Customer.CustomerName != null && f.Customer.CustomerName.Contains(search)) ||
                        (f.Content != null && f.Content.Contains(search))
                    );
                }

                // Filter pinned
                if (pinnedOnly)
                {
                    query = query.Where(f => f.IsPinned);
                }

                var feedbacks = await query
                    .OrderByDescending(f => f.IsPinned)
                    .ThenByDescending(f => f.CreatedDate)
                    .ToListAsync();

                var result = feedbacks.Select((f, index) => new
                {
                    stt = index + 1,
                    feedbackId = f.FeedbackId,
                    customerName = f.Customer?.CustomerName,
                    rating = f.Rating,
                    content = f.Content,
                    createdDate = f.CreatedDate,
                    isPinned = f.IsPinned
                }).ToList();

                return Ok(new { total = result.Count, feedbacks = result });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error fetching feedbacks: " + ex.Message });
            }
        }

        // PATCH: api/Feedback/{feedbackId}/pin - Pin/Unpin feedback
        [HttpPatch("{feedbackId}/pin")]
        public async Task<IActionResult> TogglePinFeedback(int feedbackId)
        {
            try
            {
                var feedback = await _context.Feedbacks.FindAsync(feedbackId);

                if (feedback == null)
                {
                    return NotFound(new { message = "Feedback not found" });
                }

                feedback.IsPinned = !feedback.IsPinned;
                _context.Feedbacks.Update(feedback);
                await _context.SaveChangesAsync();

                return Ok(new { message = feedback.IsPinned ? "Feedback pinned successfully" : "Feedback unpinned successfully", isPinned = feedback.IsPinned });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error toggling pin status: " + ex.Message });
            }
        }

        // GET: api/Feedback/{feedbackId}/detail - Xem chi tiết feedback
        [HttpGet("{feedbackId}/detail")]
        public async Task<IActionResult> GetFeedbackDetail(int feedbackId)
        {
            try
            {
                var feedback = await _context.Feedbacks
                    .Include(f => f.Customer)
                    .FirstOrDefaultAsync(f => f.FeedbackId == feedbackId);

                if (feedback == null)
                {
                    return NotFound(new { message = "Feedback not found" });
                }

                return Ok(new
                {
                    feedbackId = feedback.FeedbackId,
                    customerName = feedback.Customer?.CustomerName,
                    customerId = feedback.CustomerId,
                    rating = feedback.Rating,
                    content = feedback.Content,
                    createdDate = feedback.CreatedDate,
                    isPinned = feedback.IsPinned
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error fetching feedback detail: " + ex.Message });
            }
        }
    }

    // DTO classes
    public class FeedbackSubmitDto
    {
        public int CustomerId { get; set; }
        public string? Content { get; set; }
        public int Rating { get; set; }
    }
}
