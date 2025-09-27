using System.ComponentModel.DataAnnotations;

namespace TodoApi.Application.DTOs
{
    // Basic information required for listing todos
    public interface IBasicTodoInfo
    {
        int Id { get; }
        string Title { get; }
        bool IsComplete { get; }
    }

    // Detailed information for editing or detailed viewing
    public interface IDetailedTodoInfo : IBasicTodoInfo
    {
        string Description { get; }
        DateTime DueDate { get; }
        DateTime? CompletedDate { get; }
        int? AssignedToUserId { get; }
    }

    // Separate interface for handling priority-related operations
    public interface IPriorityInfo
    {
        PriorityLevelDto Priority { get; }
    }

    // Enum for priority levels
    public enum PriorityLevelDto
    {
        Low,
        Medium,
        High,
        Critical
    }

    // The TodoItemDto implementing all the interfaces
    public class TodoItemDto : IDetailedTodoInfo, IPriorityInfo
    {
        public int Id { get; set; }
        [Required]
        [StringLength(100)]
        public string Title { get; set; } = string.Empty;
        public bool IsComplete { get; set; }
        [Required]
        [StringLength(500)]
        public string Description { get; set; } = string.Empty;
        [Required]
        public DateTime DueDate { get; set; }
        public DateTime? CompletedDate { get; set; }
        [Required]
        public PriorityLevelDto Priority { get; set; }
        public int? AssignedToUserId { get; set; }
        // Additional properties and methods as needed
    }
}
