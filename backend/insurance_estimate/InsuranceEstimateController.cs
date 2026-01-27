using Microsoft.AspNetCore.Mvc;

namespace VehicleInsuranceAPI.Backend.InsuranceEstimate
{
    [ApiController]
    [Route("api/[controller]")]
    public class InsuranceEstimateController : ControllerBase
    {
        [HttpGet("demo")]
        public IActionResult Demo()
        {
            return Ok("Insurance Estimate demo endpoint");
        }
    }
}
