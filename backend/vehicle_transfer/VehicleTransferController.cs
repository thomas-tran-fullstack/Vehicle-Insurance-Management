using Microsoft.AspNetCore.Mvc;

namespace VehicleInsuranceAPI.Backend.VehicleTransfer
{
    [ApiController]
    [Route("api/[controller]")]
    public class VehicleTransferController : ControllerBase
    {
        [HttpGet("demo")]
        public IActionResult Demo()
        {
            return Ok("Vehicle Transfer Management demo endpoint");
        }
    }
}
