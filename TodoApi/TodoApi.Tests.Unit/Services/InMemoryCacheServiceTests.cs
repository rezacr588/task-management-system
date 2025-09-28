using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using TodoApi.Infrastructure.Services;
using Xunit;
using FluentAssertions;

namespace TodoApi.Tests.Unit.Services;

public class InMemoryCacheServiceTests : IDisposable
{
    private readonly IMemoryCache _memoryCache;
    private readonly Mock<ILogger<InMemoryCacheService>> _loggerMock;
    private readonly Mock<IMetricsCollector> _metricsMock;
    private readonly InMemoryCacheService _cacheService;

    public InMemoryCacheServiceTests()
    {
        _memoryCache = new MemoryCache(new MemoryCacheOptions());
        _loggerMock = new Mock<ILogger<InMemoryCacheService>>();
        _metricsMock = new Mock<IMetricsCollector>();
        _cacheService = new InMemoryCacheService(_memoryCache, _loggerMock.Object, _metricsMock.Object);
    }

    [Fact]
    public async Task GetAsync_WhenItemExists_ShouldReturnItem()
    {
        // Arrange
        var key = "test-key";
        var value = new TestObject { Name = "Test", Value = 42 };
        await _cacheService.SetAsync(key, value);

        // Act
        var result = await _cacheService.GetAsync<TestObject>(key);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("Test");
        result.Value.Should().Be(42);
        _metricsMock.Verify(m => m.RecordCacheHit(key), Times.Once);
    }

    [Fact]
    public async Task GetAsync_WhenItemDoesNotExist_ShouldReturnNull()
    {
        // Arrange
        var key = "non-existent-key";

        // Act
        var result = await _cacheService.GetAsync<TestObject>(key);

        // Assert
        result.Should().BeNull();
        _metricsMock.Verify(m => m.RecordCacheMiss(key), Times.Once);
    }

    [Fact]
    public async Task SetAsync_ShouldStoreItem()
    {
        // Arrange
        var key = "test-key";
        var value = new TestObject { Name = "Test", Value = 42 };

        // Act
        await _cacheService.SetAsync(key, value, TimeSpan.FromMinutes(5));

        // Assert
        var result = await _cacheService.GetAsync<TestObject>(key);
        result.Should().NotBeNull();
        result!.Name.Should().Be("Test");
    }

    [Fact]
    public async Task GetStringAsync_WhenStringExists_ShouldReturnString()
    {
        // Arrange
        var key = "string-key";
        var value = "test-string-value";
        await _cacheService.SetStringAsync(key, value);

        // Act
        var result = await _cacheService.GetStringAsync(key);

        // Assert
        result.Should().Be(value);
        _metricsMock.Verify(m => m.RecordCacheHit(key), Times.Once);
    }

    [Fact]
    public async Task RemoveAsync_ShouldRemoveItem()
    {
        // Arrange
        var key = "test-key";
        var value = new TestObject { Name = "Test" };
        await _cacheService.SetAsync(key, value);

        // Act
        await _cacheService.RemoveAsync(key);

        // Assert
        var result = await _cacheService.GetAsync<TestObject>(key);
        result.Should().BeNull();
    }

    [Fact]
    public async Task RemoveByPatternAsync_ShouldRemoveMatchingKeys()
    {
        // Arrange
        await _cacheService.SetStringAsync("user:1:profile", "data1");
        await _cacheService.SetStringAsync("user:2:profile", "data2");
        await _cacheService.SetStringAsync("todo:1", "todo-data");

        // Act
        await _cacheService.RemoveByPatternAsync("user:*");

        // Assert
        var user1Result = await _cacheService.GetStringAsync("user:1:profile");
        var user2Result = await _cacheService.GetStringAsync("user:2:profile");
        var todoResult = await _cacheService.GetStringAsync("todo:1");

        user1Result.Should().BeNull();
        user2Result.Should().BeNull();
        todoResult.Should().NotBeNull(); // Should not be affected by pattern
    }

