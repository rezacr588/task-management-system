using System.Linq.Expressions;
using TodoApi.Application.DTOs;
using TodoApi.Domain.Interfaces;

namespace TodoApi.Application.Services
{
    public class TodoItemService: ITodoItemService
    {
        private readonly IRepository<TodoItemDto> _todoItemRepository;

        public TodoItemService(IRepository<TodoItemDto> todoItemRepository)
        {
            _todoItemRepository = todoItemRepository;
        }

        public async Task<TodoItemDto> CreateTodoItemAsync(TodoItemDto todoItem)
        {
            await _todoItemRepository.AddAsync(todoItem);
            return todoItem;
        }

        public async Task<TodoItemDto> GetTodoItemByIdAsync(int id)
        {
            return await _todoItemRepository.GetByIdAsync(id);
        }

        public async Task<IEnumerable<TodoItemDto>> GetAllTodoItemsAsync(Expression<Func<TodoItemDto, bool>> filter)
        {
            return await _todoItemRepository.FindAsync(filter);
        }

        public async Task UpdateTodoItemAsync(int id, TodoItemDto updatedTodoItem)
        {
            var todoItem = await _todoItemRepository.GetByIdAsync(id);
            if (todoItem == null)
            {
                throw new KeyNotFoundException("Todo item not found.");
            }

            // Map the updated properties here
            todoItem.Title = updatedTodoItem.Title;
            // ... other property mappings

            _todoItemRepository.Update(todoItem);
        }

        public async Task DeleteTodoItemAsync(int id)
        {
            var todoItem = await _todoItemRepository.GetByIdAsync(id);
            if (todoItem == null)
            {
                throw new KeyNotFoundException("Todo item not found.");
            }

            await _todoItemRepository.RemoveAsync(todoItem);
        }

        public async Task MarkTodoItemCompleteAsync(int id, bool isComplete)
        {
            var todoItem = await _todoItemRepository.GetByIdAsync(id);
            if (todoItem == null)
            {
                throw new KeyNotFoundException("Todo item not found.");
            }

            todoItem.IsComplete = isComplete;
            todoItem.CompletedDate = isComplete ? DateTime.UtcNow : null;

            _todoItemRepository.Update(todoItem);
        }
    }
}
