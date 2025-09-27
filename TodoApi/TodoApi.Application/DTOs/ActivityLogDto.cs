using System.ComponentModel.DataAnnotations;
using TodoApi.Domain.Enums;

namespace TodoApi.Application.DTOs
{
    public class ActivityLogDto
    {
        public int Id { get; set; }

        [Range(1, int.MaxValue)]
        public int TodoItemId { get; set; }

        public int? ActorId { get; set; }

        [StringLength(100)]
        public string? ActorDisplayName { get; set; }

        [Required]
        [StringLength(500)]
        public string Summary { get; set; } = string.Empty;

        [StringLength(2000)]
        public string? Details { get; set; }

        [Required]
        public ActivityEventType EventType { get; set; }

        public DateTime CreatedAt { get; set; }
        public int? RelatedCommentId { get; set; }
    }
}
