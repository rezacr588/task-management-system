using AutoMapper;
using FluentAssertions;
using Moq;
using TodoApi.Application.DTOs;
using TodoApi.Application.Mappers;
using TodoApi.Application.Services;
using TodoApi.Domain.Entities;
using TodoApi.Domain.Interfaces;
using Xunit;

namespace TodoApi.Tests.Unit.Services
{
    public class ActivityLogServiceTests
    {
        private readonly Mock<IActivityLogRepository> _activityLogRepositoryMock = new();
        private readonly IMapper _mapper;
        private readonly ActivityLogService _service;

        public ActivityLogServiceTests()
        {
            var mapperConfig = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile(new CollaborationProfile());
            });
            _mapper = mapperConfig.CreateMapper();

            _service = new ActivityLogService(_activityLogRepositoryMock.Object, _mapper);
        }

        [Fact]
        public async Task GetActivityByIdAsync_ShouldReturnMappedDto()
        {
            // Arrange
            var entry = new ActivityLogEntry
            {
                Id = 1,
                EventType = Domain.Enums.ActivityEventType.TaskCreated,
                Summary = "Test",
                TodoItemId = 1,
                ActorId = 1,
                CreatedAt = DateTime.UtcNow
            };
            _activityLogRepositoryMock.Setup(repo => repo.GetByIdAsync(1)).ReturnsAsync(entry);

            // Act
            var result = await _service.GetActivityByIdAsync(1);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().Be(1);
            result.EventType.Should().Be(Domain.Enums.ActivityEventType.TaskCreated);
        }

        [Fact]
        public async Task GetActivityForTodoAsync_ShouldReturnMappedDtos()
        {
            // Arrange
            var entries = new List<ActivityLogEntry>
            {
                new ActivityLogEntry { Id = 1, TodoItemId = 1, Summary = "Created", EventType = Domain.Enums.ActivityEventType.TaskCreated },
                new ActivityLogEntry { Id = 2, TodoItemId = 1, Summary = "Commented", EventType = Domain.Enums.ActivityEventType.CommentCreated }
            };
            _activityLogRepositoryMock.Setup(repo => repo.GetByTodoItemIdAsync(1)).ReturnsAsync(entries);

            // Act
            var result = await _service.GetActivityForTodoAsync(1);

            // Assert
            result.Should().HaveCount(2);
            result.First().Id.Should().Be(1);
        }

        [Fact]
        public async Task RecordAsync_ShouldAddEntryAndReturnMappedDto()
        {
            // Arrange
            var dto = new ActivityLogDto
            {
                EventType = Domain.Enums.ActivityEventType.TaskUpdated,
                Summary = "Updated",
                TodoItemId = 1,
                ActorId = 1
            };
            ActivityLogEntry? addedEntry = null;
            _activityLogRepositoryMock.Setup(repo => repo.AddAsync(It.IsAny<ActivityLogEntry>()))
                .Callback<ActivityLogEntry>(entry => addedEntry = entry)
                .Returns(Task.CompletedTask);

            // Act
            var result = await _service.RecordAsync(dto);

            // Assert
            addedEntry.Should().NotBeNull();
            result.EventType.Should().Be(Domain.Enums.ActivityEventType.TaskUpdated);
            _activityLogRepositoryMock.Verify(repo => repo.AddAsync(It.IsAny<ActivityLogEntry>()), Times.Once);
        }

        [Fact]
        public async Task RecordRangeAsync_ShouldAddAllEntries()
        {
            // Arrange
            var dtos = new List<ActivityLogDto>
            {
                new ActivityLogDto { EventType = Domain.Enums.ActivityEventType.CommentCreated, Summary = "Comment" },
                new ActivityLogDto { EventType = Domain.Enums.ActivityEventType.StatusChanged, Summary = "Status" }
            };
            var addedEntries = new List<ActivityLogEntry>();
            _activityLogRepositoryMock.Setup(repo => repo.AddRangeAsync(It.IsAny<IEnumerable<ActivityLogEntry>>()))
                .Callback<IEnumerable<ActivityLogEntry>>(entries => addedEntries.AddRange(entries))
                .Returns(Task.CompletedTask);

            // Act
            await _service.RecordRangeAsync(dtos);

            // Assert
            addedEntries.Should().HaveCount(2);
            _activityLogRepositoryMock.Verify(repo => repo.AddRangeAsync(It.IsAny<IEnumerable<ActivityLogEntry>>()), Times.Once);
        }
    }
}