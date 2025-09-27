using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using TodoApi.Application.DTOs;
using TodoApi.Application.Interfaces;
using TodoApi.WebApi.Controllers;
using Xunit;

namespace TodoApi.Tests.Unit.Controllers
{
    public class UserControllerTests
    {
        private readonly Mock<IUserService> _userServiceMock = new();
        private readonly UserController _controller;

        public UserControllerTests()
        {
            _controller = new UserController(_userServiceMock.Object);
        }

        [Fact]
        public async Task Register_ValidModel_ReturnsCreatedAtAction()
        {
            // Arrange
            var registrationDto = new UserRegistrationDto { Email = "test@example.com", Name = "Test User" };
            var createdUser = new UserDto { Id = 1, Email = "test@example.com", Name = "Test User" };
            _userServiceMock.Setup(s => s.CreateUserAsync(registrationDto))
                .ReturnsAsync(createdUser);

            // Act
            var result = await _controller.Register(registrationDto);

            // Assert
            var createdAtActionResult = result.Should().BeOfType<CreatedAtActionResult>().Subject;
            createdAtActionResult.ActionName.Should().Be(nameof(_controller.GetUserById));
            createdAtActionResult.RouteValues.Should().ContainKey("id").WhoseValue.Should().Be(1);
            createdAtActionResult.Value.Should().Be(createdUser);
        }

        [Fact]
        public async Task Register_InvalidModel_ReturnsBadRequest()
        {
            // Arrange
            _controller.ModelState.AddModelError("Email", "Required");

            // Act
            var result = await _controller.Register(new UserRegistrationDto());

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task Register_UserAlreadyExists_ReturnsConflict()
        {
            // Arrange
            var registrationDto = new UserRegistrationDto { Email = "existing@example.com", Name = "Test User" };
            _userServiceMock.Setup(s => s.CreateUserAsync(registrationDto))
                .ThrowsAsync(new InvalidOperationException("User already exists"));

            // Act
            var result = await _controller.Register(registrationDto);

            // Assert
            var conflictResult = result.Should().BeOfType<ConflictObjectResult>().Subject;
            conflictResult.StatusCode.Should().Be(409);
        }

        [Fact]
        public async Task Register_ExceptionThrown_ReturnsBadRequest()
        {
            // Arrange
            var registrationDto = new UserRegistrationDto { Email = "test@example.com", Name = "Test User" };
            _userServiceMock.Setup(s => s.CreateUserAsync(registrationDto))
                .ThrowsAsync(new Exception("Some error"));

            // Act
            var result = await _controller.Register(registrationDto);

            // Assert
            var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            badRequestResult.Value.Should().Be("Some error");
        }

        [Fact]
        public async Task UpdateUser_ValidModel_ReturnsNoContent()
        {
            // Arrange
            var updateDto = new UserUpdateDto { Name = "Updated Name" };

            // Act
            var result = await _controller.UpdateUser(1, updateDto);

            // Assert
            result.Should().BeOfType<NoContentResult>();
            _userServiceMock.Verify(s => s.UpdateUserAsync(1, updateDto), Times.Once);
        }

        [Fact]
        public async Task UpdateUser_InvalidModel_ReturnsBadRequest()
        {
            // Arrange
            _controller.ModelState.AddModelError("Name", "Required");

            // Act
            var result = await _controller.UpdateUser(1, new UserUpdateDto());

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task DeleteUser_ValidId_ReturnsNoContent()
        {
            // Act
            var result = await _controller.DeleteUser(1);

            // Assert
            result.Should().BeOfType<NoContentResult>();
            _userServiceMock.Verify(s => s.DeleteUserAsync(1), Times.Once);
        }

        [Fact]
        public async Task GetUserById_ValidId_ReturnsUser()
        {
            // Arrange
            var expectedUser = new UserDto { Id = 1, Email = "test@example.com", Name = "Test User" };
            _userServiceMock.Setup(s => s.GetUserByIdAsync(1))
                .ReturnsAsync(expectedUser);

            // Act
            var result = await _controller.GetUserById(1);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.Value.Should().Be(expectedUser);
        }

        [Fact]
        public async Task GetUserByEmail_ValidEmail_ReturnsUser()
        {
            // Arrange
            var expectedUser = new UserDto { Id = 1, Email = "test@example.com", Name = "Test User" };
            _userServiceMock.Setup(s => s.GetUserByEmailAsync("test@example.com"))
                .ReturnsAsync(expectedUser);

            // Act
            var result = await _controller.GetUserByEmail("test@example.com");

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.Value.Should().Be(expectedUser);
        }
    }
}