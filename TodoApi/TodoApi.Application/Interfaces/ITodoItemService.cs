using System.Linq.Expressions;
using TodoApi.Application.DTOs;

namespace TodoApi.Application.Services
{
    public interface ITodoItemService
    {
        Task<TodoItemDto> CreateTodoItemAsync(TodoItemDto todoItem);
        Task<TodoItemDto> GetTodoItemByIdAsync(int id);
        Task<IEnumerable<TodoItemDto>> GetAllTodoItemsAsync(Expression<Func<TodoItemDto, bool>>? filter);
        Task<PaginatedResponse<TodoItemDto>> GetTodoItemsPaginatedAsync(int pageNumber = 1, int pageSize = 10);
        Task UpdateTodoItemAsync(int id, TodoItemDto updatedTodoItem);
        Task DeleteTodoItemAsync(int id);
        Task MarkTodoItemCompleteAsync(int id, bool isComplete);
    }
}