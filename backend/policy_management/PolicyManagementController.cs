using Microsoft.AspNetCore.Mvc;

namespace VehicleInsuranceAPI.Backend.PolicyManagement
{
    [ApiController]
    [Route("api/[controller]")]
    public class PolicyManagementController : ControllerBase
    {
        [HttpGet("demo")]
        public IActionResult Demo()
        {
            return Ok("Policy Management demo endpoint");
        }
    }
}
