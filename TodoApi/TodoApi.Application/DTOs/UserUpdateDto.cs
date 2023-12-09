namespace TodoApi.Application.DTOs
{
    public class UserUpdateDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }

        // Consider handling password updates separately for security
        public string Password { get; set; }

        // BiometricToken might not typically be updated, but included for completeness
        public string BiometricToken { get; set; }

        // Role might be updated depending on your application's functionality
        public string Role { get; set; }

        // Add any other properties that are allowed to be updated
    }
}
