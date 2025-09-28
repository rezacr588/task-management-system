using Microsoft.EntityFrameworkCore;
using Moq;
using TodoApi.Domain.Entities;
using TodoApi.Infrastructure.Data;
using TodoApi.Infrastructure.Logging;
using TodoApi.Infrastructure.Services;
using Xunit;
using FluentAssertions;

namespace TodoApi.Tests.Unit.Services;

public class PostgreSqlFullTextSearchServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly Mock<IStructuredLogger<PostgreSqlFullTextSearchService>> _loggerMock;
    private readonly Mock<ICacheService> _cacheMock;
    private readonly Mock<IMetricsCollector> _metricsMock;
    private readonly PostgreSqlFullTextSearchService _searchService;

    public PostgreSqlFullTextSearchServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _loggerMock = new Mock<IStructuredLogger<PostgreSqlFullTextSearchService>>();
        _cacheMock = new Mock<ICacheService>();
        _metricsMock = new Mock<IMetricsCollector>();
        
        _searchService = new PostgreSqlFullTextSearchService(_context, _loggerMock.Object, _cacheMock.Object, _metricsMock.Object);
    }

    [Fact]
    public async Task SearchTodoItemsAsync_WithEmptyQuery_ShouldReturnAllUserItems()
    {
        // Arrange
        await SeedTestData();
        
        var query = new SearchQuery
        {
            Query = "",
            UserId = 1,
            Page = 1,
            PageSize = 10
        };

        // Act
        var result = await _searchService.SearchTodoItemsAsync(query);

        // Assert
        result.Should().NotBeNull();
        result.Hits.Should().HaveCount(2); // User 1 has 2 todo items
        result.TotalHits.Should().Be(2);
    }

    [Fact]
    public async Task SearchTodoItemsAsync_WithTextQuery_ShouldReturnMatchingItems()
    {
        // Arrange
        await SeedTestData();
        
        var query = new SearchQuery
        {
            Query = "important",
            UserId = 1,
            Page = 1,
            PageSize = 10
        };

        // Act
        var result = await _searchService.SearchTodoItemsAsync(query);

        // Assert
        result.Should().NotBeNull();
        result.Hits.Should().HaveCountGreaterThan(0);
        result.Hits.Should().AllSatisfy(hit => 
            hit.Document.Title.Contains("important", StringComparison.OrdinalIgnoreCase) ||
            hit.Document.Description.Contains("important", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task SearchTodoItemsAsync_WithPagination_ShouldReturnCorrectPage()
    {
        // Arrange
        await SeedTestData();
        
        var query = new SearchQuery
        {
            Query = "",
            UserId = 1,
            Page = 2,
            PageSize = 1
        };

        // Act
        var result = await _searchService.SearchTodoItemsAsync(query);

        // Assert
        result.Should().NotBeNull();
        result.Page.Should().Be(2);
        result.PageSize.Should().Be(1);
        result.Hits.Should().HaveCount(1); // Second page with 1 item per page
    }

    [Fact]
    public async Task SearchTodoItemsAsync_WithSorting_ShouldReturnSortedResults()
    {
        // Arrange
        await SeedTestData();
        
        var query = new SearchQuery
        {
            Query = "",
            UserId = 1,
            SortField = "title",
            SortDirection = SortDirection.Ascending,
            Page = 1,
            PageSize = 10
        };

        // Act
        var result = await _searchService.SearchTodoItemsAsync(query);

        // Assert
        result.Should().NotBeNull();
        result.Hits.Should().HaveCount(2);
        var titles = result.Hits.Select(h => h.Document.Title).ToList();
        titles.Should().BeInAscendingOrder();
    }

    [Fact]
    public async Task SearchTodoItemsAsync_WithFilters_ShouldApplyFilters()
    {
        // Arrange
        await SeedTestData();
        
        var query = new SearchQuery
        {
            Query = "",
            UserId = 1,
            Filters = new Dictionary<string, object>
            {
                ["iscomplete"] = true
            },
            Page = 1,
            PageSize = 10
        };

        // Act
        var result = await _searchService.SearchTodoItemsAsync(query);

        // Assert
        result.Should().NotBeNull();
        result.Hits.Should().AllSatisfy(hit => hit.Document.IsComplete.Should().BeTrue());
    }

    [Fact]
    public async Task SearchTodoItemsAdvancedAsync_WithDateFilters_ShouldApplyDateFilters()
    {
        // Arrange
        await SeedTestData();
        
        var query = new AdvancedSearchQuery
        {
            Query = "",
            UserId = 1,
            DueDateFrom = DateTime.UtcNow.AddDays(-1),
            DueDateTo = DateTime.UtcNow.AddDays(1),
            Page = 1,
            PageSize = 10
        };

        // Act
        var result = await _searchService.SearchTodoItemsAdvancedAsync(query);

        // Assert
        result.Should().NotBeNull();
        result.Hits.Should().AllSatisfy(hit => 
        {
            if (hit.Document.DueDate.HasValue)
            {
                hit.Document.DueDate.Value.Should().BeOnOrAfter(query.DueDateFrom!.Value);
                hit.Document.DueDate.Value.Should().BeOnOrBefore(query.DueDateTo!.Value);
            }
        });
    }

    [Fact]
    public async Task SearchTodoItemsAdvancedAsync_WithPriorityFilters_ShouldApplyPriorityFilters()
    {
        // Arrange
        await SeedTestData();
        
        var query = new AdvancedSearchQuery
        {
            Query = "",
            UserId = 1,
            PriorityMin = 2,
            PriorityMax = 4,
            Page = 1,
            PageSize = 10
        };

        // Act
        var result = await _searchService.SearchTodoItemsAdvancedAsync(query);

        // Assert
        result.Should().NotBeNull();
        result.Hits.Should().AllSatisfy(hit => 
        {
            hit.Document.Priority.Should().BeGreaterOrEqualTo(2);
            hit.Document.Priority.Should().BeLessOrEqualTo(4);
        });
    }

    [Fact]
    public async Task SearchUsersAsync_WithQuery_ShouldReturnMatchingUsers()
    {
        // Arrange
        await SeedTestData();
        
        var query = new SearchQuery
        {
            Query = "test",
            Page = 1,
            PageSize = 10
        };

        // Act
        var result = await _searchService.SearchUsersAsync(query);

        // Assert
        result.Should().NotBeNull();
        result.Hits.Should().HaveCountGreaterThan(0);
        result.Hits.Should().AllSatisfy(hit => 
            hit.Document.Name.Contains("test", StringComparison.OrdinalIgnoreCase) ||
            hit.Document.Email.Contains("test", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task SearchCommentsAsync_WithQuery_ShouldReturnMatchingComments()
    {
        // Arrange
        await SeedTestData();
        
        var query = new SearchQuery
        {
            Query = "great",
            UserId = 1,
            Page = 1,
            PageSize = 10
        };

        // Act
        var result = await _searchService.SearchCommentsAsync(query);

        // Assert
        result.Should().NotBeNull();
        result.Hits.Should().HaveCountGreaterThan(0);
        result.Hits.Should().AllSatisfy(hit => 
            hit.Document.Content.Contains("great", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task GetSearchSuggestionsAsync_WithValidQuery_ShouldReturnSuggestions()
    {
        // Arrange
        await SeedTestData();

        // Act
        var suggestions = await _searchService.GetSearchSuggestionsAsync("task", 5);

        // Assert
        suggestions.Should().NotBeNull();
        suggestions.Should().HaveCountLessOrEqualTo(5);
    }

    [Fact]
    public async Task GetSearchSuggestionsAsync_WithShortQuery_ShouldReturnEmpty()
    {
        // Arrange & Act
        var suggestions = await _searchService.GetSearchSuggestionsAsync("a", 5);

        // Assert
        suggestions.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAutoCompleteAsync_WithTitleField_ShouldReturnTitleSuggestions()
    {
        // Arrange
        await SeedTestData();

        // Act
        var suggestions = await _searchService.GetAutoCompleteAsync("Task", "title", 5);

        // Assert
        suggestions.Should().NotBeNull();
        suggestions.Should().AllSatisfy(s => s.Should().StartWith("Task"));
    }

    [Fact]
    public async Task GetAutoCompleteAsync_WithTagsField_ShouldReturnTagSuggestions()
    {
        // Arrange
        await SeedTestData();

        // Act
        var suggestions = await _searchService.GetAutoCompleteAsync("wor", "tags", 5);

        // Assert
        suggestions.Should().NotBeNull();
        suggestions.Should().AllSatisfy(s => s.Should().StartWith("wor"));
    }

    [Fact]
    public async Task GetSearchAnalyticsAsync_ShouldReturnAnalytics()
    {
        // Act
        var analytics = await _searchService.GetSearchAnalyticsAsync(DateTime.UtcNow.AddDays(-30), DateTime.UtcNow);

        // Assert
        analytics.Should().NotBeNull();
        analytics.TotalSearches.Should().BeGreaterThan(0);
        analytics.TopQueries.Should().NotBeEmpty();
    }

    [Fact]
    public async Task SearchTodoItemsAsync_ShouldUseCache()
    {
        // Arrange
        await SeedTestData();
        
        var query = new SearchQuery
        {
            Query = "test",
            UserId = 1
        };

        var cachedResult = new SearchResult<Application.DTOs.TodoItemDto>
        {
            TotalHits = 1,
            Hits = new List<SearchHit<Application.DTOs.TodoItemDto>>()
        };

        _cacheMock.Setup(x => x.GetAsync<SearchResult<Application.DTOs.TodoItemDto>>(It.IsAny<string>()))
            .ReturnsAsync(cachedResult);

        // Act
        var result = await _searchService.SearchTodoItemsAsync(query);

        // Assert
        result.Should().Be(cachedResult);
        _cacheMock.Verify(x => x.GetAsync<SearchResult<Application.DTOs.TodoItemDto>>(It.IsAny<string>()), Times.Once);
    }

    private async Task SeedTestData()
    {
        // Add test users
        var user1 = new User { Id = 1, Name = "Test User 1", Email = "test1@example.com", Role = "User" };
        var user2 = new User { Id = 2, Name = "Test User 2", Email = "test2@example.com", Role = "User" };
        
        _context.Users.AddRange(user1, user2);

        // Add test tags
        var workTag = new Tag { Id = 1, Name = "work" };
        var urgentTag = new Tag { Id = 2, Name = "urgent" };
        
        _context.Tags.AddRange(workTag, urgentTag);

        // Add test todo items
        var todo1 = new TodoItem
        {
            Id = 1,
            Title = "Task 1 - Important Meeting",
            Description = "Very important meeting with client",
            UserId = 1,
            Priority = 3,
            IsComplete = false,
            DueDate = DateTime.UtcNow.AddDays(1),
            CreatedAt = DateTime.UtcNow.AddDays(-2),
            UpdatedAt = DateTime.UtcNow.AddDays(-1),
            Tags = new List<Tag> { workTag, urgentTag }
        };

        var todo2 = new TodoItem
        {
            Id = 2,
            Title = "Task 2 - Code Review",
            Description = "Review pull request for new feature",
            UserId = 1,
            Priority = 2,
            IsComplete = true,
            DueDate = DateTime.UtcNow.AddDays(-1),
            CreatedAt = DateTime.UtcNow.AddDays(-3),
            UpdatedAt = DateTime.UtcNow.AddHours(-2),
            Tags = new List<Tag> { workTag }
        };

        var todo3 = new TodoItem
        {
            Id = 3,
            Title = "Task 3 - Personal Task",
            Description = "Buy groceries",
            UserId = 2,
            Priority = 1,
            IsComplete = false,
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            UpdatedAt = DateTime.UtcNow.AddHours(-1)
        };

        _context.TodoItems.AddRange(todo1, todo2, todo3);

        // Add test comments
        var comment1 = new Comment
        {
            Id = 1,
            Content = "This is a great task!",
            TodoItemId = 1,
            CreatedAt = DateTime.UtcNow.AddHours(-1)
        };

        var comment2 = new Comment
        {
            Id = 2,
            Content = "Need to follow up on this",
            TodoItemId = 2,
            CreatedAt = DateTime.UtcNow.AddMinutes(-30)
        };

        _context.Comments.AddRange(comment1, comment2);

        await _context.SaveChangesAsync();
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}