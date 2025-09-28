using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using TodoApi.Application.DTOs;
using TodoApi.Infrastructure.Data;
using TodoApi.Infrastructure.Logging;

namespace TodoApi.Infrastructure.Services;

public class PostgreSqlFullTextSearchService : ISearchService
{
    private readonly ApplicationDbContext _context;
    private readonly IStructuredLogger<PostgreSqlFullTextSearchService> _logger;
    private readonly ICacheService _cache;
    private readonly IMetricsCollector _metrics;

    public PostgreSqlFullTextSearchService(
        ApplicationDbContext context,
        IStructuredLogger<PostgreSqlFullTextSearchService> logger,
        ICacheService cache,
        IMetricsCollector metrics)
    {
        _context = context;
        _logger = logger;
        _cache = cache;
        _metrics = metrics;
    }

    public async Task<SearchResult<TodoItemDto>> SearchTodoItemsAsync(SearchQuery query)
    {
        using var scope = _logger.BeginScope("SearchTodoItems", query);
        var stopwatch = Stopwatch.StartNew();

        try
        {
            // Check cache first
            var cacheKey = $"search_todos:{query.Query.GetHashCode()}:{query.Page}:{query.PageSize}:{query.UserId}";
            var cached = await _cache.GetAsync<SearchResult<TodoItemDto>>(cacheKey);
            if (cached != null)
            {
                _logger.LogTrace("Search results retrieved from cache");
                return cached;
            }

            var searchTerm = query.Query.Trim();
            var dbQuery = _context.TodoItems.AsQueryable();

            // Apply user filter
            if (query.UserId.HasValue)
            {
                dbQuery = dbQuery.Where(t => t.UserId == query.UserId.Value);
            }

            // Apply full-text search using PostgreSQL
            if (!string.IsNullOrEmpty(searchTerm))
            {
                // Use PostgreSQL's full-text search capabilities
                dbQuery = dbQuery.Where(t => 
                    EF.Functions.ToTsVector("english", t.Title + " " + t.Description)
                        .Matches(EF.Functions.PlainToTsQuery("english", searchTerm)));
            }

            // Apply additional filters
            foreach (var filter in query.Filters)
            {
                switch (filter.Key.ToLower())
                {
                    case "iscomplete":
                        if (filter.Value is bool isComplete)
                            dbQuery = dbQuery.Where(t => t.IsComplete == isComplete);
                        break;
                    case "priority":
                        if (filter.Value is int priority)
                            dbQuery = dbQuery.Where(t => t.Priority == priority);
                        break;
                    case "hastags":
                        if (filter.Value is bool hasTags && hasTags)
                            dbQuery = dbQuery.Where(t => t.Tags != null && t.Tags.Any());
                        break;
                }
            }

            // Apply sorting
            dbQuery = query.SortField?.ToLower() switch
            {
                "title" => query.SortDirection == SortDirection.Ascending 
                    ? dbQuery.OrderBy(t => t.Title) 
                    : dbQuery.OrderByDescending(t => t.Title),
                "duedate" => query.SortDirection == SortDirection.Ascending 
                    ? dbQuery.OrderBy(t => t.DueDate) 
                    : dbQuery.OrderByDescending(t => t.DueDate),
                "priority" => query.SortDirection == SortDirection.Ascending 
                    ? dbQuery.OrderBy(t => t.Priority) 
                    : dbQuery.OrderByDescending(t => t.Priority),
                "created" => query.SortDirection == SortDirection.Ascending 
                    ? dbQuery.OrderBy(t => t.CreatedAt) 
                    : dbQuery.OrderByDescending(t => t.CreatedAt),
                _ => dbQuery.OrderByDescending(t => t.UpdatedAt) // Default sort by relevance/update time
            };

            // Get total count
            var totalCount = await dbQuery.CountAsync();

            // Apply pagination
            var items = await dbQuery
                .Include(t => t.Tags)
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToListAsync();

            // Convert to DTOs with highlights
            var hits = items.Select(item => new SearchHit<TodoItemDto>
            {
                Document = new TodoItemDto
                {
                    Id = item.Id,
                    Title = item.Title,
                    Description = item.Description,
                    IsComplete = item.IsComplete,
                    DueDate = item.DueDate,
                    Priority = item.Priority,
                    CreatedAt = item.CreatedAt,
                    UpdatedAt = item.UpdatedAt,
                    UserId = item.UserId,
                    Tags = item.Tags?.Select(t => new TagDto { Id = t.Id, Name = t.Name }).ToList() ?? new List<TagDto>()
                },
                Score = CalculateSearchScore(item, searchTerm),
                Highlights = GenerateHighlights(item, searchTerm, query.IncludeHighlights)
            }).ToList();

            var result = new SearchResult<TodoItemDto>
            {
                Hits = hits,
                TotalHits = totalCount,
                Page = query.Page,
                PageSize = query.PageSize,
                QueryTime = stopwatch.Elapsed
            };

            // Cache results for 5 minutes
            await _cache.SetAsync(cacheKey, result, TimeSpan.FromMinutes(5));

            // Record metrics
            _metrics.RecordHistogram("search_query_duration", stopwatch.Elapsed.TotalMilliseconds, 
                ("query_type", "todo_items"), ("has_results", (totalCount > 0).ToString()));

            _logger.LogInformation("Search completed: {Query} returned {TotalHits} results in {Duration}ms", 
                searchTerm, totalCount, stopwatch.ElapsedMilliseconds);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError("Search failed for query '{Query}': {Error}", query.Query, ex, ex.Message);
            _metrics.IncrementErrorCounter("search_error", "SearchTodoItems");
            throw;
        }
    }

