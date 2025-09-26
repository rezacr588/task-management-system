using TodoApi.Domain.Enums;

namespace TodoApi.Application.DTOs
{
    public class ActivityLogDto
    {
        public int Id { get; set; }
        public int TodoItemId { get; set; }
        public int? ActorId { get; set; }
        public string? ActorDisplayName { get; set; }
        public string Summary { get; set; } = string.Empty;
        public string? Details { get; set; }
        public ActivityEventType EventType { get; set; }
        public DateTime CreatedAt { get; set; }
        public int? RelatedCommentId { get; set; }
    }
}
