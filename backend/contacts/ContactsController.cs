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
                contact.CreatedDate = DateTime.Now; // Tự động lấy ngày giờ hiện tại
                _context.Contacts.Add(contact);
                await _context.SaveChangesAsync();
                return Ok(new { message = "Gửi thành công!" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi server: " + ex.Message });
            }
        }

        // 2. API Xem danh sách (Admin dùng)
        // GET: api/Contacts
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Contact>>> GetContacts()
        {
            return await _context.Contacts.OrderByDescending(c => c.CreatedDate).ToListAsync();
        }
    }
}