using Microsoft.AspNetCore.Mvc;

namespace VehicleInsuranceAPI.Backend.ReportAnalytics
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReportAnalyticsController : ControllerBase
    {
        [HttpGet("demo")]
        public IActionResult Demo()
        {
            return Ok("Report & Analytics demo endpoint");
        }
    }
}
