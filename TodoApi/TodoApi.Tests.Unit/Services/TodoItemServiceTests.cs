using System;
using System.Collections.Generic;
using System.Linq;
using AutoMapper;
using FluentAssertions;
using Moq;
using TodoApi.Application.DTOs;
using TodoApi.Application.Mappers;
using TodoApi.Application.Services;
using TodoApi.Domain.Entities;
using TodoApi.Domain.Enums;
using TodoApi.Application.Interfaces;
using TodoApi.Domain.Services;
using MediatR;
using Xunit;

namespace TodoApi.Tests.Unit.Services
{
    public class TodoItemServiceTests
    {
        private readonly Mock<ITodoItemRepository> _todoRepositoryMock = new();
        private readonly Mock<IActivityLogRepository> _activityRepositoryMock = new();
        private readonly Mock<IActivityLogger> _activityLoggerMock = new();
        private readonly Mock<IMediator> _mediatorMock = new();
        private readonly IMapper _mapper;
        private readonly TodoItemService _service;

        public TodoItemServiceTests()
        {
            var mapperConfig = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile(new TodoApi.Application.Mappings.TodoItemProfile());
            });
            _mapper = mapperConfig.CreateMapper();

            _service = new TodoItemService(_todoRepositoryMock.Object, _mapper, _activityRepositoryMock.Object, _activityLoggerMock.Object, _mediatorMock.Object);
        }

        [Fact]
        public async Task UpdateTodoItemAsync_ShouldRecordActivityForChangedFields()
        {
            var existing = new TodoItem
            {
                Id = 5,
                Title = "Old title",
                Description = "Old description",
                DueDate = DateTime.UtcNow.Date,
                Priority = PriorityLevel.Low,
                AssignedToUserId = 1,
                AssignedToUser = new User
                {
                    Id = 1,
                    Name = "Owner",
                    Email = "owner@example.com",
                    PasswordHash = "hash",
                    BiometricToken = "token",
                    Role = "Manager"
                }
            };
            _todoRepositoryMock.Setup(repo => repo.GetByIdAsync(existing.Id)).ReturnsAsync(existing);
            _todoRepositoryMock.Setup(repo => repo.UpdateAsync(existing)).Returns(Task.CompletedTask);
            _activityRepositoryMock.Setup(repo => repo.AddRangeAsync(It.IsAny<IEnumerable<ActivityLogEntry>>())).Returns(Task.CompletedTask);

            // Setup activity logger to return some activities
            var activities = new List<ActivityLogEntry>
            {
                new ActivityLogEntry { TodoItemId = 5, Summary = "Title changed", EventType = ActivityEventType.TaskUpdated },
                new ActivityLogEntry { TodoItemId = 5, Summary = "Description changed", EventType = ActivityEventType.TaskUpdated },
                new ActivityLogEntry { TodoItemId = 5, Summary = "Priority changed", EventType = ActivityEventType.TaskUpdated },
                new ActivityLogEntry { TodoItemId = 5, Summary = "Due date changed", EventType = ActivityEventType.TaskUpdated },
                new ActivityLogEntry { TodoItemId = 5, Summary = "Assignee changed", EventType = ActivityEventType.TaskUpdated },
                new ActivityLogEntry { TodoItemId = 5, Summary = "Task completed", EventType = ActivityEventType.TaskCompleted }
            };
            _activityLoggerMock.Setup(logger => logger.GetActivityLogEntries(It.IsAny<TodoItem>(), It.IsAny<TodoItem>())).Returns(activities);

            var update = new TodoItemDto
            {
                Id = 5,
                Title = "New title",
                Description = "Better description",
                DueDate = DateTime.UtcNow.Date.AddDays(1),
                Priority = PriorityLevelDto.High,
                AssignedToUserId = 2,
                IsComplete = true
            };

            await _service.UpdateTodoItemAsync(existing.Id, update);

            _todoRepositoryMock.Verify(repo => repo.UpdateAsync(existing), Times.Once);
            _activityRepositoryMock.Verify(repo => repo.AddRangeAsync(It.Is<IEnumerable<ActivityLogEntry>>(entries => entries.Count() >= 4)), Times.Once);
        }

        [Fact]
        public async Task MarkTodoItemCompleteAsync_ShouldCreateActivityLog()
        {
            var todo = new TodoItem
            {
                Id = 10,
                Title = "Task",
                Description = "Desc",
                AssignedToUser = new User { Id = 1, Name = "Owner", Email = "owner@example.com", PasswordHash = "hash", BiometricToken = "token", Role = "Admin" }
            };

            _todoRepositoryMock.Setup(repo => repo.GetByIdAsync(todo.Id)).ReturnsAsync(todo);
            _todoRepositoryMock.Setup(repo => repo.UpdateAsync(todo)).Returns(Task.CompletedTask);
            _activityRepositoryMock.Setup(repo => repo.AddRangeAsync(It.IsAny<IEnumerable<ActivityLogEntry>>())).Returns(Task.CompletedTask);

            // Setup activity logger to return completion activity
            var activities = new List<ActivityLogEntry>
            {
                new ActivityLogEntry { TodoItemId = 10, Summary = "Task completed", EventType = ActivityEventType.TaskCompleted }
            };
            _activityLoggerMock.Setup(logger => logger.GetActivityLogEntries(It.IsAny<TodoItem>(), It.IsAny<TodoItem>())).Returns(activities);

            await _service.MarkTodoItemCompleteAsync(todo.Id, true);

            _todoRepositoryMock.Verify(repo => repo.UpdateAsync(todo), Times.Once);
            _activityRepositoryMock.Verify(repo => repo.AddRangeAsync(It.Is<IEnumerable<ActivityLogEntry>>(entries => entries.Any(e => e.EventType == ActivityEventType.TaskCompleted))), Times.Once);
        }
    }
}
