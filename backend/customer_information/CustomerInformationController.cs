using Microsoft.AspNetCore.Mvc;

namespace VehicleInsuranceAPI.Backend.CustomerInformation
{
    [ApiController]
    [Route("api/[controller]")]
    public class CustomerInformationController : ControllerBase
    {
        [HttpGet("demo")]
        public IActionResult Demo()
        {
            return Ok("Customer Information demo endpoint");
        }
    }
}
