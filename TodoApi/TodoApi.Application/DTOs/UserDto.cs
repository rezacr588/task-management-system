namespace TodoApi.Domain.DTOs
{
    public class UserDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? LastUpdatedAt { get; set; }

        // This could be a list of TodoItem DTOs if you want to include information about assigned todo items.
        // For simplicity, only the IDs of the TodoItems are included here.
        public ICollection<int> AssignedTodoItemIds { get; set; } = new HashSet<int>();
    }
}
