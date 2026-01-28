namespace VehicleInsuranceAPI.Backend.LoginUserManagement
{
    public class RegisterResponse
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public int? UserId { get; set; }
        public int? CustomerId { get; set; }
    }
}
