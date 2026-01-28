namespace VehicleInsuranceAPI.Backend.LoginUserManagement
{
    public class LoginResponse
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public int? UserId { get; set; }
        public string? Username { get; set; }
        public int? RoleId { get; set; }
        public string? RoleName { get; set; }
        public string? CustomerName { get; set; }
        public string? Avatar { get; set; }
        public string? FullName { get; set; }
    }
}
