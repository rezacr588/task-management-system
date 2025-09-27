using AutoMapper;
using FluentAssertions;
using Moq;
using TodoApi.Application.DTOs;
using TodoApi.Application.Mappers;
using TodoApi.Application.Services;
using TodoApi.Domain.Entities;
using TodoApi.Application.Interfaces;
using Xunit;

namespace TodoApi.Tests.Unit.Services
{
    public class UserServiceTests
    {
        private readonly Mock<IUserRepository> _userRepositoryMock = new();
        private readonly IMapper _mapper;
        private readonly UserService _service;

        public UserServiceTests()
        {
            var mapperConfig = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile(new UserProfile());
            });
            _mapper = mapperConfig.CreateMapper();

            _service = new UserService(_userRepositoryMock.Object, _mapper);
        }

        [Fact]
        public async Task CreateUserAsync_ShouldCreateAndReturnDto_WhenUserDoesNotExist()
        {
            // Arrange
            var registration = new UserRegistrationDto
            {
                Name = "Test User",
                Email = "test@example.com",
                Password = "password",
                BiometricToken = "token",
                Role = "User"
            };
            _userRepositoryMock.Setup(repo => repo.GetByEmailAsync(registration.Email)).ReturnsAsync((User?)null);
            User? addedUser = null;
            _userRepositoryMock.Setup(repo => repo.AddAsync(It.IsAny<User>()))
                .Callback<User>(user =>
                {
                    user.Id = 1;
                    addedUser = user;
                })
                .Returns(Task.CompletedTask);

            // Act
            var result = await _service.CreateUserAsync(registration);

            // Assert
            result.Id.Should().Be(1);
            result.Name.Should().Be("Test User");
            result.Email.Should().Be("test@example.com");
            addedUser.Should().NotBeNull();
            addedUser!.PasswordHash.Should().NotBeNullOrEmpty();
            _userRepositoryMock.Verify(repo => repo.AddAsync(It.IsAny<User>()), Times.Once);
        }

        [Fact]
        public async Task CreateUserAsync_ShouldThrow_WhenUserExists()
        {
            // Arrange
            var registration = new UserRegistrationDto { Email = "existing@example.com" };
            var existingUser = new User { Id = 1, Name = "Existing", Email = "existing@example.com", PasswordHash = "hash", BiometricToken = "token", Role = "User" };
            _userRepositoryMock.Setup(repo => repo.GetByEmailAsync(registration.Email)).ReturnsAsync(existingUser);

            // Act
            Func<Task> act = async () => await _service.CreateUserAsync(registration);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("User with the given email already exists.");
        }

        [Fact]
        public async Task GetUserByIdAsync_ShouldReturnDto_WhenUserExists()
        {
            // Arrange
            var user = new User { Id = 1, Name = "Test", Email = "test@example.com", PasswordHash = "hash", BiometricToken = "token", Role = "User" };
            _userRepositoryMock.Setup(repo => repo.GetByIdAsync(1)).ReturnsAsync(user);

            // Act
            var result = await _service.GetUserByIdAsync(1);

            // Assert
            result.Id.Should().Be(1);
            result.Name.Should().Be("Test");
        }

        [Fact]
        public async Task GetUserByIdAsync_ShouldThrow_WhenUserNotFound()
        {
            // Arrange
            _userRepositoryMock.Setup(repo => repo.GetByIdAsync(1)).ReturnsAsync((User?)null);

            // Act
            Func<Task> act = async () => await _service.GetUserByIdAsync(1);

            // Assert
            await act.Should().ThrowAsync<KeyNotFoundException>().WithMessage("User not found.");
        }

        [Fact]
        public async Task UserExists_ShouldReturnTrue_WhenUserExists()
        {
            // Arrange
            var user = new User { Id = 1, Name = "Test", Email = "test@example.com", PasswordHash = "hash", BiometricToken = "token", Role = "User" };
            _userRepositoryMock.Setup(repo => repo.GetByEmailAsync("test@example.com")).ReturnsAsync(user);

            // Act
            var result = await _service.UserExists("test@example.com");

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task UserExists_ShouldReturnFalse_WhenUserDoesNotExist()
        {
            // Arrange
            _userRepositoryMock.Setup(repo => repo.GetByEmailAsync("test@example.com")).ReturnsAsync((User?)null);

            // Act
            var result = await _service.UserExists("test@example.com");

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task GetUserByEmailAsync_ShouldReturnDto_WhenUserExists()
        {
            // Arrange
            var user = new User { Id = 1, Name = "Test", Email = "test@example.com", PasswordHash = "hash", BiometricToken = "token", Role = "User" };
            _userRepositoryMock.Setup(repo => repo.GetByEmailAsync("test@example.com")).ReturnsAsync(user);

            // Act
            var result = await _service.GetUserByEmailAsync("test@example.com");

            // Assert
            result.Id.Should().Be(1);
            result.Name.Should().Be("Test");
        }

        [Fact]
        public async Task GetUserByEmailAsync_ShouldThrow_WhenUserNotFound()
        {
            // Arrange
            _userRepositoryMock.Setup(repo => repo.GetByEmailAsync("test@example.com")).ReturnsAsync((User?)null);

            // Act
            Func<Task> act = async () => await _service.GetUserByEmailAsync("test@example.com");

            // Assert
            await act.Should().ThrowAsync<KeyNotFoundException>().WithMessage("User not found.");
        }

        [Fact]
        public async Task UpdateUserAsync_ShouldUpdateFields_WhenUserExists()
        {
            // Arrange
            var existingUser = new User { Id = 1, Name = "Old", Email = "old@example.com", PasswordHash = "hash", BiometricToken = "old", Role = "Old" };
            var update = new UserUpdateDto { Name = "New", Email = "new@example.com", BiometricToken = "new", Role = "New" };
            _userRepositoryMock.Setup(repo => repo.GetByIdAsync(1)).ReturnsAsync(existingUser);
            _userRepositoryMock.Setup(repo => repo.UpdateAsync(existingUser)).Returns(Task.CompletedTask);

            // Act
            await _service.UpdateUserAsync(1, update);

            // Assert
            existingUser.Name.Should().Be("New");
            existingUser.Email.Should().Be("new@example.com");
            existingUser.BiometricToken.Should().Be("new");
            existingUser.Role.Should().Be("New");
            _userRepositoryMock.Verify(repo => repo.UpdateAsync(existingUser), Times.Once);
        }

        [Fact]
        public async Task UpdateUserAsync_ShouldThrow_WhenUserNotFound()
        {
            // Arrange
            _userRepositoryMock.Setup(repo => repo.GetByIdAsync(1)).ReturnsAsync((User?)null);

            // Act
            Func<Task> act = async () => await _service.UpdateUserAsync(1, new UserUpdateDto());

            // Assert
            await act.Should().ThrowAsync<KeyNotFoundException>().WithMessage("User not found.");
        }

        [Fact]
        public async Task DeleteUserAsync_ShouldDelete_WhenUserExists()
        {
            // Arrange
            var user = new User { Id = 1, Name = "Test", Email = "test@example.com", PasswordHash = "hash", BiometricToken = "token", Role = "User" };
            _userRepositoryMock.Setup(repo => repo.GetByIdAsync(1)).ReturnsAsync(user);
            _userRepositoryMock.Setup(repo => repo.DeleteAsync(user)).Returns(Task.CompletedTask);

            // Act
            await _service.DeleteUserAsync(1);

            // Assert
            _userRepositoryMock.Verify(repo => repo.DeleteAsync(user), Times.Once);
        }

        [Fact]
        public async Task DeleteUserAsync_ShouldThrow_WhenUserNotFound()
        {
            // Arrange
            _userRepositoryMock.Setup(repo => repo.GetByIdAsync(1)).ReturnsAsync((User?)null);

            // Act
            Func<Task> act = async () => await _service.DeleteUserAsync(1);

            // Assert
            await act.Should().ThrowAsync<KeyNotFoundException>().WithMessage("User not found.");
        }
    }
}