using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using TodoApi.Domain.Entities;
using TodoApi.Domain.Interfaces;

namespace TodoApi.Application.Services
{
    public class TodoItemService
    {
        private readonly IRepository<TodoItem> _todoItemRepository;

        public TodoItemService(IRepository<TodoItem> todoItemRepository)
        {
            _todoItemRepository = todoItemRepository;
        }

        public async Task<TodoItem> CreateTodoItemAsync(TodoItem todoItem)
        {
            await _todoItemRepository.AddAsync(todoItem);
            return todoItem;
        }

        public async Task<TodoItem> GetTodoItemByIdAsync(int id)
        {
            return await _todoItemRepository.GetByIdAsync(id);
        }

        public async Task<IEnumerable<TodoItem>> GetAllTodoItemsAsync(Expression<Func<TodoItem, bool>> filter)
        {
            return await _todoItemRepository.FindAsync(filter);
        }

        public async Task UpdateTodoItemAsync(int id, TodoItem updatedTodoItem)
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
