using System.ComponentModel.DataAnnotations;

namespace TodoApi.Application.DTOs
{
    public class UserDto
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Role { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }
        public DateTime? LastUpdatedAt { get; set; }

        // This could be a list of TodoItem DTOs if you want to include information about assigned todo items.
        // For simplicity, only the IDs of the TodoItems are included here.
        public ICollection<int> AssignedTodoItemIds { get; set; } = new HashSet<int>();
    }
}
