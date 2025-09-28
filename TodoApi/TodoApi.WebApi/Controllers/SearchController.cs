using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TodoApi.Application.DTOs;
using TodoApi.Infrastructure.Services;

namespace TodoApi.WebApi.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion("1.0")]
[Authorize]
public class SearchController : ControllerBase
{
    private readonly ISearchService _searchService;
    private readonly ILogger<SearchController> _logger;

    public SearchController(ISearchService searchService, ILogger<SearchController> logger)
    {
        _searchService = searchService;
        _logger = logger;
    }

    /// <summary>
    /// Search todo items with basic query
    /// </summary>
    [HttpGet("todos")]
    [ProducesResponseType(typeof(SearchResult<TodoItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<SearchResult<TodoItemDto>>> SearchTodoItems(
        [FromQuery] string? q,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? sortBy = null,
        [FromQuery] string? sortDirection = "desc",
        [FromQuery] bool includeHighlights = true)
    {
        try
        {
            var userId = GetCurrentUserId();
            
            var query = new SearchQuery
            {
                Query = q ?? string.Empty,
                Page = page,
                PageSize = Math.Min(pageSize, 100), // Limit page size
                SortField = sortBy,
                SortDirection = sortDirection?.ToLower() == "asc" ? SortDirection.Ascending : SortDirection.Descending,
                IncludeHighlights = includeHighlights,
                UserId = int.TryParse(userId, out var userIdInt) ? userIdInt : null
            };

            var result = await _searchService.SearchTodoItemsAsync(query);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Todo search failed");
            return StatusCode(500, "Search failed");
        }
    }

    /// <summary>
    /// Advanced search for todo items with filters
    /// </summary>
    [HttpPost("todos/advanced")]
    [ProducesResponseType(typeof(SearchResult<TodoItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<SearchResult<TodoItemDto>>> AdvancedSearchTodoItems([FromBody] AdvancedSearchRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();
            
            var query = new AdvancedSearchQuery
            {
                Query = request.Query ?? string.Empty,
                Page = request.Page,
                PageSize = Math.Min(request.PageSize, 100),
                SortField = request.SortBy,
                SortDirection = request.SortDirection?.ToLower() == "asc" ? SortDirection.Ascending : SortDirection.Descending,
                IncludeHighlights = request.IncludeHighlights,
                UserId = int.TryParse(userId, out var userIdInt) ? userIdInt : null,
                Tags = request.Tags ?? Array.Empty<string>(),
                IsComplete = request.IsComplete,
                DueDateFrom = request.DueDateFrom,
                DueDateTo = request.DueDateTo,
                PriorityMin = request.PriorityMin,
                PriorityMax = request.PriorityMax,
                CreatedFrom = request.CreatedFrom,
                CreatedTo = request.CreatedTo,
                UpdatedFrom = request.UpdatedFrom,
                UpdatedTo = request.UpdatedTo
            };

            var result = await _searchService.SearchTodoItemsAdvancedAsync(query);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Advanced todo search failed");
            return StatusCode(500, "Advanced search failed");
        }
    }

    /// <summary>
    /// Search users
    /// </summary>
    [HttpGet("users")]
    [ProducesResponseType(typeof(SearchResult<UserDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<SearchResult<UserDto>>> SearchUsers(
        [FromQuery] string? q,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        try
        {
            var query = new SearchQuery
            {
                Query = q ?? string.Empty,
                Page = page,
                PageSize = Math.Min(pageSize, 100)
            };

            var result = await _searchService.SearchUsersAsync(query);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "User search failed");
            return StatusCode(500, "Search failed");
        }
    }

    /// <summary>
    /// Search comments
    /// </summary>
    [HttpGet("comments")]
    [ProducesResponseType(typeof(SearchResult<CommentDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<SearchResult<CommentDto>>> SearchComments(
        [FromQuery] string? q,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        try
        {
            var userId = GetCurrentUserId();
            
            var query = new SearchQuery
            {
                Query = q ?? string.Empty,
                Page = page,
                PageSize = Math.Min(pageSize, 100),
                UserId = int.TryParse(userId, out var userIdInt) ? userIdInt : null
            };

            var result = await _searchService.SearchCommentsAsync(query);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Comment search failed");
            return StatusCode(500, "Search failed");
        }
    }

    /// <summary>
    /// Get search suggestions for autocomplete
    /// </summary>
    [HttpGet("suggestions")]
    [ProducesResponseType(typeof(string[]), StatusCodes.Status200OK)]
    public async Task<ActionResult<string[]>> GetSearchSuggestions(
        [FromQuery] string? q,
        [FromQuery] int maxSuggestions = 10)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(q))
            {
                return Ok(Array.Empty<string>());
            }

            var suggestions = await _searchService.GetSearchSuggestionsAsync(q, maxSuggestions);
            return Ok(suggestions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Getting search suggestions failed");
            return StatusCode(500, "Failed to get suggestions");
        }
    }

    /// <summary>
    /// Get autocomplete suggestions for specific fields
    /// </summary>
    [HttpGet("autocomplete/{field}")]
    [ProducesResponseType(typeof(string[]), StatusCodes.Status200OK)]
    public async Task<ActionResult<string[]>> GetAutoComplete(
        string field,
        [FromQuery] string? q,
        [FromQuery] int maxSuggestions = 10)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(q))
            {
                return Ok(Array.Empty<string>());
            }

            var suggestions = await _searchService.GetAutoCompleteAsync(q, field, maxSuggestions);
            return Ok(suggestions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Getting autocomplete suggestions failed for field {Field}", field);
            return StatusCode(500, "Failed to get autocomplete suggestions");
        }
    }

    /// <summary>
    /// Get search analytics (admin only)
    /// </summary>
    [HttpGet("analytics")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(SearchAnalytics), StatusCodes.Status200OK)]
    public async Task<ActionResult<SearchAnalytics>> GetSearchAnalytics(
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null)
    {
        try
        {
            var fromDate = from ?? DateTime.UtcNow.AddDays(-30);
            var toDate = to ?? DateTime.UtcNow;

            var analytics = await _searchService.GetSearchAnalyticsAsync(fromDate, toDate);
            return Ok(analytics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Getting search analytics failed");
            return StatusCode(500, "Failed to get search analytics");
        }
    }

    /// <summary>
    /// Global search across all content types
    /// </summary>
    [HttpGet("global")]
    [ProducesResponseType(typeof(GlobalSearchResult), StatusCodes.Status200OK)]
    public async Task<ActionResult<GlobalSearchResult>> GlobalSearch(
        [FromQuery] string? q,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(q))
            {
                return Ok(new GlobalSearchResult());
            }

            var userId = GetCurrentUserId();
            var query = new SearchQuery
            {
                Query = q,
                Page = page,
                PageSize = Math.Min(pageSize, 20), // Smaller page size for global search
                UserId = int.TryParse(userId, out var userIdInt) ? userIdInt : null
            };

            // Search across all content types
            var todoTask = _searchService.SearchTodoItemsAsync(query);
            var userTask = _searchService.SearchUsersAsync(new SearchQuery { Query = q, PageSize = 5 });
            var commentTask = _searchService.SearchCommentsAsync(query);

            await Task.WhenAll(todoTask, userTask, commentTask);

            var result = new GlobalSearchResult
            {
                Query = q,
                TodoItems = todoTask.Result,
                Users = userTask.Result,
                Comments = commentTask.Result,
                TotalResults = todoTask.Result.TotalHits + userTask.Result.TotalHits + commentTask.Result.TotalHits
            };

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Global search failed");
            return StatusCode(500, "Global search failed");
        }
    }

    private string GetCurrentUserId()
    {
        return User.FindFirst("userId")?.Value ?? 
               User.FindFirst("sub")?.Value ?? 
               User.Identity?.Name ?? 
               "1"; // Default fallback
    }
}

public class AdvancedSearchRequest
{
    public string? Query { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? SortBy { get; set; }
    public string? SortDirection { get; set; }
    public bool IncludeHighlights { get; set; } = true;
    
    // Filters
    public string[]? Tags { get; set; }
    public bool? IsComplete { get; set; }
    public DateTime? DueDateFrom { get; set; }
    public DateTime? DueDateTo { get; set; }
    public int? PriorityMin { get; set; }
    public int? PriorityMax { get; set; }
    public DateTime? CreatedFrom { get; set; }
    public DateTime? CreatedTo { get; set; }
    public DateTime? UpdatedFrom { get; set; }
    public DateTime? UpdatedTo { get; set; }
}

public class GlobalSearchResult
{
    public string Query { get; set; } = string.Empty;
    public SearchResult<TodoItemDto>? TodoItems { get; set; }
    public SearchResult<UserDto>? Users { get; set; }
    public SearchResult<CommentDto>? Comments { get; set; }
    public long TotalResults { get; set; }
}