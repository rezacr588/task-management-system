using AutoMapper;
using FluentAssertions;
using Moq;
using TodoApi.Application.DTOs;
using TodoApi.Application.Interfaces;
using TodoApi.Application.Mappers;
using TodoApi.Application.Services;
using TodoApi.Domain.Entities;
using TodoApi.Domain.Enums;
using TodoApi.Domain.Interfaces;

namespace TodoApi.Tests.Unit.Services
{
    public class CommentServiceTests
    {
        private readonly Mock<ICommentRepository> _commentRepositoryMock = new();
        private readonly Mock<ITodoItemRepository> _todoItemRepositoryMock = new();
        private readonly Mock<IActivityLogRepository> _activityLogRepositoryMock = new();
        private readonly IMapper _mapper;
        private readonly CommentService _service;

        public CommentServiceTests()
        {
            var mapperConfig = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile(new CollaborationProfile());
            });
            _mapper = mapperConfig.CreateMapper();

            _service = new CommentService(
                _commentRepositoryMock.Object,
                _todoItemRepositoryMock.Object,
                _activityLogRepositoryMock.Object,
                _mapper);
        }

        [Fact]
        public async Task CreateCommentAsync_ShouldPersistCommentAndRecordActivity()
        {
            // Arrange
            var request = new CommentCreateRequest
            {
                TodoItemId = 1,
                AuthorId = 42,
                Content = "Initial comment",
                AuthorDisplayName = "Test User"
            };

            _todoItemRepositoryMock.Setup(repo => repo.GetByIdAsync(request.TodoItemId))
                .ReturnsAsync(new TodoItem { Id = request.TodoItemId, Title = "Sample", Description = "Desc", AssignedToUser = new User { Id = 42, Name = "Test User", Email = "user@test.com", PasswordHash = "hash", BiometricToken = "token", Role = "Admin" } });

            Comment? createdComment = null;
            _commentRepositoryMock.Setup(repo => repo.AddAsync(It.IsAny<Comment>()))
                .Callback<Comment>(comment =>
                {
                    comment.Id = 10;
                    createdComment = comment;
                })
                .Returns(Task.CompletedTask);

            _activityLogRepositoryMock.Setup(repo => repo.AddAsync(It.IsAny<ActivityLogEntry>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _service.CreateCommentAsync(request);

            // Assert
            result.Id.Should().Be(10);
            result.Content.Should().Be("Initial comment");
            createdComment.Should().NotBeNull();
            _commentRepositoryMock.Verify(repo => repo.AddAsync(It.IsAny<Comment>()), Times.Once);
            _activityLogRepositoryMock.Verify(repo => repo.AddAsync(It.Is<ActivityLogEntry>(entry => entry.EventType == ActivityEventType.CommentCreated)), Times.Once);
        }

        [Fact]
        public async Task UpdateCommentAsync_ShouldUpdateFieldsAndLogActivity()
        {
            // Arrange
            var comment = new Comment
            {
                Id = 5,
                Content = "Old",
                TodoItemId = 1,
                AuthorId = 42,
                TodoItem = new TodoItem { Id = 1, Title = "T", Description = "D", AssignedToUser = new User { Id = 7, Name = "Owner", Email = "owner@test.com", PasswordHash = "hash", BiometricToken = "token", Role = "Manager" } }
            };

            _commentRepositoryMock.Setup(repo => repo.GetByIdAsync(comment.Id))
                .ReturnsAsync(comment);
            _commentRepositoryMock.Setup(repo => repo.UpdateAsync(comment))
                .Returns(Task.CompletedTask);
            _activityLogRepositoryMock.Setup(repo => repo.AddAsync(It.IsAny<ActivityLogEntry>()))
                .Returns(Task.CompletedTask);

            var update = new CommentUpdateRequest
            {
                Content = "Updated content",
                EventType = ActivityEventType.CommentUpdated
            };

            // Act
            var result = await _service.UpdateCommentAsync(comment.Id, update);

            // Assert
            result.Content.Should().Be("Updated content");
            comment.UpdatedAt.Should().NotBeNull();
            _commentRepositoryMock.Verify(repo => repo.UpdateAsync(comment), Times.Once);
            _activityLogRepositoryMock.Verify(repo => repo.AddAsync(It.Is<ActivityLogEntry>(entry => entry.EventType == ActivityEventType.CommentUpdated)), Times.Once);
        }

        [Fact]
        public async Task DeleteCommentAsync_ShouldRemoveCommentAndLog()
        {
            // Arrange
            var comment = new Comment
            {
                Id = 8,
                Content = "Will be removed",
                TodoItemId = 3,
                AuthorId = 5
            };

            _commentRepositoryMock.Setup(repo => repo.GetByIdAsync(comment.Id))
                .ReturnsAsync(comment);
            _commentRepositoryMock.Setup(repo => repo.DeleteAsync(comment))
                .Returns(Task.CompletedTask);
            _activityLogRepositoryMock.Setup(repo => repo.AddAsync(It.IsAny<ActivityLogEntry>()))
                .Returns(Task.CompletedTask);

            // Act
            await _service.DeleteCommentAsync(comment.Id);

            // Assert
            _commentRepositoryMock.Verify(repo => repo.DeleteAsync(comment), Times.Once);
            _activityLogRepositoryMock.Verify(repo => repo.AddAsync(It.Is<ActivityLogEntry>(entry => entry.EventType == ActivityEventType.CommentDeleted)), Times.Once);
        }
    }
}
