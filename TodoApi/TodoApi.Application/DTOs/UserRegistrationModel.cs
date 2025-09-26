namespace TodoApi.Application.DTOs
{
    public class UserRegistrationDto
    {
        // Properties
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string BiometricToken { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        // Other properties as needed
    }
}
