using TodoApi.Application.DTOs;

namespace TodoApi.Infrastructure.Services;

public interface ISearchService
{
    // Full-text search
    Task<SearchResult<TodoItemDto>> SearchTodoItemsAsync(SearchQuery query);
    Task<SearchResult<UserDto>> SearchUsersAsync(SearchQuery query);
    Task<SearchResult<CommentDto>> SearchCommentsAsync(SearchQuery query);
    
    // Advanced search with filters
    Task<SearchResult<TodoItemDto>> SearchTodoItemsAdvancedAsync(AdvancedSearchQuery query);
    
    // Index management
    Task IndexTodoItemAsync(TodoItemDto todoItem);
    Task IndexUserAsync(UserDto user);
    Task IndexCommentAsync(CommentDto comment);
    Task RemoveFromIndexAsync(string indexName, string documentId);
    Task RebuildIndexAsync(string indexName);
    
    // Search suggestions and autocomplete
    Task<string[]> GetSearchSuggestionsAsync(string query, int maxSuggestions = 10);
    Task<string[]> GetAutoCompleteAsync(string query, string field, int maxSuggestions = 10);
    
    // Analytics
    Task<SearchAnalytics> GetSearchAnalyticsAsync(DateTime from, DateTime to);
}

public class SearchQuery
{
    public string Query { get; set; } = string.Empty;
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string[] Fields { get; set; } = Array.Empty<string>();
    public Dictionary<string, object> Filters { get; set; } = new();
    public string? SortField { get; set; }
    public SortDirection SortDirection { get; set; } = SortDirection.Ascending;
    public bool IncludeHighlights { get; set; } = true;
    public int? UserId { get; set; }
}

public class AdvancedSearchQuery : SearchQuery
{
    public string[] Tags { get; set; } = Array.Empty<string>();
    public bool? IsComplete { get; set; }
    public DateTime? DueDateFrom { get; set; }
    public DateTime? DueDateTo { get; set; }
    public int? PriorityMin { get; set; }
    public int? PriorityMax { get; set; }
    public DateTime? CreatedFrom { get; set; }
    public DateTime? CreatedTo { get; set; }
    public DateTime? UpdatedFrom { get; set; }
    public DateTime? UpdatedTo { get; set; }
    public string? AssignedToUser { get; set; }
}

public class SearchResult<T>
{
    public IEnumerable<SearchHit<T>> Hits { get; set; } = Enumerable.Empty<SearchHit<T>>();
    public long TotalHits { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public TimeSpan QueryTime { get; set; }
    public Dictionary<string, long> Facets { get; set; } = new();
    public string[] Suggestions { get; set; } = Array.Empty<string>();
}

public class SearchHit<T>
{
    public T Document { get; set; } = default!;
    public float Score { get; set; }
    public Dictionary<string, string[]> Highlights { get; set; } = new();
    public string? Explanation { get; set; }
}

public class SearchAnalytics
{
    public long TotalSearches { get; set; }
    public Dictionary<string, long> TopQueries { get; set; } = new();
    public Dictionary<string, long> TopResults { get; set; } = new();
    public double AverageQueryTime { get; set; }
    public long ZeroResultQueries { get; set; }
    public Dictionary<string, long> SearchesByDay { get; set; } = new();
}

public enum SortDirection
{
    Ascending,
    Descending
}