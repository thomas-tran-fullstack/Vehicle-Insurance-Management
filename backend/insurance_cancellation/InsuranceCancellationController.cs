using Microsoft.AspNetCore.Mvc;

namespace VehicleInsuranceAPI.Backend.InsuranceCancellation
{
    [ApiController]
    [Route("api/[controller]")]
    public class InsuranceCancellationController : ControllerBase
    {
        [HttpGet("demo")]
        public IActionResult Demo()
        {
            return Ok("Insurance Cancellation Management demo endpoint");
        }
    }
}
