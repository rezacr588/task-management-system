using System.ComponentModel.DataAnnotations;
using TodoApi.Domain.Enums;

namespace TodoApi.Application.DTOs
{
    public class CommentDto
    {
        public int Id { get; set; }

        [Range(1, int.MaxValue)]
        public int TodoItemId { get; set; }

        public int? AuthorId { get; set; }

        [StringLength(100)]
        public string? AuthorDisplayName { get; set; }

        [Required]
        [StringLength(4000, MinimumLength = 1)]
        public string Content { get; set; } = string.Empty;

        public bool IsSystemGenerated { get; set; }
        public ActivityEventType EventType { get; set; }
        public string? MetadataJson { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class CommentCreateRequest
    {
        [Range(1, int.MaxValue)]
        public int TodoItemId { get; set; }
        public int? AuthorId { get; set; }
        public string? AuthorDisplayName { get; set; }
        [Required]
        [MaxLength(4000)]
        public string? Content { get; set; }
        public bool IsSystemGenerated { get; set; }
        public ActivityEventType EventType { get; set; } = ActivityEventType.CommentCreated;
        public string? MetadataJson { get; set; }
    }

    public class CommentUpdateRequest
    {
        [MaxLength(4000)]
        public string Content { get; set; } = string.Empty;
        public bool? IsSystemGenerated { get; set; }
        public ActivityEventType? EventType { get; set; }
        public string? MetadataJson { get; set; }
    }
}
