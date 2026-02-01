using Microsoft.AspNetCore.Mvc;
using VehicleInsuranceAPI.Data;
using Microsoft.EntityFrameworkCore;

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
    }
}
