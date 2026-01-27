using Microsoft.AspNetCore.Mvc;

namespace VehicleInsuranceAPI.Backend.LoginUserManagement
{
    [ApiController]
    [Route("api/[controller]")]
    public class LoginUserManagementController : ControllerBase
    {
        [HttpGet("demo")]
        public IActionResult Demo()
        {
            return Ok("Login & User Management demo endpoint");
        }
    }
}
