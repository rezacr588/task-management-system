namespace TodoApi.Application.DTOs
{
    public class UserUpdateDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;

        // Consider handling password updates separately for security
        public string Password { get; set; } = string.Empty;

        // BiometricToken might not typically be updated, but included for completeness
        public string BiometricToken { get; set; } = string.Empty;

        // Role might be updated depending on your application's functionality
        public string Role { get; set; } = string.Empty;

        // Add any other properties that are allowed to be updated
    }
}
