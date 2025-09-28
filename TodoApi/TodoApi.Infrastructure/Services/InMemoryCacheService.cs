using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace TodoApi.Infrastructure.Services;

public class InMemoryCacheService : ICacheService
{
    private readonly IMemoryCache _memoryCache;
    private readonly ILogger<InMemoryCacheService> _logger;
    private readonly IMetricsCollector _metrics;
    private readonly ConcurrentDictionary<string, DateTime> _keyTracker = new();

    public InMemoryCacheService(
        IMemoryCache memoryCache,
        ILogger<InMemoryCacheService> logger,
        IMetricsCollector metrics)
    {
        _memoryCache = memoryCache;
        _logger = logger;
        _metrics = metrics;
    }

    public async Task<T?> GetAsync<T>(string key) where T : class
    {
        if (_memoryCache.TryGetValue(key, out var value) && value is T typedValue)
        {
            _metrics.RecordCacheHit(key);
            _logger.LogTrace("Memory cache hit for key: {Key}", key);
            return typedValue;
        }

        _metrics.RecordCacheMiss(key);
        return null;
    }

    public async Task<string?> GetStringAsync(string key)
    {
        if (_memoryCache.TryGetValue(key, out var value) && value is string stringValue)
        {
            _metrics.RecordCacheHit(key);
            _logger.LogTrace("Memory cache hit for string key: {Key}", key);
            return stringValue;
        }

        _metrics.RecordCacheMiss(key);
        return null;
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null) where T : class
    {
        var options = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = expiration ?? TimeSpan.FromMinutes(30),
            Priority = CacheItemPriority.Normal
        };

        options.RegisterPostEvictionCallback((k, v, reason, state) =>
        {
            _keyTracker.TryRemove(k.ToString()!, out _);
            _logger.LogTrace("Memory cache entry evicted: {Key}, Reason: {Reason}", k, reason);
        });

