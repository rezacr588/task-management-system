using Microsoft.Extensions.Logging;
using Moq;
using TodoApi.Infrastructure.Logging;
using Xunit;
using FluentAssertions;

namespace TodoApi.Tests.Unit.Services;

public class StructuredLoggerTests
{
    private readonly Mock<ILogger<TestClass>> _loggerMock;
    private readonly StructuredLogger<TestClass> _structuredLogger;

    public StructuredLoggerTests()
    {
        _loggerMock = new Mock<ILogger<TestClass>>();
        _structuredLogger = new StructuredLogger<TestClass>(_loggerMock.Object);
    }

    [Fact]
    public void LogInformation_ShouldCallUnderlyingLogger()
    {
        // Arrange
        var message = "Test information message";
        var args = new object[] { "arg1", 42 };

        // Act
        _structuredLogger.LogInformation(message, args);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString() == message),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void LogWarning_ShouldCallUnderlyingLogger()
    {
        // Arrange
        var message = "Test warning message";
        var args = new object[] { "warning" };

        // Act
        _structuredLogger.LogWarning(message, args);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString() == message),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void LogError_ShouldCallUnderlyingLoggerWithException()
    {
        // Arrange
        var message = "Test error message";
        var exception = new InvalidOperationException("Test exception");
        var args = new object[] { "error" };

        // Act
        _structuredLogger.LogError(message, exception, args);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString() == message),
                exception,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void LogDebug_ShouldCallUnderlyingLogger()
    {
        // Arrange
        var message = "Test debug message";

        // Act
        _structuredLogger.LogDebug(message);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString() == message),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void LogTrace_ShouldCallUnderlyingLogger()
    {
        // Arrange
        var message = "Test trace message";

        // Act
        _structuredLogger.LogTrace(message);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Trace,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString() == message),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void LogWithContext_ShouldCreateScopeAndLog()
    {
        // Arrange
        var message = "Test message with context";
        var context = new { UserId = 123, Action = "Test" };
        var exception = new Exception("Test exception");

        var scopeMock = new Mock<IDisposable>();
        _loggerMock.Setup(x => x.BeginScope(It.IsAny<It.IsAnyType>()))
                   .Returns(scopeMock.Object);

        // Act
        _structuredLogger.LogWithContext(LogLevel.Information, message, context, exception);

        // Assert
        _loggerMock.Verify(x => x.BeginScope(It.IsAny<It.IsAnyType>()), Times.Once);
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString() == message),
                exception,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void BeginScope_ShouldCreateScopeWithOperationName()
    {
        // Arrange
        var operationName = "TestOperation";
        var context = new { TestData = "value" };

        var scopeMock = new Mock<IDisposable>();
        _loggerMock.Setup(x => x.BeginScope(It.IsAny<It.IsAnyType>()))
                   .Returns(scopeMock.Object);

        // Act
        var result = _structuredLogger.BeginScope(operationName, context);

        // Assert
        result.Should().NotBeNull();
        _loggerMock.Verify(x => x.BeginScope(It.IsAny<It.IsAnyType>()), Times.Once);
    }

    [Fact]
    public void BeginScope_WithoutContext_ShouldCreateScope()
    {
        // Arrange
        var operationName = "SimpleOperation";

        var scopeMock = new Mock<IDisposable>();
        _loggerMock.Setup(x => x.BeginScope(It.IsAny<It.IsAnyType>()))
                   .Returns(scopeMock.Object);

        // Act
        var result = _structuredLogger.BeginScope(operationName);

        // Assert
        result.Should().NotBeNull();
        _loggerMock.Verify(x => x.BeginScope(It.IsAny<It.IsAnyType>()), Times.Once);
    }

    [Fact]
    public void LogPerformance_WithFastOperation_ShouldLogInformation()
    {
        // Arrange
        var operationName = "FastOperation";
        var duration = TimeSpan.FromMilliseconds(500);
        var success = true;
        var context = new { ItemCount = 10 };

        var scopeMock = new Mock<IDisposable>();
        _loggerMock.Setup(x => x.BeginScope(It.IsAny<It.IsAnyType>()))
                   .Returns(scopeMock.Object);

        // Act
        _structuredLogger.LogPerformance(operationName, duration, success, context);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Performance") && v.ToString()!.Contains(operationName)),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void LogPerformance_WithSlowOperation_ShouldLogWarning()
    {
        // Arrange
        var operationName = "SlowOperation";
        var duration = TimeSpan.FromMilliseconds(1500); // > 1000ms
        var success = true;

        var scopeMock = new Mock<IDisposable>();
        _loggerMock.Setup(x => x.BeginScope(It.IsAny<It.IsAnyType>()))
                   .Returns(scopeMock.Object);

        // Act
        _structuredLogger.LogPerformance(operationName, duration, success);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Performance") && v.ToString()!.Contains(operationName)),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void LogUserAction_ShouldCreateScopeAndLog()
    {
        // Arrange
        var action = "CreateTodoItem";
        var userId = 123;
        var details = new { Title = "Test Task", Priority = "High" };

        var scopeMock = new Mock<IDisposable>();
        _loggerMock.Setup(x => x.BeginScope(It.IsAny<It.IsAnyType>()))
                   .Returns(scopeMock.Object);

        // Act
        _structuredLogger.LogUserAction(action, userId, details);

        // Assert
        _loggerMock.Verify(x => x.BeginScope(It.IsAny<It.IsAnyType>()), Times.Once);
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("User Action") && v.ToString()!.Contains(action)),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void LogSystemEvent_ShouldCreateScopeAndLog()
    {
        // Arrange
        var eventName = "DatabaseMigration";
        var context = new { Version = "1.2.3", Duration = "5 minutes" };

        var scopeMock = new Mock<IDisposable>();
        _loggerMock.Setup(x => x.BeginScope(It.IsAny<It.IsAnyType>()))
                   .Returns(scopeMock.Object);

        // Act
        _structuredLogger.LogSystemEvent(eventName, context);

        // Assert
        _loggerMock.Verify(x => x.BeginScope(It.IsAny<It.IsAnyType>()), Times.Once);
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("System Event") && v.ToString()!.Contains(eventName)),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void LogSecurityEvent_ShouldCreateScopeAndLogWarning()
    {
        // Arrange
        var eventName = "UnauthorizedAccess";
        var userId = "user123";
        var details = new { IpAddress = "192.168.1.1", Endpoint = "/api/admin" };

        var scopeMock = new Mock<IDisposable>();
        _loggerMock.Setup(x => x.BeginScope(It.IsAny<It.IsAnyType>()))
                   .Returns(scopeMock.Object);

        // Act
        _structuredLogger.LogSecurityEvent(eventName, userId, details);

        // Assert
        _loggerMock.Verify(x => x.BeginScope(It.IsAny<It.IsAnyType>()), Times.Once);
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Security Event") && v.ToString()!.Contains(eventName)),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void LogSecurityEvent_WithoutUserId_ShouldLogAnonymous()
    {
        // Arrange
        var eventName = "SuspiciousActivity";

        var scopeMock = new Mock<IDisposable>();
        _loggerMock.Setup(x => x.BeginScope(It.IsAny<It.IsAnyType>()))
                   .Returns(scopeMock.Object);

        // Act
        _structuredLogger.LogSecurityEvent(eventName);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Anonymous")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    private class TestClass { }
}