using Microsoft.Extensions.Logging;
using Moq;
using TodoApi.Application.Interfaces;
using TodoApi.Domain.Entities;
using TodoApi.Infrastructure.Services;
using Xunit;
using FluentAssertions;

namespace TodoApi.Tests.Unit.Services;

public class AuthorizationServiceTests
{
    private readonly Mock<ILogger<AuthorizationService>> _loggerMock;
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<ITodoItemRepository> _todoRepositoryMock;
    private readonly AuthorizationService _authorizationService;

    public AuthorizationServiceTests()
    {
        _loggerMock = new Mock<ILogger<AuthorizationService>>();
        _userRepositoryMock = new Mock<IUserRepository>();
        _todoRepositoryMock = new Mock<ITodoItemRepository>();
        _authorizationService = new AuthorizationService(
            _loggerMock.Object,
            _userRepositoryMock.Object,
            _todoRepositoryMock.Object);
    }

    [Fact]
    public async Task CanUserAccessTodoAsync_WhenUserOwnsItem_ShouldReturnTrue()
    {
        // Arrange
        var userId = 1;
        var todoItemId = 100;
        var todoItem = new TodoItem
        {
            Id = todoItemId,
            UserId = userId,
            Title = "Test Task",
            Description = "Test Description"
        };

        _todoRepositoryMock.Setup(x => x.GetByIdAsync(todoItemId))
            .ReturnsAsync(todoItem);

        // Act
        var result = await _authorizationService.CanUserAccessTodoAsync(userId, todoItemId);

        // Assert
        result.Should().BeTrue();
        _todoRepositoryMock.Verify(x => x.GetByIdAsync(todoItemId), Times.Once);
    }

    [Fact]
    public async Task CanUserAccessTodoAsync_WhenUserDoesNotOwnItem_ShouldReturnFalse()
    {
        // Arrange
        var userId = 1;
        var otherUserId = 2;
        var todoItemId = 100;
        var todoItem = new TodoItem
        {
            Id = todoItemId,
            UserId = otherUserId,
            Title = "Test Task",
            Description = "Test Description"
        };

        _todoRepositoryMock.Setup(x => x.GetByIdAsync(todoItemId))
            .ReturnsAsync(todoItem);

        // Act
        var result = await _authorizationService.CanUserAccessTodoAsync(userId, todoItemId);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task CanUserAccessTodoAsync_WhenTodoItemNotFound_ShouldReturnFalse()
    {
        // Arrange
        var userId = 1;
        var todoItemId = 999;

        _todoRepositoryMock.Setup(x => x.GetByIdAsync(todoItemId))
            .ReturnsAsync((TodoItem?)null);

        // Act
        var result = await _authorizationService.CanUserAccessTodoAsync(userId, todoItemId);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsUserAdminAsync_WhenUserIsAdmin_ShouldReturnTrue()
    {
        // Arrange
        var userId = 1;
        var adminUser = new User
        {
            Id = userId,
            Email = "admin@example.com",
            Role = "Admin"
        };

        _userRepositoryMock.Setup(x => x.GetByIdAsync(userId))
            .ReturnsAsync(adminUser);

        // Act
        var result = await _authorizationService.IsUserAdminAsync(userId);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsUserAdminAsync_WhenUserIsNotAdmin_ShouldReturnFalse()
    {
        // Arrange
        var userId = 1;
        var regularUser = new User
        {
            Id = userId,
            Email = "user@example.com",
            Role = "User"
        };

        _userRepositoryMock.Setup(x => x.GetByIdAsync(userId))
            .ReturnsAsync(regularUser);

        // Act
        var result = await _authorizationService.IsUserAdminAsync(userId);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsUserAdminAsync_WhenUserNotFound_ShouldReturnFalse()
    {
        // Arrange
        var userId = 999;

        _userRepositoryMock.Setup(x => x.GetByIdAsync(userId))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _authorizationService.IsUserAdminAsync(userId);

        // Assert
        result.Should().BeFalse();
    }
}