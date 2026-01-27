using Microsoft.AspNetCore.Mvc;

namespace VehicleInsuranceAPI.Backend.PenaltyViolation
{
    [ApiController]
    [Route("api/[controller]")]
    public class PenaltyViolationController : ControllerBase
    {
        [HttpGet("demo")]
        public IActionResult Demo()
        {
            return Ok("Penalty & Violation Management demo endpoint");
        }
    }
}
