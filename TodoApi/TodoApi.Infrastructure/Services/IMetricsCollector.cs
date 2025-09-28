namespace TodoApi.Infrastructure.Services;

public interface IMetricsCollector
{
    // Counters
    void IncrementCounter(string name, double value = 1, params (string Key, object? Value)[] tags);
    void IncrementRequestCounter(string endpoint, string method, int statusCode);
    void IncrementErrorCounter(string errorType, string? source = null);

    // Gauges
    void SetGauge(string name, double value, params (string Key, object? Value)[] tags);
    void RecordActiveUsers(int count);
    void RecordDatabaseConnections(int count);

    // Histograms
    void RecordHistogram(string name, double value, params (string Key, object? Value)[] tags);
    void RecordRequestDuration(string endpoint, string method, TimeSpan duration);
    void RecordDatabaseQueryDuration(string operation, TimeSpan duration);

    // Business Metrics
    void RecordTodoItemCreated(int userId);
    void RecordTodoItemCompleted(int userId);
    void RecordUserRegistration();
    void RecordUserLogin(bool successful);

    // Performance Metrics
    void RecordCacheHit(string cacheKey);
    void RecordCacheMiss(string cacheKey);
    void RecordBackgroundJobDuration(string jobName, TimeSpan duration, bool successful);
}