using FluentAssertions;
using Moq;
using TodoApi.Application.Services;
using Xunit;

namespace TodoApi.Tests.Unit.Services
{
    public class TagSuggestionServiceTests
    {
        [Fact]
        public async Task SuggestTagsAsync_ShouldReturnEmpty_WhenUseDummy()
        {
            // Arrange
            var service = new TagSuggestionService(null, null, true);

            // Act
            var result = await service.SuggestTagsAsync("some text");

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public async Task SuggestTagsAsync_ShouldReturnEmpty_WhenTextIsEmpty()
        {
            // Arrange
            var service = new TagSuggestionService(null, null, true);

            // Act
            var result = await service.SuggestTagsAsync("");

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public async Task SuggestTagsAsync_ShouldReturnEmpty_WhenTextIsWhitespace()
        {
            // Arrange
            var service = new TagSuggestionService(null, null, true);

            // Act
            var result = await service.SuggestTagsAsync("   ");

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public async Task SuggestTagsAsync_ShouldReturnEmpty_WhenClientIsNull()
        {
            // Arrange
            var service = new TagSuggestionService(null, null);

            // Act
            var result = await service.SuggestTagsAsync("some text");

            // Assert
            result.Should().BeEmpty();
        }

        // Note: Testing with actual Azure client would require integration tests with mocked HTTP
        // For unit tests, we rely on the dummy mode and null checks
    }
}