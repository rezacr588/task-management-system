using Microsoft.Extensions.Logging;

namespace TodoApi.Infrastructure.Logging;

public interface IStructuredLogger<T>
{
    void LogInformation(string message, params object[] args);
    void LogWarning(string message, params object[] args);
    void LogError(string message, Exception? exception = null, params object[] args);
    void LogDebug(string message, params object[] args);
    void LogTrace(string message, params object[] args);
    
    // Structured logging with context
    void LogWithContext(LogLevel level, string message, object context, Exception? exception = null);
    
    // Performance logging
    IDisposable BeginScope(string operationName, object? context = null);
    void LogPerformance(string operationName, TimeSpan duration, bool success, object? context = null);
    
    // Business events
    void LogUserAction(string action, int userId, object? details = null);
    void LogSystemEvent(string eventName, object? context = null);
    void LogSecurityEvent(string eventName, string? userId = null, object? details = null);
}