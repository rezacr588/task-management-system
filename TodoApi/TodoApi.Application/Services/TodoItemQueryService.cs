using AutoMapper;
using Microsoft.Extensions.Logging;
using TodoApi.Application.DTOs;
using TodoApi.Application.Interfaces;
using TodoApi.Infrastructure.Logging;
using TodoApi.Infrastructure.Services;

namespace TodoApi.Application.Services;

public class TodoItemQueryService : ITodoItemQueryService
{
    private readonly ITodoItemRepository _repository;
    private readonly IMapper _mapper;
    private readonly IStructuredLogger<TodoItemQueryService> _logger;
    private readonly ICacheService _cache;

    public TodoItemQueryService(
        ITodoItemRepository repository,
        IMapper mapper,
        IStructuredLogger<TodoItemQueryService> logger,
        ICacheService cache)
    {
        _repository = repository;
        _mapper = mapper;
        _logger = logger;
        _cache = cache;
    }

    public async Task<TodoItemDto?> GetByIdAsync(int id)
    {
        using var scope = _logger.BeginScope("GetTodoItemById", new { Id = id });
        
        // Try cache first
        var cached = await _cache.GetCachedTodoItemAsync<TodoItemDto>(id);
        if (cached != null)
        {
            _logger.LogTrace("TodoItem {Id} retrieved from cache", id);
            return cached;
        }

        var todoItem = await _repository.GetByIdAsync(id);
        if (todoItem == null)
        {
            _logger.LogDebug("TodoItem {Id} not found", id);
            return null;
        }

        var dto = _mapper.Map<TodoItemDto>(todoItem);
        
        // Cache the result
        await _cache.CacheTodoItemAsync(id, dto);
        
        _logger.LogInformation("TodoItem {Id} retrieved from database", id);
        return dto;
    }

    public async Task<IEnumerable<TodoItemDto>> GetByUserIdAsync(int userId, int page = 1, int pageSize = 10)
    {
        using var scope = _logger.BeginScope("GetTodoItemsByUser", new { UserId = userId, Page = page, PageSize = pageSize });

        var cacheKey = $"user_todos:{userId}:page:{page}:size:{pageSize}";
        var cached = await _cache.GetAsync<IEnumerable<TodoItemDto>>(cacheKey);
        if (cached != null)
        {
            _logger.LogTrace("TodoItems for user {UserId} retrieved from cache", userId);
            return cached;
        }

        var todoItems = await _repository.GetByUserIdAsync(userId, page, pageSize);
        var dtos = _mapper.Map<IEnumerable<TodoItemDto>>(todoItems);
        
        // Cache for 5 minutes
        await _cache.SetAsync(cacheKey, dtos, TimeSpan.FromMinutes(5));
        
        _logger.LogInformation("Retrieved {Count} TodoItems for user {UserId}", dtos.Count(), userId);
        return dtos;
    }

    public async Task<IEnumerable<TodoItemDto>> GetCompletedByUserAsync(int userId)
    {
        using var scope = _logger.BeginScope("GetCompletedTodoItems", new { UserId = userId });

        var cacheKey = $"user_completed_todos:{userId}";
        var cached = await _cache.GetAsync<IEnumerable<TodoItemDto>>(cacheKey);
        if (cached != null)
        {
            return cached;
        }

        var todoItems = await _repository.GetByUserIdAsync(userId);
        var completedItems = todoItems.Where(t => t.IsComplete);
        var dtos = _mapper.Map<IEnumerable<TodoItemDto>>(completedItems);
        
        // Cache for 10 minutes
        await _cache.SetAsync(cacheKey, dtos, TimeSpan.FromMinutes(10));
        
        _logger.LogInformation("Retrieved {Count} completed TodoItems for user {UserId}", dtos.Count(), userId);
        return dtos;
    }

