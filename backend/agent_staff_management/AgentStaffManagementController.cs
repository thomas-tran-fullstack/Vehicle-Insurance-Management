using Microsoft.AspNetCore.Mvc;

namespace VehicleInsuranceAPI.Backend.AgentStaffManagement
{
    [ApiController]
    [Route("api/[controller]")]
    public class AgentStaffManagementController : ControllerBase
    {
        [HttpGet("demo")]
        public IActionResult Demo()
        {
            return Ok("Agent/Staff Management demo endpoint");
        }
    }
}
