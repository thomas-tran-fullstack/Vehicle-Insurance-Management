using Microsoft.AspNetCore.Mvc;

namespace VehicleInsuranceAPI.Backend.ClaimManagement
{
    [ApiController]
    [Route("api/[controller]")]
    public class ClaimManagementController : ControllerBase
    {
        [HttpGet("demo")]
        public IActionResult Demo()
        {
            return Ok("Claim Management demo endpoint");
        }
    }
}
