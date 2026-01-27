using Microsoft.AspNetCore.Mvc;

namespace VehicleInsuranceAPI.Backend.BillingPayment
{
    [ApiController]
    [Route("api/[controller]")]
    public class BillingPaymentController : ControllerBase
    {
        [HttpGet("demo")]
        public IActionResult Demo()
        {
            return Ok("Billing & Payment demo endpoint");
        }
    }
}
