using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System.Text.Json;

namespace TodoApi.Infrastructure.Services;

public class RedisCacheService : ICacheService, IDisposable
{
    private readonly IDatabase _database;
    private readonly IConnectionMultiplexer _connectionMultiplexer;
    private readonly ILogger<RedisCacheService> _logger;
    private readonly IMetricsCollector _metrics;
    private readonly JsonSerializerOptions _jsonOptions;

    public RedisCacheService(
        IConnectionMultiplexer connectionMultiplexer,
        ILogger<RedisCacheService> logger,
        IMetricsCollector metrics)
    {
        _connectionMultiplexer = connectionMultiplexer;
        _database = connectionMultiplexer.GetDatabase();
        _logger = logger;
        _metrics = metrics;
        
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
    }

    public async Task<T?> GetAsync<T>(string key) where T : class
    {
        try
        {
            var value = await _database.StringGetAsync(key);
            if (!value.HasValue)
            {
                _metrics.RecordCacheMiss(key);
                return null;
            }

            _metrics.RecordCacheHit(key);
            var result = JsonSerializer.Deserialize<T>(value!, _jsonOptions);
            _logger.LogTrace("Cache hit for key: {Key}", key);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cached item with key: {Key}", key);
            _metrics.RecordCacheMiss(key);
            return null;
        }
    }

