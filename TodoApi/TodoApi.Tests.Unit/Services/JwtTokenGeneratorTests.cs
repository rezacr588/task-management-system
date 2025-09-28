using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using TodoApi.Application.DTOs;
using TodoApi.Infrastructure.Services;
using Xunit;
using FluentAssertions;

namespace TodoApi.Tests.Unit.Services;

public class JwtTokenGeneratorTests
{
    private readonly Mock<ILogger<JwtTokenGenerator>> _loggerMock;
    private readonly Mock<IConfiguration> _configurationMock;
    private readonly JwtTokenGenerator _tokenGenerator;

    public JwtTokenGeneratorTests()
    {
        _loggerMock = new Mock<ILogger<JwtTokenGenerator>>();
        _configurationMock = new Mock<IConfiguration>();

        // Setup configuration
        _configurationMock.Setup(x => x["Jwt:Key"]).Returns("test-secret-key-for-jwt-token-generation-must-be-at-least-32-characters-long");
        _configurationMock.Setup(x => x["Jwt:Issuer"]).Returns("TestIssuer");
        _configurationMock.Setup(x => x["Jwt:Audience"]).Returns("TestAudience");

        _tokenGenerator = new JwtTokenGenerator(_configurationMock.Object, _loggerMock.Object);
    }

    [Fact]
    public void GenerateToken_WithValidUserDto_ShouldReturnJwtToken()
    {
        // Arrange
        var userDto = new UserDto
        {
            Id = 123,
            Name = "Test User",
            Email = "test@example.com",
            Role = "User"
        };

        // Act
        var token = _tokenGenerator.GenerateToken(userDto);

        // Assert
        token.Should().NotBeNullOrEmpty();

        // Verify it's a valid JWT token
        var handler = new JwtSecurityTokenHandler();
        var canRead = handler.CanReadToken(token);
        canRead.Should().BeTrue();

        var jwtToken = handler.ReadJwtToken(token);
        jwtToken.Claims.Should().Contain(c => c.Type == "userId" && c.Value == "123");
        jwtToken.Claims.Should().Contain(c => c.Type == "name" && c.Value == "Test User");
        jwtToken.Claims.Should().Contain(c => c.Type == "email" && c.Value == "test@example.com");
        jwtToken.Claims.Should().Contain(c => c.Type == ClaimTypes.Role && c.Value == "User");
        jwtToken.Issuer.Should().Be("TestIssuer");
        jwtToken.Audiences.Should().Contain("TestAudience");
    }

    [Fact]
    public void GenerateToken_WithAdminUser_ShouldIncludeAdminRole()
    {
        // Arrange
        var adminUserDto = new UserDto
        {
            Id = 456,
            Name = "Admin User",
            Email = "admin@example.com",
            Role = "Admin"
        };

        // Act
        var token = _tokenGenerator.GenerateToken(adminUserDto);

        // Assert
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);
        jwtToken.Claims.Should().Contain(c => c.Type == ClaimTypes.Role && c.Value == "Admin");
    }

    [Fact]
    public void GenerateToken_ShouldIncludeExpirationClaim()
    {
        // Arrange
        var userDto = new UserDto
        {
            Id = 789,
            Name = "Test User",
            Email = "test@example.com",
            Role = "User"
        };

        // Act
        var token = _tokenGenerator.GenerateToken(userDto);

        // Assert
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        var expClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "exp");
        expClaim.Should().NotBeNull();

        var expTime = DateTimeOffset.FromUnixTimeSeconds(long.Parse(expClaim!.Value));
        var expectedExpTime = DateTime.UtcNow.AddHours(24);
        
        // Allow for some variance in time (within 1 minute)
        expTime.Should().BeCloseTo(expectedExpTime, TimeSpan.FromMinutes(1));
    }

    [Fact]
    public void GenerateToken_ShouldIncludeIssuedAtClaim()
    {
        // Arrange
        var userDto = new UserDto
        {
            Id = 999,
            Name = "Test User",
            Email = "test@example.com",
            Role = "User"
        };

        // Act
        var token = _tokenGenerator.GenerateToken(userDto);

        // Assert
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        var iatClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "iat");
        iatClaim.Should().NotBeNull();

        var issuedTime = DateTimeOffset.FromUnixTimeSeconds(long.Parse(iatClaim!.Value));
        issuedTime.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
    }

    [Fact]
    public void GenerateToken_WithDifferentUsers_ShouldReturnDifferentTokens()
    {
        // Arrange
        var user1 = new UserDto
        {
            Id = 1,
            Name = "User One",
            Email = "user1@example.com",
            Role = "User"
        };

        var user2 = new UserDto
        {
            Id = 2,
            Name = "User Two",
            Email = "user2@example.com",
            Role = "User"
        };

        // Act
        var token1 = _tokenGenerator.GenerateToken(user1);
        var token2 = _tokenGenerator.GenerateToken(user2);

        // Assert
        token1.Should().NotBe(token2);
    }

    [Fact]
    public void GenerateToken_ShouldLogTokenGeneration()
    {
        // Arrange
        var userDto = new UserDto
        {
            Id = 555,
            Name = "Log Test User",
            Email = "logtest@example.com",
            Role = "User"
        };

        // Act
        _tokenGenerator.GenerateToken(userDto);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("JWT token generated for user")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("short")]
    public void Constructor_WithInvalidJwtKey_ShouldThrowException(string? invalidKey)
    {
        // Arrange
        var invalidConfigMock = new Mock<IConfiguration>();
        invalidConfigMock.Setup(x => x["Jwt:Key"]).Returns(invalidKey);
        invalidConfigMock.Setup(x => x["Jwt:Issuer"]).Returns("TestIssuer");
        invalidConfigMock.Setup(x => x["Jwt:Audience"]).Returns("TestAudience");

        // Act & Assert
        var act = () => new JwtTokenGenerator(invalidConfigMock.Object, _loggerMock.Object);
        act.Should().Throw<InvalidOperationException>()
           .WithMessage("*JWT key must be at least 32 characters*");
    }

    [Theory]
    [InlineData("Jwt:Issuer", null)]
    [InlineData("Jwt:Issuer", "")]
    [InlineData("Jwt:Audience", null)]
    [InlineData("Jwt:Audience", "")]
    public void Constructor_WithMissingConfiguration_ShouldThrowException(string configKey, string? configValue)
    {
        // Arrange
        var invalidConfigMock = new Mock<IConfiguration>();
        invalidConfigMock.Setup(x => x["Jwt:Key"]).Returns("test-secret-key-for-jwt-token-generation-must-be-at-least-32-characters-long");
        invalidConfigMock.Setup(x => x["Jwt:Issuer"]).Returns(configKey == "Jwt:Issuer" ? configValue : "TestIssuer");
        invalidConfigMock.Setup(x => x["Jwt:Audience"]).Returns(configKey == "Jwt:Audience" ? configValue : "TestAudience");

        // Act & Assert
        var act = () => new JwtTokenGenerator(invalidConfigMock.Object, _loggerMock.Object);
        act.Should().Throw<InvalidOperationException>()
           .WithMessage("*configuration*");
    }
}