        _memoryCache.Set(key, value, options);
        _keyTracker.TryAdd(key, DateTime.UtcNow);
        _logger.LogTrace("Cached item in memory with key: {Key} (expires: {Expiration})", key, expiration);
    }

    public async Task SetStringAsync(string key, string value, TimeSpan? expiration = null)
    {
        var options = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = expiration ?? TimeSpan.FromMinutes(30),
            Priority = CacheItemPriority.Normal
        };

        options.RegisterPostEvictionCallback((k, v, reason, state) =>
        {
            _keyTracker.TryRemove(k.ToString()!, out _);
        });

        _memoryCache.Set(key, value, options);
        _keyTracker.TryAdd(key, DateTime.UtcNow);
        _logger.LogTrace("Cached string in memory with key: {Key} (expires: {Expiration})", key, expiration);
    }

    public async Task RemoveAsync(string key)
    {
        _memoryCache.Remove(key);
        _keyTracker.TryRemove(key, out _);
        _logger.LogTrace("Removed memory cache item with key: {Key}", key);
    }

    public async Task RemoveByPatternAsync(string pattern)
    {
        var regex = new Regex(pattern.Replace("*", ".*"));
        var keysToRemove = _keyTracker.Keys.Where(key => regex.IsMatch(key)).ToList();

        foreach (var key in keysToRemove)
        {
            _memoryCache.Remove(key);
            _keyTracker.TryRemove(key, out _);
        }

        if (keysToRemove.Count > 0)
        {
            _logger.LogInformation("Removed {Count} memory cache items matching pattern: {Pattern}", keysToRemove.Count, pattern);
        }
    }

    public async Task<bool> ExistsAsync(string key)
    {
        return _memoryCache.TryGetValue(key, out _);
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
        // Memory cache doesn't support refreshing TTL, so this is a no-op
        _logger.LogTrace("Refresh requested for memory cache key (no-op): {Key}", key);
    }

    public async Task<long> IncrementAsync(string key, long value = 1)
    {
        var currentValue = 0L;
        if (_memoryCache.TryGetValue(key, out var existing) && existing is long longValue)
        {
            currentValue = longValue;
        }

        var newValue = currentValue + value;
        await SetAsync(key, newValue);
        return newValue;
    }

    public async Task<double> IncrementAsync(string key, double value)
    {
        var currentValue = 0.0;
        if (_memoryCache.TryGetValue(key, out var existing) && existing is double doubleValue)
        {
            currentValue = doubleValue;
        }

        var newValue = currentValue + value;
        await SetAsync(key, newValue);
        return newValue;
    }

    // Hash operations (simulated with Dictionary)
    public async Task SetHashFieldAsync(string key, string field, string value)
    {
        var hash = await GetAsync<Dictionary<string, string>>(key) ?? new Dictionary<string, string>();
        hash[field] = value;
        await SetAsync(key, hash);
        _logger.LogTrace("Set hash field {Field} in {Key}", field, key);
    }

    public async Task<string?> GetHashFieldAsync(string key, string field)
    {
        var hash = await GetAsync<Dictionary<string, string>>(key);
        return hash?.GetValueOrDefault(field);
    }

    public async Task<Dictionary<string, string>> GetHashAsync(string key)
    {
        return await GetAsync<Dictionary<string, string>>(key) ?? new Dictionary<string, string>();
    }

    public async Task RemoveHashFieldAsync(string key, string field)
    {
        var hash = await GetAsync<Dictionary<string, string>>(key);
        if (hash != null && hash.Remove(field))
        {
            await SetAsync(key, hash);
            _logger.LogTrace("Removed hash field {Field} from {Key}", field, key);
        }
    }

    // List operations (simulated with List<string>)
    public async Task<long> ListLeftPushAsync(string key, string value)
    {
        var list = await GetAsync<List<string>>(key) ?? new List<string>();
        list.Insert(0, value);
        await SetAsync(key, list);
        return list.Count;
    }

    public async Task<long> ListRightPushAsync(string key, string value)
    {
        var list = await GetAsync<List<string>>(key) ?? new List<string>();
        list.Add(value);
        await SetAsync(key, list);
        return list.Count;
    }

    public async Task<string?> ListLeftPopAsync(string key)
    {
        var list = await GetAsync<List<string>>(key);
        if (list == null || list.Count == 0)
            return null;

        var value = list[0];
        list.RemoveAt(0);
        await SetAsync(key, list);
        return value;
    }

    public async Task<string?> ListRightPopAsync(string key)
    {
        var list = await GetAsync<List<string>>(key);
        if (list == null || list.Count == 0)
            return null;

        var value = list[^1];
        list.RemoveAt(list.Count - 1);
        await SetAsync(key, list);
        return value;
    }

    public async Task<string[]> ListRangeAsync(string key, long start = 0, long stop = -1)
    {
        var list = await GetAsync<List<string>>(key);
        if (list == null || list.Count == 0)
            return Array.Empty<string>();

        var endIndex = stop == -1 ? list.Count - 1 : (int)Math.Min(stop, list.Count - 1);
        var startIndex = (int)Math.Max(start, 0);
        
        if (startIndex > endIndex)
            return Array.Empty<string>();

        return list.Skip(startIndex).Take(endIndex - startIndex + 1).ToArray();
    }

    public async Task<long> ListLengthAsync(string key)
    {
        var list = await GetAsync<List<string>>(key);
        return list?.Count ?? 0;
    }

    // Set operations (simulated with HashSet<string>)
    public async Task<bool> SetAddAsync(string key, string value)
    {
        var set = await GetAsync<HashSet<string>>(key) ?? new HashSet<string>();
        var added = set.Add(value);
        await SetAsync(key, set);
        return added;
    }

    public async Task<bool> SetRemoveAsync(string key, string value)
    {
        var set = await GetAsync<HashSet<string>>(key);
        if (set == null)
            return false;

        var removed = set.Remove(value);
        await SetAsync(key, set);
        return removed;
    }

    public async Task<bool> SetContainsAsync(string key, string value)
    {
        var set = await GetAsync<HashSet<string>>(key);
        return set?.Contains(value) ?? false;
    }

    public async Task<string[]> SetMembersAsync(string key)
    {
        var set = await GetAsync<HashSet<string>>(key);
        return set?.ToArray() ?? Array.Empty<string>();
    }

    public async Task<long> SetLengthAsync(string key)
    {
        var set = await GetAsync<HashSet<string>>(key);
        return set?.Count ?? 0;
    }

    // Cache management
    public async Task<Dictionary<string, string>> GetStatsAsync()
    {
        return new Dictionary<string, string>
        {
            ["type"] = "InMemoryCache",
            ["total_keys"] = _keyTracker.Count.ToString(),
            ["implementation"] = "Microsoft.Extensions.Caching.Memory"
        };
    }

    public async Task FlushAllAsync()
    {
        var keys = _keyTracker.Keys.ToList();
        foreach (var key in keys)
        {
            _memoryCache.Remove(key);
            _keyTracker.TryRemove(key, out _);
        }
        _logger.LogWarning("Flushed all memory cache entries ({Count} keys)", keys.Count);
    }

    public async Task<string[]> GetKeysAsync(string pattern = "*")
    {
        if (pattern == "*")
        {
            return _keyTracker.Keys.ToArray();
        }

        var regex = new Regex(pattern.Replace("*", ".*"));
        return _keyTracker.Keys.Where(key => regex.IsMatch(key)).ToArray();
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
}