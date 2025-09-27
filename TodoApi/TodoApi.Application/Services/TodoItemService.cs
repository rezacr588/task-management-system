using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using TodoApi.Application.DTOs;
using TodoApi.Application.Interfaces;
using TodoApi.Domain.Services;
using AutoMapper;
using TodoApi.Domain.Entities;
using TodoApi.Domain.Enums;
using MediatR;
using TodoApi.Domain.Events;
using TodoApi.Application.Events;

namespace TodoApi.Application.Services
{
    public class TodoItemService : ITodoItemService
    {
        private readonly ITodoItemRepository _todoItemRepository;
        private readonly IMapper _mapper;
        private readonly IActivityLogRepository _activityLogRepository;
        private readonly IActivityLogger _activityLogger;
        private readonly IMediator _mediator;

        public TodoItemService(ITodoItemRepository todoItemRepository, IMapper mapper, IActivityLogRepository activityLogRepository, IActivityLogger activityLogger, IMediator mediator)
        {
            _todoItemRepository = todoItemRepository;
            _mapper = mapper;
            _activityLogRepository = activityLogRepository;
            _activityLogger = activityLogger;
            _mediator = mediator;
        }

        public async Task<TodoItemDto> CreateTodoItemAsync(TodoItemDto todoItem)
        {
            var tItem = _mapper.Map<TodoItem>(todoItem);
            await _todoItemRepository.AddAsync(tItem);

            // Log task creation activity
            var activityEntry = new ActivityLogEntry
            {
                TodoItemId = tItem.Id,
                Summary = "Task created",
                Details = $"Task '{tItem.Title}' was created",
                EventType = ActivityEventType.TaskCreated
            };
            await _activityLogRepository.AddAsync(activityEntry);

            var dto = _mapper.Map<TodoItemDto>(tItem);
            HateoasHelper.AddTodoItemLinks(dto);
            return dto;
        }

        public async Task<TodoItemDto> GetTodoItemByIdAsync(int id)
        {
            var todoItem = await _todoItemRepository.GetByIdAsync(id);
            if (todoItem == null)
            {
                throw new KeyNotFoundException("Todo item not found.");
            }

            var dto = _mapper.Map<TodoItemDto>(todoItem);
            HateoasHelper.AddTodoItemLinks(dto);
            return dto;
        }

        public async Task<IEnumerable<TodoItemDto>> GetAllTodoItemsAsync(Expression<Func<TodoItemDto, bool>>? filter)
        {
            var todoItems = await _todoItemRepository.GetAllAsync();
            return _mapper.Map<IEnumerable<TodoItemDto>>(todoItems);
        }

        public async Task<PaginatedResponse<TodoItemDto>> GetTodoItemsPaginatedAsync(int pageNumber = 1, int pageSize = 10)
        {
            var todoItems = await _todoItemRepository.GetAllAsync();
            var totalCount = todoItems.Count();

            var paginatedItems = todoItems
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize);

            var dtoItems = _mapper.Map<List<TodoItemDto>>(paginatedItems);

            // Add HATEOAS links to each item
            foreach (var item in dtoItems)
            {
                HateoasHelper.AddTodoItemLinks(item);
            }

            var response = new PaginatedResponse<TodoItemDto>(dtoItems, pageNumber, pageSize, totalCount);

            // Add collection-level HATEOAS links
            if (dtoItems.Count > 0)
            {
                HateoasHelper.AddTodoItemsCollectionLinks(dtoItems, pageNumber, pageSize, totalCount);
            }

            return response;
        }

        public async Task UpdateTodoItemAsync(int id, TodoItemDto updatedTodoItem)
        {
            var todoItem = await _todoItemRepository.GetByIdAsync(id);
            if (todoItem == null)
            {
                throw new KeyNotFoundException("Todo item not found.");
            }

            var originalTodoItem = new TodoItem
            {
                Id = todoItem.Id,
                Title = todoItem.Title,
                Description = todoItem.Description,
                Priority = todoItem.Priority,
                DueDate = todoItem.DueDate,
                AssignedToUserId = todoItem.AssignedToUserId,
                IsComplete = todoItem.IsComplete,
                AssignedToUser = todoItem.AssignedToUser,
                CompletedDate = todoItem.CompletedDate
            };

            // Update the entity
            todoItem.Title = updatedTodoItem.Title;
            todoItem.Description = updatedTodoItem.Description;
            todoItem.Priority = Enum.Parse<PriorityLevel>(updatedTodoItem.Priority.ToString());
            todoItem.DueDate = updatedTodoItem.DueDate;
            todoItem.AssignedToUserId = updatedTodoItem.AssignedToUserId;
            todoItem.IsComplete = updatedTodoItem.IsComplete;
            todoItem.CompletedDate = updatedTodoItem.IsComplete ? DateTime.UtcNow : null;

            await _todoItemRepository.UpdateAsync(todoItem);

            // Log activities
            var activities = _activityLogger.GetActivityLogEntries(originalTodoItem, todoItem);
            if (activities.Any())
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

            var originalTodoItem = new TodoItem
            {
                Id = todoItem.Id,
                Title = todoItem.Title,
                Description = todoItem.Description,
                Priority = todoItem.Priority,
                DueDate = todoItem.DueDate,
                AssignedToUserId = todoItem.AssignedToUserId,
                IsComplete = todoItem.IsComplete,
                AssignedToUser = todoItem.AssignedToUser,
                CompletedDate = todoItem.CompletedDate
            };

            todoItem.IsComplete = isComplete;
            todoItem.CompletedDate = isComplete ? DateTime.UtcNow : null;

            await _todoItemRepository.UpdateAsync(todoItem);

            // Log activities
            var activities = _activityLogger.GetActivityLogEntries(originalTodoItem, todoItem);
            if (activities.Any())
            {
                await _activityLogRepository.AddRangeAsync(activities);
            }

            // Publish domain event if task was completed
            if (isComplete && !originalTodoItem.IsComplete)
            {
                await _mediator.Publish(new MediatRDomainEvent(new TaskCompletedEvent(todoItem)));
            }
        }
    }
}
