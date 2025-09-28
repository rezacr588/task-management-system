namespace TodoApi.Infrastructure.Services;

public interface ICacheService
{
    // Basic cache operations
    Task<T?> GetAsync<T>(string key) where T : class;
    Task<string?> GetStringAsync(string key);
    Task SetAsync<T>(string key, T value, TimeSpan? expiration = null) where T : class;
    Task SetStringAsync(string key, string value, TimeSpan? expiration = null);
    Task RemoveAsync(string key);
    Task RemoveByPatternAsync(string pattern);
    Task<bool> ExistsAsync(string key);

    // Advanced cache operations
    Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null) where T : class;
    Task RefreshAsync(string key);
    Task<long> IncrementAsync(string key, long value = 1);
    Task<double> IncrementAsync(string key, double value);

    // Hash operations
    Task SetHashFieldAsync(string key, string field, string value);
    Task<string?> GetHashFieldAsync(string key, string field);
    Task<Dictionary<string, string>> GetHashAsync(string key);
    Task RemoveHashFieldAsync(string key, string field);

    // List operations
    Task<long> ListLeftPushAsync(string key, string value);
    Task<long> ListRightPushAsync(string key, string value);
    Task<string?> ListLeftPopAsync(string key);
    Task<string?> ListRightPopAsync(string key);
    Task<string[]> ListRangeAsync(string key, long start = 0, long stop = -1);
    Task<long> ListLengthAsync(string key);

    // Set operations
    Task<bool> SetAddAsync(string key, string value);
    Task<bool> SetRemoveAsync(string key, string value);
    Task<bool> SetContainsAsync(string key, string value);
    Task<string[]> SetMembersAsync(string key);
    Task<long> SetLengthAsync(string key);

    // Cache statistics and management
    Task<Dictionary<string, string>> GetStatsAsync();
    Task FlushAllAsync();
    Task<string[]> GetKeysAsync(string pattern = "*");

    // Business-specific cache methods for TodoApi
    Task CacheUserAsync(int userId, object userData, TimeSpan? expiration = null);
    Task<T?> GetCachedUserAsync<T>(int userId) where T : class;
    Task InvalidateUserCacheAsync(int userId);

    Task CacheTodoItemAsync(int todoItemId, object todoItem, TimeSpan? expiration = null);
    Task<T?> GetCachedTodoItemAsync<T>(int todoItemId) where T : class;
    Task InvalidateTodoItemCacheAsync(int todoItemId);
    Task InvalidateUserTodoItemsCacheAsync(int userId);

    Task CacheTagSuggestionsAsync(string text, string[] suggestions, TimeSpan? expiration = null);
    Task<string[]?> GetCachedTagSuggestionsAsync(string text);
}