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
    public class CommentsControllerTests
    {
        private readonly Mock<ICommentService> _commentServiceMock = new();
        private readonly Mock<IActivityLogService> _activityLogServiceMock = new();
        private readonly CommentsController _controller;

        public CommentsControllerTests()
        {
            _controller = new CommentsController(_commentServiceMock.Object, _activityLogServiceMock.Object);
        }

        [Fact]
        public async Task GetCommentsForTodo_ValidTodoId_ReturnsComments()
        {
            // Arrange
            var expectedComments = new List<CommentDto>
            {
                new CommentDto { Id = 1, Content = "Comment 1", TodoItemId = 1 },
                new CommentDto { Id = 2, Content = "Comment 2", TodoItemId = 1 }
            };
            _commentServiceMock.Setup(s => s.GetCommentsForTodoAsync(1))
                .ReturnsAsync(expectedComments);

            // Act
            var result = await _controller.GetCommentsForTodo(1);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.Value.Should().BeEquivalentTo(expectedComments);
        }

        [Fact]
        public async Task GetComment_ValidId_ReturnsComment()
        {
            // Arrange
            var expectedComment = new CommentDto { Id = 1, Content = "Test Comment", TodoItemId = 1 };
            _commentServiceMock.Setup(s => s.GetCommentByIdAsync(1))
                .ReturnsAsync(expectedComment);

            // Act
            var result = await _controller.GetComment(1);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.Value.Should().Be(expectedComment);
        }

        [Fact]
        public async Task CreateComment_ValidModel_ReturnsCreatedAtAction()
        {
            // Arrange
            var createRequest = new CommentCreateRequest { Content = "New Comment", TodoItemId = 1 };
            var createdComment = new CommentDto { Id = 1, Content = "New Comment", TodoItemId = 1 };
            _commentServiceMock.Setup(s => s.CreateCommentAsync(createRequest))
                .ReturnsAsync(createdComment);

            // Act
            var result = await _controller.CreateComment(createRequest);

            // Assert
            var createdAtActionResult = result.Should().BeOfType<CreatedAtActionResult>().Subject;
            createdAtActionResult.ActionName.Should().Be(nameof(_controller.GetComment));
            createdAtActionResult.RouteValues.Should().ContainKey("id").WhoseValue.Should().Be(1);
            createdAtActionResult.Value.Should().Be(createdComment);
        }

        [Fact]
        public async Task CreateComment_InvalidModel_ReturnsBadRequest()
        {
            // Arrange
            _controller.ModelState.AddModelError("Content", "Required");

            // Act
            var result = await _controller.CreateComment(new CommentCreateRequest());

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task UpdateComment_ValidModel_ReturnsOk()
        {
            // Arrange
            var updateRequest = new CommentUpdateRequest { Content = "Updated Comment" };
            var updatedComment = new CommentDto { Id = 1, Content = "Updated Comment", TodoItemId = 1 };
            _commentServiceMock.Setup(s => s.UpdateCommentAsync(1, updateRequest))
                .ReturnsAsync(updatedComment);

            // Act
            var result = await _controller.UpdateComment(1, updateRequest);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.Value.Should().Be(updatedComment);
        }

        [Fact]
        public async Task UpdateComment_InvalidModel_ReturnsBadRequest()
        {
            // Arrange
            _controller.ModelState.AddModelError("Content", "Required");

            // Act
            var result = await _controller.UpdateComment(1, new CommentUpdateRequest());

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task DeleteComment_ValidId_ReturnsNoContent()
        {
            // Act
            var result = await _controller.DeleteComment(1);

            // Assert
            result.Should().BeOfType<NoContentResult>();
            _commentServiceMock.Verify(s => s.DeleteCommentAsync(1), Times.Once);
        }

        [Fact]
        public async Task GetActivity_ValidTodoId_ReturnsActivity()
        {
            // Arrange
            var expectedActivity = new List<ActivityLogDto>
            {
                new ActivityLogDto { Id = 1, Summary = "Created", TodoItemId = 1, EventType = TodoApi.Domain.Enums.ActivityEventType.CommentCreated },
                new ActivityLogDto { Id = 2, Summary = "Updated", TodoItemId = 1, EventType = TodoApi.Domain.Enums.ActivityEventType.CommentUpdated }
            };
            _activityLogServiceMock.Setup(s => s.GetActivityForTodoAsync(1))
                .ReturnsAsync(expectedActivity);

            // Act
            var result = await _controller.GetActivity(1);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.Value.Should().BeEquivalentTo(expectedActivity);
        }
    }
}