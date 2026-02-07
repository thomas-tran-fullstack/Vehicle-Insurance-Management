using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VehicleInsuranceAPI.Data;
using VehicleInsuranceAPI.Models;
using System;
using System.Threading.Tasks;
using System.Linq;

namespace VehicleInsuranceAPI.Controllers
{
    [Route("api/faq")]
    [ApiController]
    public class FaqController : ControllerBase
    {
        private readonly VehicleInsuranceContext _context;

        public FaqController(VehicleInsuranceContext context)
        {
            _context = context;
        }

        // GET: Lấy danh sách FAQ công khai
        [HttpGet]
        public async Task<IActionResult> GetPublicFaqs()
        {
            try
            {
                var faqs = await _context.Faqs
                    .Where(f => f.IsActive == true)
                    .OrderBy(f => f.FaqId)
                    .Select(f => new
                    {
                        f.FaqId,
                        f.Question,
                        f.Answer
                    })
                    .ToListAsync();

                return Ok(faqs);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error: " + ex.Message });
            }
        }

        // GET: Lấy danh sách FAQ cho admin (kể cả không public)
        [HttpGet("admin")]
        public async Task<IActionResult> GetAllFaqs(int pageSize = 20, int pageNumber = 1)
        {
            try
            {
                var total = await _context.Faqs.CountAsync();
                var faqs = await _context.Faqs
                    .OrderBy(f => f.FaqId)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                return Ok(new { total, faqs });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error: " + ex.Message });
            }
        }

        // GET: Lấy FAQ theo ID
        [HttpGet("{id}")]
        public async Task<IActionResult> GetFaq(int id)
        {
            try
            {
                var faq = await _context.Faqs.FindAsync(id);
                if (faq == null)
                    return NotFound(new { message = "FAQ not found" });

                return Ok(faq);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error: " + ex.Message });
            }
        }

        // POST: Tạo FAQ mới
        [HttpPost]
        public async Task<IActionResult> CreateFaq([FromBody] CreateFaqRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Question) || string.IsNullOrWhiteSpace(request.Answer))
                return BadRequest(new { message = "Question and Answer are required" });

            try
            {
                var faq = new Faq
                {
                    Question = request.Question,
                    Answer = request.Answer,
                    IsActive = request.IsActive ?? true
                };

                _context.Faqs.Add(faq);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetFaq), new { id = faq.FaqId }, faq);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error: " + ex.Message });
            }
        }

        // PUT: Cập nhật FAQ
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateFaq(int id, [FromBody] CreateFaqRequest request)
        {
            try
            {
                var faq = await _context.Faqs.FindAsync(id);
                if (faq == null)
                    return NotFound(new { message = "FAQ not found" });

                if (!string.IsNullOrWhiteSpace(request.Question))
                    faq.Question = request.Question;

                if (!string.IsNullOrWhiteSpace(request.Answer))
                    faq.Answer = request.Answer;

                if (request.IsActive.HasValue)
                    faq.IsActive = request.IsActive;

                _context.Faqs.Update(faq);
                await _context.SaveChangesAsync();

                return Ok(faq);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error: " + ex.Message });
            }
        }

        // DELETE: Xóa FAQ
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteFaq(int id)
        {
            try
            {
                var faq = await _context.Faqs.FindAsync(id);
                if (faq == null)
                    return NotFound(new { message = "FAQ not found" });

                _context.Faqs.Remove(faq);
                await _context.SaveChangesAsync();

                return Ok(new { message = "FAQ deleted" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error: " + ex.Message });
            }
        }

        // PUT: Kích hoạt/Vô hiệu hóa FAQ
        [HttpPut("{id}/toggle")]
        public async Task<IActionResult> ToggleFaqStatus(int id)
        {
            try
            {
                var faq = await _context.Faqs.FindAsync(id);
                if (faq == null)
                    return NotFound(new { message = "FAQ not found" });

                faq.IsActive = !faq.IsActive;
                _context.Faqs.Update(faq);
                await _context.SaveChangesAsync();

                return Ok(new { message = $"FAQ {(faq.IsActive ?? false ? "activated" : "deactivated")}", faq });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error: " + ex.Message });
            }
        }
    }

    public class CreateFaqRequest
    {
        public string? Question { get; set; }
        public string? Answer { get; set; }
        public bool? IsActive { get; set; }
    }
}