    public async Task<string?> GetStringAsync(string key)
    {
        try
        {
            var value = await _database.StringGetAsync(key);
            if (!value.HasValue)
            {
                _metrics.RecordCacheMiss(key);
                return null;
            }

            _metrics.RecordCacheHit(key);
            _logger.LogTrace("Cache hit for string key: {Key}", key);
            return value!;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cached string with key: {Key}", key);
            _metrics.RecordCacheMiss(key);
            return null;
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null) where T : class
    {
        try
        {
            var serialized = JsonSerializer.Serialize(value, _jsonOptions);
            var success = await _database.StringSetAsync(key, serialized, expiration);
            
            if (success)
            {
                _logger.LogTrace("Cached item with key: {Key} (expires: {Expiration})", key, expiration);
            }
            else
            {
                _logger.LogWarning("Failed to cache item with key: {Key}", key);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error caching item with key: {Key}", key);
        }
    }

    public async Task SetStringAsync(string key, string value, TimeSpan? expiration = null)
    {
        try
        {
            var success = await _database.StringSetAsync(key, value, expiration);
            
            if (success)
            {
                _logger.LogTrace("Cached string with key: {Key} (expires: {Expiration})", key, expiration);
            }
            else
            {
                _logger.LogWarning("Failed to cache string with key: {Key}", key);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error caching string with key: {Key}", key);
        }
    }

    public async Task RemoveAsync(string key)
    {
        try
        {
            var removed = await _database.KeyDeleteAsync(key);
            if (removed)
            {
                _logger.LogTrace("Removed cached item with key: {Key}", key);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing cached item with key: {Key}", key);
        }
    }

    public async Task RemoveByPatternAsync(string pattern)
    {
        try
        {
            var server = _connectionMultiplexer.GetServer(_connectionMultiplexer.GetEndPoints().First());
            var keys = server.Keys(pattern: pattern);
            
            var keysArray = keys.ToArray();
            if (keysArray.Length > 0)
            {
                await _database.KeyDeleteAsync(keysArray);
                _logger.LogInformation("Removed {Count} cached items matching pattern: {Pattern}", keysArray.Length, pattern);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing cached items with pattern: {Pattern}", pattern);
        }
    }

    public async Task<bool> ExistsAsync(string key)
    {
        try
        {
            return await _database.KeyExistsAsync(key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if key exists: {Key}", key);
            return false;
        }
    }

    public async Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null) where T : class
    {
        var cached = await GetAsync<T>(key);
        if (cached != null)
        {
            return cached;
        }

        var value = await factory();
        await SetAsync(key, value, expiration);
        return value;
    }

    public async Task RefreshAsync(string key)
    {
        try
        {
            var ttl = await _database.KeyTimeToLiveAsync(key);
            if (ttl.HasValue)
            {
                await _database.KeyExpireAsync(key, ttl.Value);
                _logger.LogTrace("Refreshed TTL for key: {Key}", key);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing TTL for key: {Key}", key);
        }
    }

    public async Task<long> IncrementAsync(string key, long value = 1)
    {
        try
        {
            return await _database.StringIncrementAsync(key, value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error incrementing key: {Key} by {Value}", key, value);
            return 0;
        }
    }

    public async Task<double> IncrementAsync(string key, double value)
    {
        try
        {
            return await _database.StringIncrementAsync(key, value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error incrementing key: {Key} by {Value}", key, value);
            return 0;
        }
    }

    // Hash operations
    public async Task SetHashFieldAsync(string key, string field, string value)
    {
        try
        {
            await _database.HashSetAsync(key, field, value);
            _logger.LogTrace("Set hash field {Field} in {Key}", field, key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting hash field {Field} in {Key}", field, key);
        }
    }

    public async Task<string?> GetHashFieldAsync(string key, string field)
    {
        try
        {
            var value = await _database.HashGetAsync(key, field);
            return value.HasValue ? value! : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting hash field {Field} from {Key}", field, key);
            return null;
        }
    }

    public async Task<Dictionary<string, string>> GetHashAsync(string key)
    {
        try
        {
            var hash = await _database.HashGetAllAsync(key);
            return hash.ToDictionary(kv => kv.Name!, kv => kv.Value!);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting hash from {Key}", key);
            return new Dictionary<string, string>();
        }
    }

    public async Task RemoveHashFieldAsync(string key, string field)
    {
        try
        {
            await _database.HashDeleteAsync(key, field);
            _logger.LogTrace("Removed hash field {Field} from {Key}", field, key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing hash field {Field} from {Key}", field, key);
        }
    }

    // List operations
    public async Task<long> ListLeftPushAsync(string key, string value)
    {
        try
        {
            return await _database.ListLeftPushAsync(key, value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error left-pushing to list {Key}", key);
            return 0;
        }
    }

    public async Task<long> ListRightPushAsync(string key, string value)
    {
        try
        {
            return await _database.ListRightPushAsync(key, value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error right-pushing to list {Key}", key);
            return 0;
        }
    }

    public async Task<string?> ListLeftPopAsync(string key)
    {
        try
        {
            var value = await _database.ListLeftPopAsync(key);
            return value.HasValue ? value! : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error left-popping from list {Key}", key);
            return null;
        }
    }

    public async Task<string?> ListRightPopAsync(string key)
    {
        try
        {
            var value = await _database.ListRightPopAsync(key);
            return value.HasValue ? value! : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error right-popping from list {Key}", key);
            return null;
        }
    }

    public async Task<string[]> ListRangeAsync(string key, long start = 0, long stop = -1)
    {
        try
        {
            var values = await _database.ListRangeAsync(key, start, stop);
            return values.Select(v => v.ToString()).ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting range from list {Key}", key);
            return Array.Empty<string>();
        }
    }

    public async Task<long> ListLengthAsync(string key)
    {
        try
        {
            return await _database.ListLengthAsync(key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting length of list {Key}", key);
            return 0;
        }
    }

    // Set operations
    public async Task<bool> SetAddAsync(string key, string value)
    {
        try
        {
            return await _database.SetAddAsync(key, value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding to set {Key}", key);
            return false;
        }
    }

    public async Task<bool> SetRemoveAsync(string key, string value)
    {
        try
        {
            return await _database.SetRemoveAsync(key, value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing from set {Key}", key);
            return false;
        }
    }

    public async Task<bool> SetContainsAsync(string key, string value)
    {
        try
        {
            return await _database.SetContainsAsync(key, value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking set {Key} contains value", key);
            return false;
        }
    }

    public async Task<string[]> SetMembersAsync(string key)
    {
        try
        {
            var values = await _database.SetMembersAsync(key);
            return values.Select(v => v.ToString()).ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting members of set {Key}", key);
            return Array.Empty<string>();
        }
    }

    public async Task<long> SetLengthAsync(string key)
    {
        try
        {
            return await _database.SetLengthAsync(key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting length of set {Key}", key);
            return 0;
        }
    }

    // Cache management
    public async Task<Dictionary<string, string>> GetStatsAsync()
    {
        try
        {
            var server = _connectionMultiplexer.GetServer(_connectionMultiplexer.GetEndPoints().First());
            var info = await server.InfoAsync();
            
            return new Dictionary<string, string>
            {
                ["connected_clients"] = info.FirstOrDefault(i => i.Key == "connected_clients").Value ?? "unknown",
                ["used_memory"] = info.FirstOrDefault(i => i.Key == "used_memory_human").Value ?? "unknown",
                ["total_commands_processed"] = info.FirstOrDefault(i => i.Key == "total_commands_processed").Value ?? "unknown",
                ["keyspace_hits"] = info.FirstOrDefault(i => i.Key == "keyspace_hits").Value ?? "unknown",
                ["keyspace_misses"] = info.FirstOrDefault(i => i.Key == "keyspace_misses").Value ?? "unknown"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting Redis stats");
            return new Dictionary<string, string>();
        }
    }

    public async Task FlushAllAsync()
    {
        try
        {
            var server = _connectionMultiplexer.GetServer(_connectionMultiplexer.GetEndPoints().First());
            await server.FlushAllDatabasesAsync();
            _logger.LogWarning("Flushed all Redis databases");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error flushing Redis databases");
        }
    }

    public async Task<string[]> GetKeysAsync(string pattern = "*")
    {
        try
        {
            var server = _connectionMultiplexer.GetServer(_connectionMultiplexer.GetEndPoints().First());
            var keys = server.Keys(pattern: pattern);
            return keys.Select(k => k.ToString()).ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting keys with pattern: {Pattern}", pattern);
            return Array.Empty<string>();
        }
    }

    // Business-specific cache methods
    public async Task CacheUserAsync(int userId, object userData, TimeSpan? expiration = null)
    {
        await SetAsync($"user:{userId}", userData, expiration ?? TimeSpan.FromMinutes(30));
    }

    public async Task<T?> GetCachedUserAsync<T>(int userId) where T : class
    {
        return await GetAsync<T>($"user:{userId}");
    }

    public async Task InvalidateUserCacheAsync(int userId)
    {
        await RemoveByPatternAsync($"user:{userId}*");
        await RemoveByPatternAsync($"user_todos:{userId}*");
    }

    public async Task CacheTodoItemAsync(int todoItemId, object todoItem, TimeSpan? expiration = null)
    {
        await SetAsync($"todo:{todoItemId}", todoItem, expiration ?? TimeSpan.FromMinutes(15));
    }

    public async Task<T?> GetCachedTodoItemAsync<T>(int todoItemId) where T : class
    {
        return await GetAsync<T>($"todo:{todoItemId}");
    }

    public async Task InvalidateTodoItemCacheAsync(int todoItemId)
    {
        await RemoveAsync($"todo:{todoItemId}");
    }

    public async Task InvalidateUserTodoItemsCacheAsync(int userId)
    {
        await RemoveByPatternAsync($"user_todos:{userId}*");
    }

    public async Task CacheTagSuggestionsAsync(string text, string[] suggestions, TimeSpan? expiration = null)
    {
        var key = $"tag_suggestions:{Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(text)))}";
        await SetAsync(key, suggestions, expiration ?? TimeSpan.FromHours(24));
    }

    public async Task<string[]?> GetCachedTagSuggestionsAsync(string text)
    {
        var key = $"tag_suggestions:{Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(text)))}";
        return await GetAsync<string[]>(key);
    }

    public void Dispose()
    {
        _connectionMultiplexer?.Dispose();
    }
}