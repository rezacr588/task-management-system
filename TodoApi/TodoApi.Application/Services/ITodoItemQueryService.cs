using TodoApi.Application.DTOs;

namespace TodoApi.Application.Services;

/// <summary>
/// Read-only service for TodoItem queries (CQRS Query side)
/// </summary>
public interface ITodoItemQueryService
{
    Task<TodoItemDto?> GetByIdAsync(int id);
    Task<IEnumerable<TodoItemDto>> GetByUserIdAsync(int userId, int page = 1, int pageSize = 10);
    Task<IEnumerable<TodoItemDto>> GetCompletedByUserAsync(int userId);
    Task<IEnumerable<TodoItemDto>> GetPendingByUserAsync(int userId);
    Task<IEnumerable<TodoItemDto>> SearchAsync(string searchTerm, int userId, int page = 1, int pageSize = 10);
    Task<IEnumerable<TodoItemDto>> GetByTagsAsync(int userId, string[] tags);
    Task<TodoItemStatisticsDto> GetStatisticsAsync(int userId);
    Task<IEnumerable<TodoItemDto>> GetRecentlyModifiedAsync(int userId, int count = 10);
    Task<IEnumerable<TodoItemDto>> GetOverdueAsync(int userId);
    Task<IEnumerable<TodoItemDto>> GetDueTodayAsync(int userId);
    Task<IEnumerable<TodoItemDto>> GetDueThisWeekAsync(int userId);
}

/// <summary>
/// Command service for TodoItem mutations (CQRS Command side)
/// </summary>
public interface ITodoItemCommandService
{
    Task<TodoItemDto> CreateAsync(TodoItemCreateDto createDto);
    Task<TodoItemDto> UpdateAsync(int id, TodoItemUpdateDto updateDto);
    Task<bool> DeleteAsync(int id);
    Task<bool> MarkCompleteAsync(int id);
    Task<bool> MarkIncompleteAsync(int id);
    Task<bool> AssignTagsAsync(int id, string[] tagNames);
    Task<bool> RemoveTagsAsync(int id, string[] tagNames);
    Task<bool> SetPriorityAsync(int id, int priority);
    Task<bool> SetDueDateAsync(int id, DateTime? dueDate);
    Task<TodoItemDto> DuplicateAsync(int id);
}

public class TodoItemStatisticsDto
{
    public int TotalItems { get; set; }
    public int CompletedItems { get; set; }
    public int PendingItems { get; set; }
    public double CompletionRate { get; set; }
    public DateTime? LastActivity { get; set; }
    public string[] MostUsedTags { get; set; } = Array.Empty<string>();
    public int OverdueItems { get; set; }
    public int DueTodayItems { get; set; }
    public int DueThisWeekItems { get; set; }
}

public class TodoItemCreateDto
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime? DueDate { get; set; }
    public int Priority { get; set; }
    public int UserId { get; set; }
    public string[] Tags { get; set; } = Array.Empty<string>();
}

public class TodoItemUpdateDto
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public bool? IsComplete { get; set; }
    public DateTime? DueDate { get; set; }
    public int? Priority { get; set; }
    public string[]? Tags { get; set; }
}