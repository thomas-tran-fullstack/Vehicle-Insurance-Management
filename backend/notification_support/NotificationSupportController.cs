using Microsoft.AspNetCore.Mvc;

namespace VehicleInsuranceAPI.Backend.NotificationSupport
{
    [ApiController]
    [Route("api/[controller]")]
    public class NotificationSupportController : ControllerBase
    {
        [HttpGet("demo")]
        public IActionResult Demo()
        {
            return Ok("Notification & Support Management demo endpoint");
        }
    }
}