    [Fact]
    public async Task ExistsAsync_WhenItemExists_ShouldReturnTrue()
    {
        // Arrange
        var key = "test-key";
        await _cacheService.SetStringAsync(key, "test-value");

        // Act
        var exists = await _cacheService.ExistsAsync(key);

        // Assert
        exists.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsAsync_WhenItemDoesNotExist_ShouldReturnFalse()
    {
        // Arrange
        var key = "non-existent-key";

        // Act
        var exists = await _cacheService.ExistsAsync(key);

        // Assert
        exists.Should().BeFalse();
    }

    [Fact]
    public async Task GetOrSetAsync_WhenItemExists_ShouldReturnExisting()
    {
        // Arrange
        var key = "test-key";
        var existingValue = new TestObject { Name = "Existing", Value = 1 };
        await _cacheService.SetAsync(key, existingValue);

        // Act
        var result = await _cacheService.GetOrSetAsync(key, () => Task.FromResult(new TestObject { Name = "New", Value = 2 }));

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("Existing");
        result.Value.Should().Be(1);
    }

    [Fact]
    public async Task GetOrSetAsync_WhenItemDoesNotExist_ShouldCreateAndReturn()
    {
        // Arrange
        var key = "test-key";
        var newValue = new TestObject { Name = "New", Value = 2 };

        // Act
        var result = await _cacheService.GetOrSetAsync(key, () => Task.FromResult(newValue));

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("New");
        result.Value.Should().Be(2);

        // Verify it was cached
        var cachedResult = await _cacheService.GetAsync<TestObject>(key);
        cachedResult.Should().NotBeNull();
        cachedResult!.Name.Should().Be("New");
    }

    [Fact]
    public async Task IncrementAsync_Long_ShouldIncrementValue()
    {
        // Arrange
        var key = "counter";

        // Act
        var result1 = await _cacheService.IncrementAsync(key, 5);
        var result2 = await _cacheService.IncrementAsync(key, 3);

        // Assert
        result1.Should().Be(5);
        result2.Should().Be(8);
    }

    [Fact]
    public async Task IncrementAsync_Double_ShouldIncrementValue()
    {
        // Arrange
        var key = "score";

        // Act
        var result1 = await _cacheService.IncrementAsync(key, 1.5);
        var result2 = await _cacheService.IncrementAsync(key, 2.3);

        // Assert
        result1.Should().Be(1.5);
        result2.Should().Be(3.8);
    }

    [Fact]
    public async Task HashOperations_ShouldWorkCorrectly()
    {
        // Arrange
        var hashKey = "user:profile";
        var field1 = "name";
        var field2 = "email";
        var value1 = "John Doe";
        var value2 = "john@example.com";

        // Act
        await _cacheService.SetHashFieldAsync(hashKey, field1, value1);
        await _cacheService.SetHashFieldAsync(hashKey, field2, value2);

        // Assert
        var nameResult = await _cacheService.GetHashFieldAsync(hashKey, field1);
        var emailResult = await _cacheService.GetHashFieldAsync(hashKey, field2);
        var fullHash = await _cacheService.GetHashAsync(hashKey);

        nameResult.Should().Be(value1);
        emailResult.Should().Be(value2);
        fullHash.Should().HaveCount(2);
        fullHash[field1].Should().Be(value1);
        fullHash[field2].Should().Be(value2);
    }

    [Fact]
    public async Task ListOperations_ShouldWorkCorrectly()
    {
        // Arrange
        var listKey = "notifications";

        // Act
        await _cacheService.ListLeftPushAsync(listKey, "message1");
        await _cacheService.ListRightPushAsync(listKey, "message2");
        await _cacheService.ListLeftPushAsync(listKey, "message0");

        // Assert
        var length = await _cacheService.ListLengthAsync(listKey);
        var range = await _cacheService.ListRangeAsync(listKey);
        var leftPop = await _cacheService.ListLeftPopAsync(listKey);
        var rightPop = await _cacheService.ListRightPopAsync(listKey);

        length.Should().Be(3);
        range.Should().Equal("message0", "message1", "message2");
        leftPop.Should().Be("message0");
        rightPop.Should().Be("message2");
    }

    [Fact]
    public async Task SetOperations_ShouldWorkCorrectly()
    {
        // Arrange
        var setKey = "tags";

        // Act
        var added1 = await _cacheService.SetAddAsync(setKey, "work");
        var added2 = await _cacheService.SetAddAsync(setKey, "personal");
        var added3 = await _cacheService.SetAddAsync(setKey, "work"); // Duplicate

        // Assert
        var contains = await _cacheService.SetContainsAsync(setKey, "work");
        var members = await _cacheService.SetMembersAsync(setKey);
        var length = await _cacheService.SetLengthAsync(setKey);

        added1.Should().BeTrue();
        added2.Should().BeTrue();
        added3.Should().BeFalse(); // Duplicate not added
        contains.Should().BeTrue();
        members.Should().HaveCount(2);
        length.Should().Be(2);
    }

    [Fact]
    public async Task BusinessSpecificMethods_ShouldWorkCorrectly()
    {
        // Arrange
        var userId = 123;
        var todoItemId = 456;
        var userData = new { Name = "Test User", Email = "test@example.com" };
        var todoItem = new { Title = "Test Task", IsComplete = false };

        // Act
        await _cacheService.CacheUserAsync(userId, userData);
        await _cacheService.CacheTodoItemAsync(todoItemId, todoItem);

        // Assert
        var cachedUser = await _cacheService.GetCachedUserAsync<object>(userId);
        var cachedTodo = await _cacheService.GetCachedTodoItemAsync<object>(todoItemId);

        cachedUser.Should().NotBeNull();
        cachedTodo.Should().NotBeNull();

        // Test invalidation
        await _cacheService.InvalidateUserCacheAsync(userId);
        await _cacheService.InvalidateTodoItemCacheAsync(todoItemId);

        var invalidatedUser = await _cacheService.GetCachedUserAsync<object>(userId);
        var invalidatedTodo = await _cacheService.GetCachedTodoItemAsync<object>(todoItemId);

        invalidatedUser.Should().BeNull();
        invalidatedTodo.Should().BeNull();
    }

    [Fact]
    public async Task TagSuggestions_ShouldWorkCorrectly()
    {
        // Arrange
        var text = "This is a test description for tag suggestions";
        var suggestions = new[] { "test", "description", "suggestions" };

        // Act
        await _cacheService.CacheTagSuggestionsAsync(text, suggestions);
        var cached = await _cacheService.GetCachedTagSuggestionsAsync(text);

        // Assert
        cached.Should().NotBeNull();
        cached.Should().Equal(suggestions);
    }

    [Fact]
    public async Task GetStatsAsync_ShouldReturnStats()
    {
        // Arrange
        await _cacheService.SetStringAsync("key1", "value1");
        await _cacheService.SetStringAsync("key2", "value2");

        // Act
        var stats = await _cacheService.GetStatsAsync();

        // Assert
        stats.Should().NotBeNull();
        stats.Should().ContainKey("type");
        stats.Should().ContainKey("total_keys");
        stats["type"].Should().Be("InMemoryCache");
        stats["total_keys"].Should().Be("2");
    }

    [Fact]
    public async Task GetKeysAsync_ShouldReturnMatchingKeys()
    {
        // Arrange
        await _cacheService.SetStringAsync("user:1", "data1");
        await _cacheService.SetStringAsync("user:2", "data2");
        await _cacheService.SetStringAsync("todo:1", "data3");

        // Act
        var allKeys = await _cacheService.GetKeysAsync();
        var userKeys = await _cacheService.GetKeysAsync("user:*");

        // Assert
        allKeys.Should().HaveCount(3);
        userKeys.Should().HaveCount(2);
        userKeys.Should().Contain("user:1");
        userKeys.Should().Contain("user:2");
    }

    [Fact]
    public async Task FlushAllAsync_ShouldRemoveAllKeys()
    {
        // Arrange
        await _cacheService.SetStringAsync("key1", "value1");
        await _cacheService.SetStringAsync("key2", "value2");

        // Act
        await _cacheService.FlushAllAsync();

        // Assert
        var key1Result = await _cacheService.GetStringAsync("key1");
        var key2Result = await _cacheService.GetStringAsync("key2");
        
        key1Result.Should().BeNull();
        key2Result.Should().BeNull();
    }

    public void Dispose()
    {
        _memoryCache?.Dispose();
    }

    private class TestObject
    {
        public string Name { get; set; } = string.Empty;
        public int Value { get; set; }
    }
}