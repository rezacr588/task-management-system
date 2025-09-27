using System.ComponentModel.DataAnnotations;

namespace TodoApi.Application.DTOs
{
    public class UserRegistrationDto
    {
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(100, MinimumLength = 6)]
        public string Password { get; set; } = string.Empty;

        [Required]
        public string BiometricToken { get; set; } = string.Empty;

        [Required]
        public string Role { get; set; } = string.Empty;
    }
}
