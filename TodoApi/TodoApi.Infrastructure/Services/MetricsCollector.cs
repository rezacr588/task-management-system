using Microsoft.Extensions.Logging;
using System.Diagnostics.Metrics;

namespace TodoApi.Infrastructure.Services;

public class MetricsCollector : IMetricsCollector, IDisposable
{
    private readonly ILogger<MetricsCollector> _logger;
    private readonly Meter _meter;

    // Counters
    private readonly Counter<long> _requestCounter;
    private readonly Counter<long> _errorCounter;
    private readonly Counter<long> _todoItemsCreated;
    private readonly Counter<long> _todoItemsCompleted;
    private readonly Counter<long> _userRegistrations;
    private readonly Counter<long> _userLogins;
    private readonly Counter<long> _cacheHits;
    private readonly Counter<long> _cacheMisses;

    // Histograms
    private readonly Histogram<double> _requestDuration;
    private readonly Histogram<double> _databaseQueryDuration;
    private readonly Histogram<double> _backgroundJobDuration;

    // Gauges (using ObservableGauge)
    private int _activeUsers;
    private int _databaseConnections;

    public MetricsCollector(ILogger<MetricsCollector> logger)
    {
        _logger = logger;
        _meter = new Meter("TodoApi", "1.0.0");

        // Initialize counters
        _requestCounter = _meter.CreateCounter<long>("todoapi_requests_total", "requests", "Total number of HTTP requests");
        _errorCounter = _meter.CreateCounter<long>("todoapi_errors_total", "errors", "Total number of errors");
        _todoItemsCreated = _meter.CreateCounter<long>("todoapi_todo_items_created_total", "items", "Total number of todo items created");
        _todoItemsCompleted = _meter.CreateCounter<long>("todoapi_todo_items_completed_total", "items", "Total number of todo items completed");
        _userRegistrations = _meter.CreateCounter<long>("todoapi_user_registrations_total", "registrations", "Total number of user registrations");
        _userLogins = _meter.CreateCounter<long>("todoapi_user_logins_total", "logins", "Total number of user login attempts");
        _cacheHits = _meter.CreateCounter<long>("todoapi_cache_hits_total", "hits", "Total number of cache hits");
        _cacheMisses = _meter.CreateCounter<long>("todoapi_cache_misses_total", "misses", "Total number of cache misses");

        // Initialize histograms
        _requestDuration = _meter.CreateHistogram<double>("todoapi_request_duration_seconds", "seconds", "Duration of HTTP requests");
        _databaseQueryDuration = _meter.CreateHistogram<double>("todoapi_database_query_duration_seconds", "seconds", "Duration of database queries");
        _backgroundJobDuration = _meter.CreateHistogram<double>("todoapi_background_job_duration_seconds", "seconds", "Duration of background jobs");

        // Initialize observable gauges
        _meter.CreateObservableGauge("todoapi_active_users", () => _activeUsers, "users", "Number of currently active users");
        _meter.CreateObservableGauge("todoapi_database_connections", () => _databaseConnections, "connections", "Number of active database connections");
    }

    public void IncrementCounter(string name, double value = 1, params (string Key, object? Value)[] tags)
    {
        var tagList = new TagList();
        foreach (var tag in tags)
        {
            tagList.Add(tag.Key, tag.Value);
        }

        // Since we can't create dynamic counters, log for custom metrics
        _logger.LogDebug("Custom Counter: {Name} incremented by {Value} with tags {@Tags}", name, value, tags);
    }

    public void IncrementRequestCounter(string endpoint, string method, int statusCode)
    {
        var tags = new TagList
        {
            ["endpoint"] = endpoint,
            ["method"] = method,
            ["status_code"] = statusCode.ToString()
        };

        _requestCounter.Add(1, tags);
        _logger.LogTrace("Request counter incremented: {Endpoint} {Method} -> {StatusCode}", endpoint, method, statusCode);
    }