    public async Task<IEnumerable<TodoItemDto>> GetPendingByUserAsync(int userId)
    {
        using var scope = _logger.BeginScope("GetPendingTodoItems", new { UserId = userId });

        var cacheKey = $"user_pending_todos:{userId}";
        var cached = await _cache.GetAsync<IEnumerable<TodoItemDto>>(cacheKey);
        if (cached != null)
        {
            return cached;
        }

        var todoItems = await _repository.GetByUserIdAsync(userId);
        var pendingItems = todoItems.Where(t => !t.IsComplete);
        var dtos = _mapper.Map<IEnumerable<TodoItemDto>>(pendingItems);
        
        // Cache for 2 minutes (shorter since pending items change frequently)
        await _cache.SetAsync(cacheKey, dtos, TimeSpan.FromMinutes(2));
        
        _logger.LogInformation("Retrieved {Count} pending TodoItems for user {UserId}", dtos.Count(), userId);
        return dtos;
    }

    public async Task<IEnumerable<TodoItemDto>> SearchAsync(string searchTerm, int userId, int page = 1, int pageSize = 10)
    {
        using var scope = _logger.BeginScope("SearchTodoItems", new { SearchTerm = searchTerm, UserId = userId, Page = page, PageSize = pageSize });

        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            return await GetByUserIdAsync(userId, page, pageSize);
        }

        var cacheKey = $"search_todos:{userId}:{searchTerm.GetHashCode()}:page:{page}:size:{pageSize}";
        var cached = await _cache.GetAsync<IEnumerable<TodoItemDto>>(cacheKey);
        if (cached != null)
        {
            return cached;
        }

        var todoItems = await _repository.SearchAsync(searchTerm, userId, page, pageSize);
        var dtos = _mapper.Map<IEnumerable<TodoItemDto>>(todoItems);
        
        // Cache search results for 5 minutes
        await _cache.SetAsync(cacheKey, dtos, TimeSpan.FromMinutes(5));
        
