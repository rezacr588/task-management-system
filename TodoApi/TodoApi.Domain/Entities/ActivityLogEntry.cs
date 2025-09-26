using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TodoApi.Domain.Enums;

namespace TodoApi.Domain.Entities
{
    public class ActivityLogEntry
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [ForeignKey(nameof(TodoItem))]
        public int TodoItemId { get; set; }

        public virtual required TodoItem TodoItem { get; set; }

        [ForeignKey(nameof(Actor))]
        public int? ActorId { get; set; }

        public virtual User? Actor { get; set; }

        [Required]
        [MaxLength(256)]
        public required string Summary { get; set; }

        [MaxLength(4000)]
        public string? Details { get; set; }

        public ActivityEventType EventType { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public int? RelatedCommentId { get; set; }

        public virtual Comment? RelatedComment { get; set; }
    }
}
