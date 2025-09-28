using Microsoft.Extensions.Logging;
using Moq;
using TodoApi.Infrastructure.Services;
using Xunit;
using FluentAssertions;

namespace TodoApi.Tests.Unit.Services;

public class BiometricTokenValidatorTests
{
    private readonly Mock<ILogger<BiometricTokenValidator>> _loggerMock;
    private readonly BiometricTokenValidator _tokenValidator;

    public BiometricTokenValidatorTests()
    {
        _loggerMock = new Mock<ILogger<BiometricTokenValidator>>();
        _tokenValidator = new BiometricTokenValidator(_loggerMock.Object);
    }

    [Fact]
    public async Task ValidateTokenAsync_WithValidToken_ShouldReturnTrue()
    {
        // Arrange
        var validToken = "valid-biometric-token-12345";
        var userId = 123;

        // Act
        var result = await _tokenValidator.ValidateTokenAsync(validToken, userId);

        // Assert
        result.Should().BeTrue();
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Biometric token validation successful")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task ValidateTokenAsync_WithInvalidToken_ShouldReturnFalse(string? invalidToken)
    {
        // Arrange
        var userId = 123;

        // Act
        var result = await _tokenValidator.ValidateTokenAsync(invalidToken!, userId);

        // Assert
        result.Should().BeFalse();
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Invalid biometric token")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task ValidateTokenAsync_WithShortToken_ShouldReturnFalse()
    {
        // Arrange
        var shortToken = "short";
        var userId = 123;

        // Act
        var result = await _tokenValidator.ValidateTokenAsync(shortToken, userId);

        // Assert
        result.Should().BeFalse();
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Invalid biometric token")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task ValidateTokenAsync_WithNegativeUserId_ShouldReturnFalse()
    {
        // Arrange
        var validToken = "valid-biometric-token-12345";
        var invalidUserId = -1;

        // Act
        var result = await _tokenValidator.ValidateTokenAsync(validToken, invalidUserId);

        // Assert
        result.Should().BeFalse();
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Invalid user ID")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task ValidateTokenAsync_WithZeroUserId_ShouldReturnFalse()
    {
        // Arrange
        var validToken = "valid-biometric-token-12345";
        var invalidUserId = 0;

        // Act
        var result = await _tokenValidator.ValidateTokenAsync(validToken, invalidUserId);

        // Assert
        result.Should().BeFalse();
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Invalid user ID")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task GenerateTokenAsync_ShouldReturnNonEmptyString()
    {
        // Arrange
        var userId = 123;

        // Act
        var result = await _tokenValidator.GenerateTokenAsync(userId);

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Should().Contain(userId.ToString());
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Generated biometric token")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task GenerateTokenAsync_WithDifferentUserIds_ShouldReturnDifferentTokens()
    {
        // Arrange
        var userId1 = 123;
        var userId2 = 456;

        // Act
        var token1 = await _tokenValidator.GenerateTokenAsync(userId1);
        var token2 = await _tokenValidator.GenerateTokenAsync(userId2);

        // Assert
        token1.Should().NotBe(token2);
        token1.Should().Contain(userId1.ToString());
        token2.Should().Contain(userId2.ToString());
    }

    [Fact]
    public async Task ValidateTokenAsync_WithGeneratedToken_ShouldReturnTrue()
    {
        // Arrange
        var userId = 123;
        var generatedToken = await _tokenValidator.GenerateTokenAsync(userId);

        // Act
        var result = await _tokenValidator.ValidateTokenAsync(generatedToken, userId);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateTokenAsync_WithGeneratedTokenForDifferentUser_ShouldReturnFalse()
    {
        // Arrange
        var userId1 = 123;
        var userId2 = 456;
        var tokenForUser1 = await _tokenValidator.GenerateTokenAsync(userId1);

        // Act
        var result = await _tokenValidator.ValidateTokenAsync(tokenForUser1, userId2);

        // Assert
        result.Should().BeFalse();
    }
}