    public async Task<SearchResult<TodoItemDto>> SearchTodoItemsAdvancedAsync(AdvancedSearchQuery query)
    {
        using var scope = _logger.BeginScope("SearchTodoItemsAdvanced", query);
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var dbQuery = _context.TodoItems.AsQueryable();

            // Apply user filter
            if (query.UserId.HasValue)
            {
                dbQuery = dbQuery.Where(t => t.UserId == query.UserId.Value);
            }

            // Apply text search
            if (!string.IsNullOrEmpty(query.Query))
            {
                dbQuery = dbQuery.Where(t => 
                    EF.Functions.ToTsVector("english", t.Title + " " + t.Description)
                        .Matches(EF.Functions.PlainToTsQuery("english", query.Query)));
            }

            // Apply advanced filters
            if (query.IsComplete.HasValue)
                dbQuery = dbQuery.Where(t => t.IsComplete == query.IsComplete.Value);

            if (query.DueDateFrom.HasValue)
                dbQuery = dbQuery.Where(t => t.DueDate >= query.DueDateFrom.Value);

            if (query.DueDateTo.HasValue)
                dbQuery = dbQuery.Where(t => t.DueDate <= query.DueDateTo.Value);

            if (query.PriorityMin.HasValue)
                dbQuery = dbQuery.Where(t => t.Priority >= query.PriorityMin.Value);

            if (query.PriorityMax.HasValue)
                dbQuery = dbQuery.Where(t => t.Priority <= query.PriorityMax.Value);

            if (query.CreatedFrom.HasValue)
                dbQuery = dbQuery.Where(t => t.CreatedAt >= query.CreatedFrom.Value);

            if (query.CreatedTo.HasValue)
                dbQuery = dbQuery.Where(t => t.CreatedAt <= query.CreatedTo.Value);

            if (query.UpdatedFrom.HasValue)
                dbQuery = dbQuery.Where(t => t.UpdatedAt >= query.UpdatedFrom.Value);

            if (query.UpdatedTo.HasValue)
                dbQuery = dbQuery.Where(t => t.UpdatedAt <= query.UpdatedTo.Value);

            // Apply tag filters
            if (query.Tags?.Length > 0)
            {
                dbQuery = dbQuery.Where(t => t.Tags != null && 
                    t.Tags.Any(tag => query.Tags.Contains(tag.Name)));
            }

            // Apply sorting
            dbQuery = ApplySorting(dbQuery, query);

            // Get total count
            var totalCount = await dbQuery.CountAsync();

            // Apply pagination
            var items = await dbQuery
                .Include(t => t.Tags)
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToListAsync();

            // Convert to DTOs
            var hits = items.Select(item => new SearchHit<TodoItemDto>
            {
                Document = new TodoItemDto
                {
                    Id = item.Id,
                    Title = item.Title,
                    Description = item.Description,
                    IsComplete = item.IsComplete,
                    DueDate = item.DueDate,
                    Priority = item.Priority,
                    CreatedAt = item.CreatedAt,
                    UpdatedAt = item.UpdatedAt,
                    UserId = item.UserId,
                    Tags = item.Tags?.Select(t => new TagDto { Id = t.Id, Name = t.Name }).ToList() ?? new List<TagDto>()
                },
                Score = CalculateSearchScore(item, query.Query),
                Highlights = GenerateHighlights(item, query.Query, query.IncludeHighlights)
            }).ToList();

            var result = new SearchResult<TodoItemDto>
            {
                Hits = hits,
                TotalHits = totalCount,
                Page = query.Page,
                PageSize = query.PageSize,
                QueryTime = stopwatch.Elapsed,
                Facets = await GenerateFacets(query)
            };

            _logger.LogInformation("Advanced search completed: returned {TotalHits} results in {Duration}ms", 
                totalCount, stopwatch.ElapsedMilliseconds);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError("Advanced search failed: {Error}", ex, ex.Message);
            throw;
        }
    }

    public async Task<SearchResult<UserDto>> SearchUsersAsync(SearchQuery query)
    {
        // Implementation for user search
        var stopwatch = Stopwatch.StartNew();
        
        var dbQuery = _context.Users.AsQueryable();
        
        if (!string.IsNullOrEmpty(query.Query))
        {
            dbQuery = dbQuery.Where(u => 
                EF.Functions.ToTsVector("english", u.Name + " " + u.Email)
                    .Matches(EF.Functions.PlainToTsQuery("english", query.Query)));
        }

        var totalCount = await dbQuery.CountAsync();
        var users = await dbQuery
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        var hits = users.Select(user => new SearchHit<UserDto>
        {
            Document = new UserDto
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
                Role = user.Role
            },
            Score = 1.0f
        }).ToList();

        return new SearchResult<UserDto>
        {
            Hits = hits,
            TotalHits = totalCount,
            Page = query.Page,
            PageSize = query.PageSize,
            QueryTime = stopwatch.Elapsed
        };
    }

    public async Task<SearchResult<CommentDto>> SearchCommentsAsync(SearchQuery query)
    {
        // Implementation for comment search
        var stopwatch = Stopwatch.StartNew();
        
        var dbQuery = _context.Comments.AsQueryable();
        
        if (query.UserId.HasValue)
        {
            dbQuery = dbQuery.Where(c => c.TodoItem != null && c.TodoItem.UserId == query.UserId.Value);
        }
        
        if (!string.IsNullOrEmpty(query.Query))
        {
            dbQuery = dbQuery.Where(c => 
                EF.Functions.ToTsVector("english", c.Content)
                    .Matches(EF.Functions.PlainToTsQuery("english", query.Query)));
        }

        var totalCount = await dbQuery.CountAsync();
        var comments = await dbQuery
            .Include(c => c.TodoItem)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        var hits = comments.Select(comment => new SearchHit<CommentDto>
        {
            Document = new CommentDto
            {
                Id = comment.Id,
                Content = comment.Content,
                TodoItemId = comment.TodoItemId,
                CreatedAt = comment.CreatedAt
            },
            Score = 1.0f
        }).ToList();

        return new SearchResult<CommentDto>
        {
            Hits = hits,
            TotalHits = totalCount,
            Page = query.Page,
            PageSize = query.PageSize,
            QueryTime = stopwatch.Elapsed
        };
    }

    // Placeholder implementations for index management
    public async Task IndexTodoItemAsync(TodoItemDto todoItem) => await Task.CompletedTask;
    public async Task IndexUserAsync(UserDto user) => await Task.CompletedTask;
    public async Task IndexCommentAsync(CommentDto comment) => await Task.CompletedTask;
    public async Task RemoveFromIndexAsync(string indexName, string documentId) => await Task.CompletedTask;
    public async Task RebuildIndexAsync(string indexName) => await Task.CompletedTask;

    public async Task<string[]> GetSearchSuggestionsAsync(string query, int maxSuggestions = 10)
    {
        if (string.IsNullOrEmpty(query) || query.Length < 2)
            return Array.Empty<string>();

        // Get common words from titles and descriptions
        var suggestions = await _context.TodoItems
            .Where(t => t.Title.Contains(query) || t.Description.Contains(query))
            .Select(t => t.Title)
            .Take(maxSuggestions)
            .ToArrayAsync();

        return suggestions;
    }

    public async Task<string[]> GetAutoCompleteAsync(string query, string field, int maxSuggestions = 10)
    {
        if (string.IsNullOrEmpty(query))
            return Array.Empty<string>();

        return field.ToLower() switch
        {
            "title" => await _context.TodoItems
                .Where(t => t.Title.StartsWith(query))
                .Select(t => t.Title)
                .Distinct()
                .Take(maxSuggestions)
                .ToArrayAsync(),
            "tags" => await _context.Tags
                .Where(t => t.Name.StartsWith(query))
                .Select(t => t.Name)
                .Take(maxSuggestions)
                .ToArrayAsync(),
            _ => Array.Empty<string>()
        };
    }

    public async Task<SearchAnalytics> GetSearchAnalyticsAsync(DateTime from, DateTime to)
    {
        // This would typically be stored in a separate analytics table
        // For now, return placeholder data
        return new SearchAnalytics
        {
            TotalSearches = 1000,
            TopQueries = new Dictionary<string, long>
            {
                ["task"] = 150,
                ["urgent"] = 100,
                ["meeting"] = 80
            },
            AverageQueryTime = 45.2,
            ZeroResultQueries = 25
        };
    }

    private float CalculateSearchScore(object item, string searchTerm)
    {
        // Simple scoring based on term frequency and field importance
        if (string.IsNullOrEmpty(searchTerm))
            return 1.0f;

        float score = 0.0f;
        
        if (item is Domain.Entities.TodoItem todoItem)
        {
            if (todoItem.Title.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                score += 2.0f; // Title matches are more important
                
            if (todoItem.Description.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                score += 1.0f;
                
            // Boost recent items
            var daysSinceUpdate = (DateTime.UtcNow - todoItem.UpdatedAt).Days;
            score += Math.Max(0, 1.0f - (daysSinceUpdate / 30.0f));
        }

        return Math.Max(0.1f, score);
    }

    private Dictionary<string, string[]> GenerateHighlights(object item, string searchTerm, bool includeHighlights)
    {
        if (!includeHighlights || string.IsNullOrEmpty(searchTerm))
            return new Dictionary<string, string[]>();

        var highlights = new Dictionary<string, string[]>();

        if (item is Domain.Entities.TodoItem todoItem)
        {
            if (todoItem.Title.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
            {
                highlights["title"] = new[] { HighlightText(todoItem.Title, searchTerm) };
            }
            
            if (todoItem.Description.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
            {
                highlights["description"] = new[] { HighlightText(todoItem.Description, searchTerm) };
            }
        }

        return highlights;
    }

    private string HighlightText(string text, string searchTerm)
    {
        if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(searchTerm))
            return text;

        var highlightedText = text.Replace(searchTerm, $"<mark>{searchTerm}</mark>", StringComparison.OrdinalIgnoreCase);
        
        // Truncate to show context around the match
        var index = highlightedText.IndexOf("<mark>", StringComparison.OrdinalIgnoreCase);
        if (index > 50)
        {
            var start = Math.Max(0, index - 50);
            var end = Math.Min(highlightedText.Length, index + searchTerm.Length + 100);
            highlightedText = "..." + highlightedText.Substring(start, end - start);
            if (end < text.Length) highlightedText += "...";
        }

        return highlightedText;
    }

    private IQueryable<Domain.Entities.TodoItem> ApplySorting(IQueryable<Domain.Entities.TodoItem> query, SearchQuery searchQuery)
    {
        return searchQuery.SortField?.ToLower() switch
        {
            "title" => searchQuery.SortDirection == SortDirection.Ascending 
                ? query.OrderBy(t => t.Title) 
                : query.OrderByDescending(t => t.Title),
            "duedate" => searchQuery.SortDirection == SortDirection.Ascending 
                ? query.OrderBy(t => t.DueDate) 
                : query.OrderByDescending(t => t.DueDate),
            "priority" => searchQuery.SortDirection == SortDirection.Ascending 
                ? query.OrderBy(t => t.Priority) 
                : query.OrderByDescending(t => t.Priority),
            "created" => searchQuery.SortDirection == SortDirection.Ascending 
                ? query.OrderBy(t => t.CreatedAt) 
                : query.OrderByDescending(t => t.CreatedAt),
            _ => query.OrderByDescending(t => t.UpdatedAt)
        };
    }

    private async Task<Dictionary<string, long>> GenerateFacets(AdvancedSearchQuery query)
    {
        // Generate facets for common filters
        var facets = new Dictionary<string, long>();

        // Status facets
        var completedCount = await _context.TodoItems
            .Where(t => query.UserId == null || t.UserId == query.UserId.Value)
            .CountAsync(t => t.IsComplete);
        var pendingCount = await _context.TodoItems
            .Where(t => query.UserId == null || t.UserId == query.UserId.Value)
            .CountAsync(t => !t.IsComplete);

        facets["completed"] = completedCount;
        facets["pending"] = pendingCount;

        // Priority facets
        for (int i = 1; i <= 5; i++)
        {
            var count = await _context.TodoItems
                .Where(t => query.UserId == null || t.UserId == query.UserId.Value)
                .CountAsync(t => t.Priority == i);
            facets[$"priority_{i}"] = count;
        }

        return facets;
    }
}