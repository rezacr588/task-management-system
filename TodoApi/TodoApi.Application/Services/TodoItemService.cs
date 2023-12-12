using System.Linq.Expressions;
using TodoApi.Application.DTOs;
using TodoApi.Domain.Interfaces;
using AutoMapper;
using TodoApi.Domain.Entities;

namespace TodoApi.Application.Services
{
    public class TodoItemService: ITodoItemService
    {
        private readonly ITodoItemRepository _todoItemRepository;
        private readonly IMapper _mapper;

        public TodoItemService(ITodoItemRepository todoItemRepository, IMapper mapper)
        {
            _todoItemRepository = todoItemRepository;
            _mapper = mapper;
        }

        public async Task<TodoItemDto> CreateTodoItemAsync(TodoItemDto todoItem)
        {
            var tItem = _mapper.Map<TodoItem>(todoItem);
            await _todoItemRepository.AddAsync(tItem);
            return _mapper.Map<TodoItemDto>(tItem);
        }

        public async Task<TodoItemDto> GetTodoItemByIdAsync(int id)
        {
            var todoItem = await _todoItemRepository.GetByIdAsync(id);
            if (todoItem == null)
            {
                throw new KeyNotFoundException("Todo item not found.");
            }

            return _mapper.Map<TodoItemDto>(todoItem);
        }

        public async Task<IEnumerable<TodoItemDto>> GetAllTodoItemsAsync(Expression<Func<TodoItemDto, bool>> filter)
        {
            var todoItems = await _todoItemRepository.GetAllAsync();
            return _mapper.Map<IEnumerable<TodoItemDto>>(todoItems);
        }

        public async Task UpdateTodoItemAsync(int id, TodoItemDto updatedTodoItem)
        {
            var todoItem = await _todoItemRepository.GetByIdAsync(id);
            if (todoItem == null)
            {
                throw new KeyNotFoundException("Todo item not found.");
            }

            todoItem.Title = updatedTodoItem.Title;
            todoItem.Description = updatedTodoItem.Description;
            todoItem.IsComplete = updatedTodoItem.IsComplete;
            todoItem.CompletedDate = updatedTodoItem.IsComplete ? DateTime.UtcNow : null;

            _todoItemRepository.UpdateAsync(todoItem);
        }

        public async Task DeleteTodoItemAsync(int id)
        {
            var todoItem = await _todoItemRepository.GetByIdAsync(id);
            if (todoItem == null)
            {
                throw new KeyNotFoundException("Todo item not found.");
            }

            await _todoItemRepository.DeleteAsync(todoItem);
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

            _todoItemRepository.UpdateAsync(todoItem);
        }
    }
}
