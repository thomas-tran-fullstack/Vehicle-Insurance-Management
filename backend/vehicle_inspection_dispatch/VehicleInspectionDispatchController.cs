using Microsoft.AspNetCore.Mvc;

namespace VehicleInsuranceAPI.Backend.VehicleInspectionDispatch
{
    [ApiController]
    [Route("api/[controller]")]
    public class VehicleInspectionDispatchController : ControllerBase
    {
        [HttpGet("demo")]
        public IActionResult Demo()
        {
            return Ok("Vehicle Inspection Dispatch demo endpoint");
        }
    }
}