    public void IncrementErrorCounter(string errorType, string? source = null)
    {
        var tags = new TagList
        {
            ["error_type"] = errorType
        };

        if (!string.IsNullOrEmpty(source))
        {
            tags.Add("source", source);
        }

        _errorCounter.Add(1, tags);
        _logger.LogWarning("Error counter incremented: {ErrorType} from {Source}", errorType, source ?? "Unknown");
    }

    public void SetGauge(string name, double value, params (string Key, object? Value)[] tags)
    {
        _logger.LogDebug("Custom Gauge: {Name} set to {Value} with tags {@Tags}", name, value, tags);
    }

    public void RecordActiveUsers(int count)
    {
        _activeUsers = count;
        _logger.LogTrace("Active users gauge updated: {Count}", count);
    }

    public void RecordDatabaseConnections(int count)
    {
        _databaseConnections = count;
        _logger.LogTrace("Database connections gauge updated: {Count}", count);
    }

    public void RecordHistogram(string name, double value, params (string Key, object? Value)[] tags)
    {
        _logger.LogDebug("Custom Histogram: {Name} recorded value {Value} with tags {@Tags}", name, value, tags);
    }

    public void RecordRequestDuration(string endpoint, string method, TimeSpan duration)
    {
        var tags = new TagList
        {
            ["endpoint"] = endpoint,
            ["method"] = method
        };

        _requestDuration.Record(duration.TotalSeconds, tags);
        _logger.LogTrace("Request duration recorded: {Endpoint} {Method} took {Duration}ms", 
            endpoint, method, duration.TotalMilliseconds);
    }

    public void RecordDatabaseQueryDuration(string operation, TimeSpan duration)
    {
        var tags = new TagList
        {
            ["operation"] = operation
        };

        _databaseQueryDuration.Record(duration.TotalSeconds, tags);
        
        if (duration.TotalMilliseconds > 1000) // Log slow queries
        {
            _logger.LogWarning("Slow database query detected: {Operation} took {Duration}ms", operation, duration.TotalMilliseconds);
        }
        else
        {
            _logger.LogTrace("Database query duration recorded: {Operation} took {Duration}ms", operation, duration.TotalMilliseconds);
        }
    }

    public void RecordTodoItemCreated(int userId)
    {
        var tags = new TagList
        {
            ["user_id"] = userId.ToString()
        };

        _todoItemsCreated.Add(1, tags);
        _logger.LogInformation("Todo item created by user {UserId}", userId);
    }

    public void RecordTodoItemCompleted(int userId)
    {
        var tags = new TagList
        {
            ["user_id"] = userId.ToString()
        };

        _todoItemsCompleted.Add(1, tags);
        _logger.LogInformation("Todo item completed by user {UserId}", userId);
    }

    public void RecordUserRegistration()
    {
        _userRegistrations.Add(1);
        _logger.LogInformation("User registration recorded");
    }

    public void RecordUserLogin(bool successful)
    {
        var tags = new TagList
        {
            ["successful"] = successful.ToString().ToLower()
        };

        _userLogins.Add(1, tags);
        _logger.LogInformation("User login attempt recorded: {Successful}", successful);
    }

    public void RecordCacheHit(string cacheKey)
    {
        var tags = new TagList
        {
            ["cache_key"] = cacheKey
        };

        _cacheHits.Add(1, tags);
        _logger.LogTrace("Cache hit recorded for key: {CacheKey}", cacheKey);
    }

    public void RecordCacheMiss(string cacheKey)
    {
        var tags = new TagList
        {
            ["cache_key"] = cacheKey
        };

        _cacheMisses.Add(1, tags);
        _logger.LogTrace("Cache miss recorded for key: {CacheKey}", cacheKey);
    }

    public void RecordBackgroundJobDuration(string jobName, TimeSpan duration, bool successful)
    {
        var tags = new TagList
        {
            ["job_name"] = jobName,
            ["successful"] = successful.ToString().ToLower()
        };

        _backgroundJobDuration.Record(duration.TotalSeconds, tags);
        _logger.LogInformation("Background job {JobName} completed in {Duration}ms (Success: {Successful})", 
            jobName, duration.TotalMilliseconds, successful);
    }

    public void Dispose()
    {
        _meter?.Dispose();
    }
}