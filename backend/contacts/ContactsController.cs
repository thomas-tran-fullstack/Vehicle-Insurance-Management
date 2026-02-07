using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VehicleInsuranceAPI.Data;
using VehicleInsuranceAPI.Models;

namespace VehicleInsuranceAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ContactsController : ControllerBase
    {
        private readonly VehicleInsuranceContext _context;

        public ContactsController(VehicleInsuranceContext context)
        {
            _context = context;
        }

        // 1. API Gửi liên hệ (Khách hàng dùng)
        // POST: api/Contacts
        [HttpPost]
        public async Task<ActionResult<Contact>> CreateContact(Contact contact)
        {
            try
            {
                // Set CreatedDate if not provided
                if (contact.CreatedDate == null || contact.CreatedDate == DateTime.MinValue)
                {
                    contact.CreatedDate = DateTime.UtcNow;
                }

                // Validate required fields
                if (string.IsNullOrWhiteSpace(contact.Name) || string.IsNullOrWhiteSpace(contact.Email))
                {
                    return BadRequest(new { message = "Name and Email are required" });
                }

                _context.Contacts.Add(contact);
                await _context.SaveChangesAsync();
                return Ok(new { message = "Contact created successfully!", contactId = contact.ContactId });
            }
            catch (DbUpdateException dbEx)
            {
                // Log the exact database error
                var innerException = dbEx.InnerException?.Message ?? dbEx.Message;
                return StatusCode(500, new { message = "Database error: " + innerException });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Server error: " + ex.Message });
            }
        }

        // 2. API Xem danh sách (Admin dùng)
        // GET: api/Contacts
        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> GetContacts()
        {
            try
            {
                var contacts = await _context.Contacts
                    .Include(c => c.Category)
                    .Select(c => new
                    {
                        c.ContactId,
                        CustomerName = c.Name ?? "Unknown",
                        Email = c.Email ?? "",
                        Subject = c.Subject ?? "Support Request",
                        c.Message,
                        c.CreatedDate,
                        CategoryId = c.CategoryId,
                        CategoryName = c.Category != null ? c.Category.CategoryName : "General Inquiry",
                        Status = c.Status ?? "Open",
                        IsResolved = c.Status == "Resolved",
                        c.UserId
                    })
                    .OrderByDescending(x => x.CreatedDate)
                    .ToListAsync();

                return Ok(contacts);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error loading contacts: " + ex.Message });
            }
        }

        // 3. API Cập nhật trạng thái ticket
        // PUT: api/Contacts/{id}/status
        [HttpPut("{id}/status")]
        public async Task<IActionResult> UpdateContactStatus(int id, [FromBody] UpdateStatusRequest request)
        {
            try
            {
                var contact = await _context.Contacts.FindAsync(id);
                if (contact == null)
                {
                    return NotFound(new { message = "Contact not found" });
                }

                contact.Status = request.Status ?? "Open";
                _context.Contacts.Update(contact);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Status updated successfully", status = contact.Status });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error updating status: " + ex.Message });
            }
        }
    }

    // Helper class for status update request
    public class UpdateStatusRequest
    {
        public string? Status { get; set; }
    }
}