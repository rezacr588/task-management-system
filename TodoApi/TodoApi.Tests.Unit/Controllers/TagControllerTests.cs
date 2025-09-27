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
    public class TagControllerTests
    {
        private readonly Mock<ITagService> _tagServiceMock = new();
        private readonly TagController _controller;

        public TagControllerTests()
        {
            _controller = new TagController(_tagServiceMock.Object);
        }

        [Fact]
        public async Task CreateTag_ValidModel_ReturnsCreatedAtAction()
        {
            // Arrange
            var tagDto = new TagDto { Id = 0, Name = "New Tag" };
            var createdTag = new TagDto { Id = 1, Name = "New Tag" };
            _tagServiceMock.Setup(s => s.CreateTagAsync(tagDto))
                .ReturnsAsync(createdTag);

            // Act
            var result = await _controller.CreateTag(tagDto);

            // Assert
            var createdAtActionResult = result.Should().BeOfType<CreatedAtActionResult>().Subject;
            createdAtActionResult.ActionName.Should().Be(nameof(_controller.GetTag));
            createdAtActionResult.RouteValues.Should().ContainKey("id").WhoseValue.Should().Be(1);
            createdAtActionResult.Value.Should().Be(createdTag);
        }

        [Fact]
        public async Task CreateTag_InvalidModel_ReturnsBadRequest()
        {
            // Arrange
            _controller.ModelState.AddModelError("Name", "Required");

            // Act
            var result = await _controller.CreateTag(new TagDto());

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task GetTag_ValidId_ReturnsTag()
        {
            // Arrange
            var expectedTag = new TagDto { Id = 1, Name = "Test Tag" };
            _tagServiceMock.Setup(s => s.GetTagByIdAsync(1))
                .ReturnsAsync(expectedTag);

            // Act
            var result = await _controller.GetTag(1);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.Value.Should().Be(expectedTag);
        }

        [Fact]
        public async Task GetAllTags_ReturnsAllTags()
        {
            // Arrange
            var expectedTags = new List<TagDto>
            {
                new TagDto { Id = 1, Name = "Tag1" },
                new TagDto { Id = 2, Name = "Tag2" }
            };
            _tagServiceMock.Setup(s => s.GetAllTagsAsync())
                .ReturnsAsync(expectedTags);

            // Act
            var result = await _controller.GetAllTags();

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.Value.Should().BeEquivalentTo(expectedTags);
        }

        [Fact]
        public async Task UpdateTag_ValidId_ReturnsNoContent()
        {
            // Arrange
            var tagDto = new TagDto { Id = 1, Name = "Updated Tag" };

            // Act
            var result = await _controller.UpdateTag(1, tagDto);

            // Assert
            result.Should().BeOfType<NoContentResult>();
            _tagServiceMock.Verify(s => s.UpdateTagAsync(1, tagDto), Times.Once);
        }

        [Fact]
        public async Task UpdateTag_IdMismatch_ReturnsBadRequest()
        {
            // Arrange
            var tagDto = new TagDto { Id = 2, Name = "Updated Tag" };

            // Act
            var result = await _controller.UpdateTag(1, tagDto);

            // Assert
            var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            badRequestResult.Value.Should().Be("ID mismatch");
            _tagServiceMock.Verify(s => s.UpdateTagAsync(It.IsAny<int>(), It.IsAny<TagDto>()), Times.Never);
        }

        [Fact]
        public async Task DeleteTag_ValidId_ReturnsNoContent()
        {
            // Act
            var result = await _controller.DeleteTag(1);

            // Assert
            result.Should().BeOfType<NoContentResult>();
            _tagServiceMock.Verify(s => s.DeleteTagAsync(1), Times.Once);
        }
    }
}