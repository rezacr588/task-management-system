using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using TodoApi.Application.DTOs;
using TodoApi.Application.Interfaces;
using TodoApi.Application.Services;
using TodoApi.WebApi.Controllers;
using Xunit;

namespace TodoApi.Tests.Unit.Controllers
{
    public class TodoItemsControllerTests
    {
        private readonly Mock<ITodoItemService> _todoItemServiceMock = new();
        private readonly Mock<ITagService> _tagServiceMock = new();
        private readonly TodoItemsController _controller;

        public TodoItemsControllerTests()
        {
            _controller = new TodoItemsController(_todoItemServiceMock.Object, _tagServiceMock.Object);
        }

        [Fact]
        public async Task GetAllTodoItems_WithoutPagination_ReturnsAllItems()
        {
            // Arrange
            var expectedItems = new List<TodoItemDto>
            {
                new TodoItemDto { Id = 1, Title = "Test Item 1" },
                new TodoItemDto { Id = 2, Title = "Test Item 2" }
            };
            _todoItemServiceMock.Setup(s => s.GetAllTodoItemsAsync(null))
                .ReturnsAsync(expectedItems);

            // Act
            var result = await _controller.GetAllTodoItems(null, null);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.Value.Should().BeEquivalentTo(expectedItems);
        }

        [Fact]
        public async Task GetAllTodoItems_WithPagination_ReturnsPaginatedResult()
        {
            // Arrange
            var items = new List<TodoItemDto> { new TodoItemDto { Id = 1, Title = "Test Item" } };
            var paginatedResult = new PaginatedResponse<TodoItemDto>(items, 1, 10, 1);
            _todoItemServiceMock.Setup(s => s.GetTodoItemsPaginatedAsync(1, 10))
                .ReturnsAsync(paginatedResult);

            // Act
            var result = await _controller.GetAllTodoItems(1, 10);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.Value.Should().BeEquivalentTo(paginatedResult);
        }

        [Fact]
        public async Task GetTodoItem_ValidId_ReturnsItem()
        {
            // Arrange
            var expectedItem = new TodoItemDto { Id = 1, Title = "Test Item" };
            _todoItemServiceMock.Setup(s => s.GetTodoItemByIdAsync(1))
                .ReturnsAsync(expectedItem);

            // Act
            var result = await _controller.GetTodoItem(1);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.Value.Should().Be(expectedItem);
        }

        [Fact]
        public async Task CreateTodoItem_ValidModel_ReturnsCreatedAtAction()
        {
            // Arrange
            var todoItemDto = new TodoItemDto { Id = 0, Title = "New Item" };
            var createdItem = new TodoItemDto { Id = 1, Title = "New Item" };
            _todoItemServiceMock.Setup(s => s.CreateTodoItemAsync(todoItemDto))
                .ReturnsAsync(createdItem);

            // Act
            var result = await _controller.CreateTodoItem(todoItemDto);

            // Assert
            var createdAtActionResult = result.Should().BeOfType<CreatedAtActionResult>().Subject;
            createdAtActionResult.ActionName.Should().Be(nameof(_controller.GetTodoItem));
            createdAtActionResult.RouteValues.Should().ContainKey("id").WhoseValue.Should().Be(1);
            createdAtActionResult.Value.Should().Be(createdItem);
        }

        [Fact]
        public async Task CreateTodoItem_InvalidModel_ReturnsBadRequest()
        {
            // Arrange
            _controller.ModelState.AddModelError("Title", "Required");

            // Act
            var result = await _controller.CreateTodoItem(new TodoItemDto());

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task UpdateTodoItem_ValidId_ReturnsNoContent()
        {
            // Arrange
            var todoItemDto = new TodoItemDto { Id = 1, Title = "Updated Item" };

            // Act
            var result = await _controller.UpdateTodoItem(1, todoItemDto);

            // Assert
            result.Should().BeOfType<NoContentResult>();
            _todoItemServiceMock.Verify(s => s.UpdateTodoItemAsync(1, todoItemDto), Times.Once);
        }

        [Fact]
        public async Task UpdateTodoItem_IdMismatch_ReturnsBadRequest()
        {
            // Arrange
            var todoItemDto = new TodoItemDto { Id = 2, Title = "Updated Item" };

            // Act
            var result = await _controller.UpdateTodoItem(1, todoItemDto);

            // Assert
            result.Should().BeOfType<BadRequestResult>();
            _todoItemServiceMock.Verify(s => s.UpdateTodoItemAsync(It.IsAny<int>(), It.IsAny<TodoItemDto>()), Times.Never);
        }

        [Fact]
        public async Task DeleteTodoItem_ValidId_ReturnsNoContent()
        {
            // Act
            var result = await _controller.DeleteTodoItem(1);

            // Assert
            result.Should().BeOfType<NoContentResult>();
            _todoItemServiceMock.Verify(s => s.DeleteTodoItemAsync(1), Times.Once);
        }

        [Fact]
        public async Task GetTagsForTodo_ValidId_ReturnsTags()
        {
            // Arrange
            var expectedTags = new List<TagDto>
            {
                new TagDto { Id = 1, Name = "Tag1" },
                new TagDto { Id = 2, Name = "Tag2" }
            };
            _tagServiceMock.Setup(s => s.GetTagsForTodoAsync(1))
                .ReturnsAsync(expectedTags);

            // Act
            var result = await _controller.GetTagsForTodo(1);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.Value.Should().BeEquivalentTo(expectedTags);
        }

        [Fact]
        public async Task AttachTagToTodo_ValidIds_ReturnsNoContent()
        {
            // Act
            var result = await _controller.AttachTagToTodo(1, 2);

            // Assert
            result.Should().BeOfType<NoContentResult>();
            _tagServiceMock.Verify(s => s.AttachTagToTodoAsync(1, 2), Times.Once);
        }

        [Fact]
        public async Task DetachTagFromTodo_ValidIds_ReturnsNoContent()
        {
            // Act
            var result = await _controller.DetachTagFromTodo(1, 2);

            // Assert
            result.Should().BeOfType<NoContentResult>();
            _tagServiceMock.Verify(s => s.DetachTagFromTodoAsync(1, 2), Times.Once);
        }
    }
}