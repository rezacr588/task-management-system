namespace TodoApi.Application.DTOs {
    public class TodoItemDto
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public bool IsComplete { get; set; }
        public DateTime DueDate { get; set; }
        public DateTime? CompletedDate { get; set; }
        public PriorityLevelDto Priority { get; set; }
        public int? AssignedToUserId { get; set; }
        // Add other properties that are relevant for client-side consumption
    }

    public enum PriorityLevelDto
    {
        Low,
        Medium,
        High,
        Critical
        // The enum can be identical to your domain model or modified to suit client needs
    }

}
