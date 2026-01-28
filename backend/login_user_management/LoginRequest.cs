using System.ComponentModel.DataAnnotations;

namespace VehicleInsuranceAPI.Backend.LoginUserManagement
{
    public class LoginRequest
    {
        [Required]
        public string Username { get; set; } = null!;

        [Required]
        public string Password { get; set; } = null!;
    }
}
