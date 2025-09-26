using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TodoApi.Domain.Enums;

namespace TodoApi.Domain.Entities
{
    public class Comment
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [ForeignKey(nameof(TodoItem))]
        public int TodoItemId { get; set; }

        public virtual TodoItem? TodoItem { get; set; }

        [ForeignKey(nameof(Author))]
        public int? AuthorId { get; set; }

        public virtual User? Author { get; set; }

        [Required]
        [MaxLength(4000)]
        public required string Content { get; set; }

        [MaxLength(200)]
        public string? AuthorDisplayName { get; set; }

        public bool IsSystemGenerated { get; set; }

        public ActivityEventType EventType { get; set; } = ActivityEventType.CommentCreated;

        [MaxLength(2000)]
        public string? MetadataJson { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }
    }
}
