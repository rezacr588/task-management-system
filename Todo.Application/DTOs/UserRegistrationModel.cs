namespace TodoApi.Application.DTOs
{
    public class UserRegistrationModel
    {
        // Properties
        public string Name { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public string BiometricToken { get; set; }
        public string Role { get; set; }
        // Other properties as needed
    }
}
