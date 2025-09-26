using System.ComponentModel.DataAnnotations;

namespace TodoApi.Domain.Entities
 {
    public class User
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public required string Name { get; set; }

        [Required]
        [EmailAddress]
        public required string Email { get; set; }

        public required string PasswordHash { get; set; }

        public required string BiometricToken { get; set; }

        public required string Role { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastUpdatedAt { get; set; }

        public virtual ICollection<TodoItem> AssignedTodoItems { get; set; } = new HashSet<TodoItem>();
        public virtual ICollection<Comment> AuthoredComments { get; set; } = new HashSet<Comment>();
        public virtual ICollection<ActivityLogEntry> AuthoredActivityLogEntries { get; set; } = new HashSet<ActivityLogEntry>();
    }

 }
