using System.ComponentModel.DataAnnotations;

namespace VehicleInsuranceAPI.Backend.LoginUserManagement
{
    public class RegisterRequest
    {
        [Required]
        public string Username { get; set; } = null!;

        [Required]
        public string Password { get; set; } = null!;

        [Required]
        public string Email { get; set; } = null!;

        [Required]
        public string CustomerName { get; set; } = null!;

        [Required]
        public string Phone { get; set; } = null!;

        public string? Address { get; set; }
    }
}