        _logger.LogInformation("Search '{SearchTerm}' returned {Count} TodoItems for user {UserId}", searchTerm, dtos.Count(), userId);
        return dtos;
    }

    public async Task<IEnumerable<TodoItemDto>> GetByTagsAsync(int userId, string[] tags)
    {
        using var scope = _logger.BeginScope("GetTodoItemsByTags", new { UserId = userId, Tags = tags });

        if (tags == null || tags.Length == 0)
        {
            return Enumerable.Empty<TodoItemDto>();
        }

        var cacheKey = $"tagged_todos:{userId}:{string.Join(",", tags.OrderBy(t => t))}";
        var cached = await _cache.GetAsync<IEnumerable<TodoItemDto>>(cacheKey);
        if (cached != null)
        {
            return cached;
        }

        var todoItems = await _repository.GetByTagsAsync(userId, tags);
        var dtos = _mapper.Map<IEnumerable<TodoItemDto>>(todoItems);
        
        // Cache for 10 minutes
        await _cache.SetAsync(cacheKey, dtos, TimeSpan.FromMinutes(10));
        
        _logger.LogInformation("Retrieved {Count} TodoItems with tags [{Tags}] for user {UserId}", 
            dtos.Count(), string.Join(", ", tags), userId);
        return dtos;
    }

    public async Task<TodoItemStatisticsDto> GetStatisticsAsync(int userId)
    {
        using var scope = _logger.BeginScope("GetTodoItemStatistics", new { UserId = userId });

        var cacheKey = $"user_stats:{userId}";
        var cached = await _cache.GetAsync<TodoItemStatisticsDto>(cacheKey);
        if (cached != null)
        {
            return cached;
        }

        var todoItems = await _repository.GetByUserIdAsync(userId);
        var itemsList = todoItems.ToList();
        
        var stats = new TodoItemStatisticsDto
        {
            TotalItems = itemsList.Count,
            CompletedItems = itemsList.Count(t => t.IsComplete),
            PendingItems = itemsList.Count(t => !t.IsComplete),
            LastActivity = itemsList.Any() ? itemsList.Max(t => t.UpdatedAt) : null,
            MostUsedTags = itemsList
                .SelectMany(t => t.Tags?.Select(tag => tag.Name) ?? Enumerable.Empty<string>())
                .GroupBy(tag => tag)
                .OrderByDescending(g => g.Count())
                .Take(5)
                .Select(g => g.Key)
                .ToArray(),
            OverdueItems = itemsList.Count(t => !t.IsComplete && t.DueDate.HasValue && t.DueDate < DateTime.UtcNow),
            DueTodayItems = itemsList.Count(t => !t.IsComplete && t.DueDate.HasValue && t.DueDate.Value.Date == DateTime.UtcNow.Date),
            DueThisWeekItems = itemsList.Count(t => !t.IsComplete && t.DueDate.HasValue && 
                t.DueDate >= DateTime.UtcNow && t.DueDate <= DateTime.UtcNow.AddDays(7))
        };

        stats.CompletionRate = stats.TotalItems > 0 ? (double)stats.CompletedItems / stats.TotalItems * 100 : 0;

        // Cache statistics for 1 hour
        await _cache.SetAsync(cacheKey, stats, TimeSpan.FromHours(1));
        
        _logger.LogInformation("Generated statistics for user {UserId}: {TotalItems} total, {CompletedItems} completed, {CompletionRate:F1}% completion rate", 
            userId, stats.TotalItems, stats.CompletedItems, stats.CompletionRate);
        
        return stats;
    }

    public async Task<IEnumerable<TodoItemDto>> GetRecentlyModifiedAsync(int userId, int count = 10)
    {
        using var scope = _logger.BeginScope("GetRecentlyModifiedTodoItems", new { UserId = userId, Count = count });

        var cacheKey = $"recent_todos:{userId}:{count}";
        var cached = await _cache.GetAsync<IEnumerable<TodoItemDto>>(cacheKey);
        if (cached != null)
        {
            return cached;
        }

        var todoItems = await _repository.GetByUserIdAsync(userId);
        var recentItems = todoItems
            .OrderByDescending(t => t.UpdatedAt)
            .Take(count);
        
        var dtos = _mapper.Map<IEnumerable<TodoItemDto>>(recentItems);
        
        // Cache for 5 minutes
        await _cache.SetAsync(cacheKey, dtos, TimeSpan.FromMinutes(5));
        
        _logger.LogInformation("Retrieved {Count} recently modified TodoItems for user {UserId}", dtos.Count(), userId);
        return dtos;
    }

    public async Task<IEnumerable<TodoItemDto>> GetOverdueAsync(int userId)
    {
        using var scope = _logger.BeginScope("GetOverdueTodoItems", new { UserId = userId });

        var todoItems = await _repository.GetByUserIdAsync(userId);
        var overdueItems = todoItems.Where(t => 
            !t.IsComplete && 
            t.DueDate.HasValue && 
            t.DueDate < DateTime.UtcNow);
        
        var dtos = _mapper.Map<IEnumerable<TodoItemDto>>(overdueItems);
        
        _logger.LogInformation("Retrieved {Count} overdue TodoItems for user {UserId}", dtos.Count(), userId);
        return dtos;
    }

    public async Task<IEnumerable<TodoItemDto>> GetDueTodayAsync(int userId)
    {
        using var scope = _logger.BeginScope("GetDueTodayTodoItems", new { UserId = userId });

        var todoItems = await _repository.GetByUserIdAsync(userId);
        var dueTodayItems = todoItems.Where(t => 
            !t.IsComplete && 
            t.DueDate.HasValue && 
            t.DueDate.Value.Date == DateTime.UtcNow.Date);
        
        var dtos = _mapper.Map<IEnumerable<TodoItemDto>>(dueTodayItems);
        
        _logger.LogInformation("Retrieved {Count} TodoItems due today for user {UserId}", dtos.Count(), userId);
        return dtos;
    }

    public async Task<IEnumerable<TodoItemDto>> GetDueThisWeekAsync(int userId)
    {
        using var scope = _logger.BeginScope("GetDueThisWeekTodoItems", new { UserId = userId });

        var startOfWeek = DateTime.UtcNow.Date;
        var endOfWeek = startOfWeek.AddDays(7);

        var todoItems = await _repository.GetByUserIdAsync(userId);
        var dueThisWeekItems = todoItems.Where(t => 
            !t.IsComplete && 
            t.DueDate.HasValue && 
            t.DueDate >= startOfWeek && 
            t.DueDate <= endOfWeek);
        
        var dtos = _mapper.Map<IEnumerable<TodoItemDto>>(dueThisWeekItems);
        
        _logger.LogInformation("Retrieved {Count} TodoItems due this week for user {UserId}", dtos.Count(), userId);
        return dtos;
    }
}