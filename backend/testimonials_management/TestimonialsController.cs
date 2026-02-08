using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VehicleInsuranceAPI.Data;
using VehicleInsuranceAPI.Models;

namespace VehicleInsuranceAPI.Backend.TestimonialsManagement
{
    [Route("api/[controller]")]
    [ApiController]
    public class TestimonialsController : ControllerBase
    {
        private readonly VehicleInsuranceContext _context;

        public TestimonialsController(VehicleInsuranceContext context)
        {
            _context = context;
        }

        // ===== CUSTOMER ENDPOINTS =====

        // GET: api/Testimonials/published - Lấy tất cả testimonials được published (rating >= 4 sao)
        [HttpGet("published")]
        public async Task<IActionResult> GetPublishedTestimonials()
        {
            try
            {
                var testimonials = await _context.Testimonials
                    .Include(t => t.Customer)
                    .Where(t => t.Status == "Published" && t.Rating >= 4)
                    .OrderByDescending(t => t.CreatedDate)
                    .ToListAsync();

                var result = testimonials.Select(t => new
                {
                    testimonialId = t.TestimonialId,
                    customerName = t.Customer?.CustomerName,
                    customerAvatar = t.Customer?.Avatar,
                    rating = t.Rating,
                    content = $"\"{t.Content}\"",
                    createdDate = t.CreatedDate?.ToString("MMM dd, yyyy"),
                    status = t.Status
                }).ToList();

                return Ok(new { total = result.Count, testimonials = result });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error fetching testimonials: " + ex.Message });
            }
        }

        // ===== ADMIN/STAFF ENDPOINTS =====

        // GET: api/Testimonials/list - Admin/Staff xem tất cả testimonials (rating >= 4)
        [HttpGet("list")]
        public async Task<IActionResult> GetAllTestimonials([FromQuery] string status = "all")
        {
            try
            {
                var query = _context.Testimonials
                    .Include(t => t.Customer)
                    .Where(t => t.Rating >= 4)
                    .AsQueryable();

                // Filter by status
                if (!string.IsNullOrEmpty(status) && status != "all")
                {
                    query = query.Where(t => t.Status == status);
                }

                var testimonials = await query
                    .OrderByDescending(t => t.CreatedDate)
                    .ToListAsync();

                var result = testimonials.Select((t, index) => new
                {
                    stt = index + 1,
                    testimonialId = t.TestimonialId,
                    customerName = t.Customer?.CustomerName,
                    rating = t.Rating,
                    content = t.Content,
                    createdDate = t.CreatedDate,
                    status = t.Status,
                    statusColor = t.Status == "Published" ? "purple" : (t.Status == "Pending" ? "yellow" : "red")
                }).ToList();

                return Ok(new { total = result.Count, testimonials = result });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error fetching testimonials: " + ex.Message });
            }
        }

        // PATCH: api/Testimonials/{testimonialId}/approve - Accept testimonial
        [HttpPatch("{testimonialId}/approve")]
        public async Task<IActionResult> ApproveTestimonial(int testimonialId)
        {
            try
            {
                var testimonial = await _context.Testimonials.FindAsync(testimonialId);

                if (testimonial == null)
                {
                    return NotFound(new { message = "Testimonial not found" });
                }

                testimonial.Status = "Published";
                _context.Testimonials.Update(testimonial);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Testimonial approved and published successfully", status = testimonial.Status });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error approving testimonial: " + ex.Message });
            }
        }

        // PATCH: api/Testimonials/{testimonialId}/reject - Reject testimonial
        [HttpPatch("{testimonialId}/reject")]
        public async Task<IActionResult> RejectTestimonial(int testimonialId)
        {
            try
            {
                var testimonial = await _context.Testimonials.FindAsync(testimonialId);

                if (testimonial == null)
                {
                    return NotFound(new { message = "Testimonial not found" });
                }

                testimonial.Status = "Denied";
                _context.Testimonials.Update(testimonial);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Testimonial rejected successfully", status = testimonial.Status });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error rejecting testimonial: " + ex.Message });
            }
        }

        // GET: api/Testimonials/{testimonialId}/detail - Xem chi tiết testimonial
        [HttpGet("{testimonialId}/detail")]
        public async Task<IActionResult> GetTestimonialDetail(int testimonialId)
        {
            try
            {
                var testimonial = await _context.Testimonials
                    .Include(t => t.Customer)
                    .FirstOrDefaultAsync(t => t.TestimonialId == testimonialId);

                if (testimonial == null)
                {
                    return NotFound(new { message = "Testimonial not found" });
                }

                return Ok(new
                {
                    testimonialId = testimonial.TestimonialId,
                    customerName = testimonial.Customer?.CustomerName,
                    customerId = testimonial.CustomerId,
                    rating = testimonial.Rating,
                    content = testimonial.Content,
                    createdDate = testimonial.CreatedDate,
                    status = testimonial.Status
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error fetching testimonial detail: " + ex.Message });
            }
        }

        // POST: api/Testimonials/create-from-feedback - Tạo testimonial từ feedback (tự động hoặc manual)
        [HttpPost("create-from-feedback")]
        public async Task<IActionResult> CreateTestimonialFromFeedback([FromBody] TestimonialFromFeedbackDto dto)
        {
            try
            {
                // Kiểm tra feedback có tồn tại và có rating >= 4 không
                var feedback = await _context.Feedbacks.FindAsync(dto.FeedbackId);

                if (feedback == null)
                {
                    return NotFound(new { message = "Feedback not found" });
                }

                if (feedback.Rating < 4)
                {
                    return BadRequest(new { message = "Feedback must have rating >= 4 stars to create testimonial" });
                }

                // Kiểm tra testimonial đã tồn tại chưa từ feedback này
                var existingTestimonial = await _context.Testimonials
                    .FirstOrDefaultAsync(t => t.CustomerId == feedback.CustomerId);

                if (existingTestimonial != null)
                {
                    return BadRequest(new { message = "Customer already has a testimonial" });
                }

                // Tạo testimonial
                var testimonial = new Testimonial
                {
                    CustomerId = feedback.CustomerId,
                    Content = feedback.Content,
                    Rating = feedback.Rating,
                    Status = "Pending",
                    CreatedDate = DateTime.Now
                };

                _context.Testimonials.Add(testimonial);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Testimonial created from feedback successfully", testimonialId = testimonial.TestimonialId });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error creating testimonial: " + ex.Message });
            }
        }
    }

    // DTO classes
    public class TestimonialFromFeedbackDto
    {
        public int FeedbackId { get; set; }
    }
}
