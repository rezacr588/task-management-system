using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using TodoApi.Application.Interfaces;
using TodoApi.WebApi.Controllers;
using Xunit;

namespace TodoApi.Tests.Unit.Controllers
{
    public class TagSuggestionsControllerTests
    {
        private readonly Mock<ITagSuggestionService> _tagSuggestionServiceMock = new();
        private readonly TagSuggestionsController _controller;

        public TagSuggestionsControllerTests()
        {
            _controller = new TagSuggestionsController(_tagSuggestionServiceMock.Object);
        }

        [Fact]
        public async Task SuggestTags_ValidText_ReturnsTags()
        {
            // Arrange
            var inputText = "Implement user authentication and authorization";
            var expectedTags = new string[] { "authentication", "authorization", "security" };
            _tagSuggestionServiceMock.Setup(s => s.SuggestTagsAsync(inputText))
                .ReturnsAsync(expectedTags);

            // Act
            var result = await _controller.SuggestTags(inputText);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.Value.Should().BeEquivalentTo(expectedTags);
        }

        [Fact]
        public async Task SuggestTags_EmptyText_ReturnsEmptyList()
        {
            // Arrange
            var inputText = "";
            var expectedTags = new string[] { };
            _tagSuggestionServiceMock.Setup(s => s.SuggestTagsAsync(inputText))
                .ReturnsAsync(expectedTags);

            // Act
            var result = await _controller.SuggestTags(inputText);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.Value.Should().BeEquivalentTo(expectedTags);
        }

        [Fact]
        public async Task SuggestTags_NullText_ReturnsEmptyList()
        {
            // Arrange
            string? inputText = null;
            var expectedTags = new string[] { };
            _tagSuggestionServiceMock.Setup(s => s.SuggestTagsAsync(inputText!))
                .ReturnsAsync(expectedTags);

            // Act
            var result = await _controller.SuggestTags(inputText!);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.Value.Should().BeEquivalentTo(expectedTags);
        }
    }
}