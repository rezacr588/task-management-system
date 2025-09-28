using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Text.Json;

namespace TodoApi.Infrastructure.Logging;

public class StructuredLogger<T> : IStructuredLogger<T>
{
    private readonly ILogger<T> _logger;

    public StructuredLogger(ILogger<T> logger)
    {
        _logger = logger;
    }

    public void LogInformation(string message, params object[] args)
    {
        _logger.LogInformation(message, args);
    }

    public void LogWarning(string message, params object[] args)
    {
        _logger.LogWarning(message, args);
    }

    public void LogError(string message, Exception? exception = null, params object[] args)
    {
        _logger.LogError(exception, message, args);
    }

    public void LogDebug(string message, params object[] args)
    {
        _logger.LogDebug(message, args);
    }

    public void LogTrace(string message, params object[] args)
    {
        _logger.LogTrace(message, args);
    }

    public void LogWithContext(LogLevel level, string message, object context, Exception? exception = null)
    {
        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["Context"] = JsonSerializer.Serialize(context),
            ["Timestamp"] = DateTimeOffset.UtcNow
        });

        _logger.Log(level, exception, message);
    }

    public IDisposable BeginScope(string operationName, object? context = null)
    {
        var scopeData = new Dictionary<string, object>
        {
            ["Operation"] = operationName,
            ["StartTime"] = DateTimeOffset.UtcNow,
            ["TraceId"] = Activity.Current?.TraceId.ToString() ?? Guid.NewGuid().ToString()
        };

        if (context != null)
        {
            scopeData["Context"] = JsonSerializer.Serialize(context);
        }

        return _logger.BeginScope(scopeData);
    }

    public void LogPerformance(string operationName, TimeSpan duration, bool success, object? context = null)
    {
        var performanceData = new
        {
            Operation = operationName,
            DurationMs = duration.TotalMilliseconds,
            Success = success,
            Context = context,
            Timestamp = DateTimeOffset.UtcNow
        };

        var level = duration.TotalMilliseconds > 1000 ? LogLevel.Warning : LogLevel.Information;
        
        _logger.Log(level, "Performance: {Operation} completed in {DurationMs}ms (Success: {Success})",
            operationName, duration.TotalMilliseconds, success);

        // Log structured data for external systems (APM, metrics)
        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["PerformanceMetric"] = JsonSerializer.Serialize(performanceData)
        });
    }

    public void LogUserAction(string action, int userId, object? details = null)
    {
        var actionData = new
        {
            Action = action,
            UserId = userId,
            Details = details,
            Timestamp = DateTimeOffset.UtcNow,
            TraceId = Activity.Current?.TraceId.ToString()
        };

        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["UserAction"] = JsonSerializer.Serialize(actionData)
        });

        _logger.LogInformation("User Action: {Action} by User {UserId}", action, userId);
    }

    public void LogSystemEvent(string eventName, object? context = null)
    {
        var eventData = new
        {
            EventName = eventName,
            Context = context,
            Timestamp = DateTimeOffset.UtcNow,
            Machine = Environment.MachineName,
            TraceId = Activity.Current?.TraceId.ToString()
        };

        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["SystemEvent"] = JsonSerializer.Serialize(eventData)
        });

        _logger.LogInformation("System Event: {EventName}", eventName);
    }

    public void LogSecurityEvent(string eventName, string? userId = null, object? details = null)
    {
        var securityData = new
        {
            EventName = eventName,
            UserId = userId,
            Details = details,
            Timestamp = DateTimeOffset.UtcNow,
            IpAddress = "Unknown", // This would be set by middleware
            UserAgent = "Unknown", // This would be set by middleware
            TraceId = Activity.Current?.TraceId.ToString()
        };

        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["SecurityEvent"] = JsonSerializer.Serialize(securityData)
        });

        // Security events are always logged as warnings or higher
        _logger.LogWarning("Security Event: {EventName} for User {UserId}", eventName, userId ?? "Anonymous");
    }
}