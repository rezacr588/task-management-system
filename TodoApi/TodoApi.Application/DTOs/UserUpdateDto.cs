using System.ComponentModel.DataAnnotations;

namespace TodoApi.Application.DTOs
{
    public class UserUpdateDto
    {
        [Range(1, int.MaxValue)]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        // Consider handling password updates separately for security
        [StringLength(100, MinimumLength = 6)]
        public string Password { get; set; } = string.Empty;

        [Required]
        public string BiometricToken { get; set; } = string.Empty;

        [Required]
        public string Role { get; set; } = string.Empty;
    }
}
