using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using TodoApi.Application.DTOs;
using TodoApi.Domain.Interfaces;
using AutoMapper;
using TodoApi.Domain.Entities;
using TodoApi.Domain.Enums;

namespace TodoApi.Application.Services
{
    public class TodoItemService: ITodoItemService
    {
        private readonly ITodoItemRepository _todoItemRepository;
        private readonly IMapper _mapper;
        private readonly IActivityLogRepository _activityLogRepository;

        public TodoItemService(ITodoItemRepository todoItemRepository, IMapper mapper, IActivityLogRepository activityLogRepository)
        {
            _todoItemRepository = todoItemRepository;
            _mapper = mapper;
            _activityLogRepository = activityLogRepository;
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

            var activities = new List<ActivityLogEntry>();

            if (!string.Equals(todoItem.Title, updatedTodoItem.Title, StringComparison.Ordinal))
            {
                activities.Add(new ActivityLogEntry
                {
                    TodoItemId = todoItem.Id,
                    Summary = "Title changed",
                    Details = $"Title updated from '{todoItem.Title}' to '{updatedTodoItem.Title}'",
                    EventType = ActivityEventType.TaskUpdated
                });
                todoItem.Title = updatedTodoItem.Title;
            }

            if (!string.Equals(todoItem.Description, updatedTodoItem.Description, StringComparison.Ordinal))
            {
                activities.Add(new ActivityLogEntry
                {
                    TodoItemId = todoItem.Id,
                    Summary = "Description changed",
                    EventType = ActivityEventType.TaskUpdated
                });
                todoItem.Description = updatedTodoItem.Description;
            }

            if (!string.Equals(todoItem.Priority.ToString(), updatedTodoItem.Priority.ToString(), StringComparison.Ordinal))
            {
                activities.Add(new ActivityLogEntry
                {
                    TodoItemId = todoItem.Id,
                    Summary = "Priority changed",
                    Details = $"Priority updated to {updatedTodoItem.Priority}",
                    EventType = ActivityEventType.PriorityChanged
                });
                todoItem.Priority = Enum.Parse<PriorityLevel>(updatedTodoItem.Priority.ToString());
            }

            if (todoItem.DueDate != updatedTodoItem.DueDate)
            {
                activities.Add(new ActivityLogEntry
                {
                    TodoItemId = todoItem.Id,
                    Summary = "Due date changed",
                    Details = $"Due date updated to {updatedTodoItem.DueDate:u}",
                    EventType = ActivityEventType.DueDateChanged
                });
                todoItem.DueDate = updatedTodoItem.DueDate;
            }

            if (todoItem.AssignedToUserId != updatedTodoItem.AssignedToUserId)
            {
                activities.Add(new ActivityLogEntry
                {
                    TodoItemId = todoItem.Id,
                    Summary = "Assignment changed",
                    Details = $"Assigned user changed to {updatedTodoItem.AssignedToUserId}",
                    EventType = ActivityEventType.AssignmentChanged
                });
                todoItem.AssignedToUserId = updatedTodoItem.AssignedToUserId;
            }

            if (todoItem.IsComplete != updatedTodoItem.IsComplete)
            {
                activities.Add(new ActivityLogEntry
                {
                    TodoItemId = todoItem.Id,
                    Summary = updatedTodoItem.IsComplete ? "Task completed" : "Task reopened",
                    EventType = updatedTodoItem.IsComplete ? ActivityEventType.TaskCompleted : ActivityEventType.TaskReopened
                });
                todoItem.IsComplete = updatedTodoItem.IsComplete;
                todoItem.CompletedDate = updatedTodoItem.IsComplete ? DateTime.UtcNow : null;
            }

            await _todoItemRepository.UpdateAsync(todoItem);

            if (activities.Count > 0)
            {
                await _activityLogRepository.AddRangeAsync(activities);
            }
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

            await _todoItemRepository.UpdateAsync(todoItem);

            await _activityLogRepository.AddAsync(new ActivityLogEntry
            {
                TodoItemId = todoItem.Id,
                Summary = isComplete ? "Task completed" : "Task reopened",
                EventType = isComplete ? ActivityEventType.TaskCompleted : ActivityEventType.TaskReopened
            });
        }
    }
}
