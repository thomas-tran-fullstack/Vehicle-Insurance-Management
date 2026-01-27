using Microsoft.AspNetCore.Mvc;

namespace VehicleInsuranceAPI.Backend.CompanyExpenses
{
    [ApiController]
    [Route("api/[controller]")]
    public class CompanyExpensesController : ControllerBase
    {
        [HttpGet("demo")]
        public IActionResult Demo()
        {
            return Ok("Company Expenses demo endpoint");
        }
    }
}
