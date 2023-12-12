namespace TodoApi.Domain.DTOs
{
    public class UserDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string Role { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastUpdatedAt { get; set; }

        // This could be a list of TodoItem DTOs if you want to include information about assigned todo items.
        // For simplicity, only the IDs of the TodoItems are included here.
        public ICollection<int> AssignedTodoItemIds { get; set; } = new HashSet<int>();
    }
}
