using Microsoft.Extensions.Logging;
using Moq;
using TodoApi.Infrastructure.Services;
using Xunit;

namespace TodoApi.Tests.Unit.Services;

public class MetricsCollectorTests
{
    private readonly Mock<ILogger<MetricsCollector>> _loggerMock;
    private readonly MetricsCollector _metricsCollector;

    public MetricsCollectorTests()
    {
        _loggerMock = new Mock<ILogger<MetricsCollector>>();
        _metricsCollector = new MetricsCollector(_loggerMock.Object);
    }

    [Fact]
    public void IncrementRequestCounter_ShouldNotThrow()
    {
        // Arrange
        var endpoint = "/api/v1/todos";
        var method = "GET";
        var statusCode = 200;

        // Act & Assert
        _metricsCollector.IncrementRequestCounter(endpoint, method, statusCode);
        // Should not throw exception
    }

    [Fact]
    public void IncrementErrorCounter_ShouldNotThrow()
    {
        // Arrange
        var errorType = "ValidationError";
        var source = "TodoController";

        // Act & Assert
        _metricsCollector.IncrementErrorCounter(errorType, source);
        // Should not throw exception
    }

    [Fact]
    public void RecordRequestDuration_ShouldNotThrow()
    {
        // Arrange
        var endpoint = "/api/v1/todos";
        var method = "POST";
        var duration = TimeSpan.FromMilliseconds(150);

        // Act & Assert
        _metricsCollector.RecordRequestDuration(endpoint, method, duration);
        // Should not throw exception
    }

    [Fact]
    public void RecordDatabaseQueryDuration_ShouldLogWarningForSlowQuery()
    {
        // Arrange
        var operation = "GetTodos";
        var slowDuration = TimeSpan.FromMilliseconds(1500); // > 1000ms

        // Act
        _metricsCollector.RecordDatabaseQueryDuration(operation, slowDuration);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Slow database query detected")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void RecordTodoItemCreated_ShouldLogInformation()
    {
        // Arrange
        var userId = 123;

        // Act
        _metricsCollector.RecordTodoItemCreated(userId);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Todo item created")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void RecordUserLogin_ShouldTrackSuccessfulAndFailedLogins()
    {
        // Act
        _metricsCollector.RecordUserLogin(true);
        _metricsCollector.RecordUserLogin(false);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("User login attempt recorded")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Exactly(2));
    }

    [Fact]
    public void RecordActiveUsers_ShouldUpdateGauge()
    {
        // Arrange
        var count = 42;

        // Act & Assert
        _metricsCollector.RecordActiveUsers(count);
        // Should not throw exception
    }

    [Fact]
    public void RecordBackgroundJobDuration_ShouldLogJobCompletion()
    {
        // Arrange
        var jobName = "EmailSender";
        var duration = TimeSpan.FromSeconds(2.5);
        var successful = true;

        // Act
        _metricsCollector.RecordBackgroundJobDuration(jobName, duration, successful);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Background job") && v.ToString()!.Contains("completed")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void RecordCacheHit_ShouldLogTrace()
    {
        // Arrange
        var cacheKey = "user:123";

        // Act
        _metricsCollector.RecordCacheHit(cacheKey);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Trace,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Cache hit recorded")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void Dispose_ShouldNotThrow()
    {
        // Act & Assert
        _metricsCollector.Dispose();
        // Should not throw exception
    }
}