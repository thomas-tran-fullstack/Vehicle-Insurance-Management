using Microsoft.AspNetCore.Mvc;

namespace VehicleInsuranceAPI.Backend.VehicleInformation
{
    [ApiController]
    [Route("api/[controller]")]
    public class VehicleInformationController : ControllerBase
    {
        [HttpGet("demo")]
        public IActionResult Demo()
        {
            return Ok("Vehicle Information demo endpoint");
        }
    }
}
