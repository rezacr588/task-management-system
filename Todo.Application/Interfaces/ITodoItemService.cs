using System.Collections.Generic;
using System.Threading.Tasks;
using TodoApi.Application.DTOs;
using TodoApi.Domain.Entities;

namespace TodoApi.Application.Interfaces
{
    public interface ITodoItemService
    {
        // Create a new TodoItem and return its DTO
        Task<TodoItemDto> CreateTodoItemAsync(TodoItemDto todoItemDto);

        // Get a specific TodoItem by ID and return its DTO
        Task<TodoItemDto> GetTodoItemByIdAsync(int id);

        // Get all TodoItems and return their DTOs
        Task<IEnumerable<TodoItemDto>> GetAllTodoItemsAsync();

        // Update an existing TodoItem based on the provided DTO
        Task UpdateTodoItemAsync(int id, TodoItemDto todoItemDto);

        // Delete a TodoItem by ID
        Task DeleteTodoItemAsync(int id);

        // Mark a TodoItem as complete or incomplete
        Task MarkTodoItemCompleteAsync(int id, bool isComplete);
    }
}
