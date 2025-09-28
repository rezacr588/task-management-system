using AutoMapper;
using TodoApi.Application.DTOs;
using TodoApi.Application.Interfaces;
using TodoApi.Domain.Entities;
using TodoApi.Infrastructure.Logging;
using TodoApi.Infrastructure.Services;
using TodoApi.Domain.Services;

namespace TodoApi.Application.Services;

public class TodoItemCommandService : ITodoItemCommandService
{
    private readonly ITodoItemRepository _repository;
    private readonly ITagRepository _tagRepository;
    private readonly IMapper _mapper;
    private readonly IStructuredLogger<TodoItemCommandService> _logger;
    private readonly ICacheService _cache;
    private readonly IActivityLogger _activityLogger;
    private readonly IMetricsCollector _metrics;

    public TodoItemCommandService(
        ITodoItemRepository repository,
        ITagRepository tagRepository,
        IMapper mapper,
        IStructuredLogger<TodoItemCommandService> logger,
        ICacheService cache,
        IActivityLogger activityLogger,
        IMetricsCollector metrics)
    {
        _repository = repository;
        _tagRepository = tagRepository;
        _mapper = mapper;
        _logger = logger;
        _cache = cache;
        _activityLogger = activityLogger;
        _metrics = metrics;
    }

    public async Task<TodoItemDto> CreateAsync(TodoItemCreateDto createDto)
    {
        using var scope = _logger.BeginScope("CreateTodoItem", createDto);

        var todoItem = new TodoItem
        {
            Title = createDto.Title,
            Description = createDto.Description,
            DueDate = createDto.DueDate,
            Priority = createDto.Priority,
            UserId = createDto.UserId,
            IsComplete = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Handle tags if provided
        if (createDto.Tags?.Length > 0)
        {
            var tags = await ProcessTagsAsync(createDto.Tags);
            todoItem.Tags = tags;
        }

        var createdItem = await _repository.CreateAsync(todoItem);
        var dto = _mapper.Map<TodoItemDto>(createdItem);

        // Log activity
        await _activityLogger.LogAsync(createDto.UserId, "Created todo item", new { TodoItemId = createdItem.Id, Title = createDto.Title });
        
        // Record metrics
        _metrics.RecordTodoItemCreated(createDto.UserId);
        
        // Invalidate relevant caches
        await InvalidateUserCaches(createDto.UserId);

        _logger.LogUserAction("CreateTodoItem", createDto.UserId, new { TodoItemId = createdItem.Id, Title = createDto.Title });
        
        return dto;
    }

    public async Task<TodoItemDto> UpdateAsync(int id, TodoItemUpdateDto updateDto)
    {
        using var scope = _logger.BeginScope("UpdateTodoItem", new { Id = id, UpdateDto = updateDto });

        var existingItem = await _repository.GetByIdAsync(id);
        if (existingItem == null)
        {
            throw new KeyNotFoundException($"TodoItem with ID {id} not found");
        }

        var originalIsComplete = existingItem.IsComplete;

        // Update fields that are provided
        if (updateDto.Title != null) existingItem.Title = updateDto.Title;
        if (updateDto.Description != null) existingItem.Description = updateDto.Description;
        if (updateDto.IsComplete.HasValue) existingItem.IsComplete = updateDto.IsComplete.Value;
        if (updateDto.DueDate.HasValue) existingItem.DueDate = updateDto.DueDate;
        if (updateDto.Priority.HasValue) existingItem.Priority = updateDto.Priority.Value;

        existingItem.UpdatedAt = DateTime.UtcNow;

        // Handle tags if provided
        if (updateDto.Tags != null)
        {
            var tags = await ProcessTagsAsync(updateDto.Tags);
            existingItem.Tags = tags;
        }

        var updatedItem = await _repository.UpdateAsync(existingItem);
        var dto = _mapper.Map<TodoItemDto>(updatedItem);

        // Log completion if status changed
        if (!originalIsComplete && existingItem.IsComplete)
        {
            _metrics.RecordTodoItemCompleted(existingItem.UserId);
            await _activityLogger.LogAsync(existingItem.UserId, "Completed todo item", new { TodoItemId = id, Title = existingItem.Title });
        }

        // Log activity
        await _activityLogger.LogAsync(existingItem.UserId, "Updated todo item", new { TodoItemId = id, Changes = updateDto });

        // Invalidate caches
        await InvalidateItemCaches(id, existingItem.UserId);

        _logger.LogUserAction("UpdateTodoItem", existingItem.UserId, new { TodoItemId = id, Changes = updateDto });
        
        return dto;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        using var scope = _logger.BeginScope("DeleteTodoItem", new { Id = id });

        var existingItem = await _repository.GetByIdAsync(id);
        if (existingItem == null)
        {
            return false;
        }

        var userId = existingItem.UserId;
        var title = existingItem.Title;

        var deleted = await _repository.DeleteAsync(id);
        
        if (deleted)
        {
            // Log activity
            await _activityLogger.LogAsync(userId, "Deleted todo item", new { TodoItemId = id, Title = title });
            
            // Invalidate caches
            await InvalidateItemCaches(id, userId);

            _logger.LogUserAction("DeleteTodoItem", userId, new { TodoItemId = id, Title = title });
        }

        return deleted;
    }

    public async Task<bool> MarkCompleteAsync(int id)
    {
        using var scope = _logger.BeginScope("MarkTodoItemComplete", new { Id = id });

        var existingItem = await _repository.GetByIdAsync(id);
        if (existingItem == null)
        {
            return false;
        }

        if (!existingItem.IsComplete)
        {
            existingItem.IsComplete = true;
            existingItem.UpdatedAt = DateTime.UtcNow;

            await _repository.UpdateAsync(existingItem);
            
            // Record metrics and log
            _metrics.RecordTodoItemCompleted(existingItem.UserId);
            await _activityLogger.LogAsync(existingItem.UserId, "Marked todo item complete", new { TodoItemId = id, Title = existingItem.Title });
            
            // Invalidate caches
            await InvalidateItemCaches(id, existingItem.UserId);

            _logger.LogUserAction("CompleteTodoItem", existingItem.UserId, new { TodoItemId = id, Title = existingItem.Title });
        }

        return true;
    }

    public async Task<bool> MarkIncompleteAsync(int id)
    {
        using var scope = _logger.BeginScope("MarkTodoItemIncomplete", new { Id = id });

        var existingItem = await _repository.GetByIdAsync(id);
        if (existingItem == null)
        {
            return false;
        }

        if (existingItem.IsComplete)
        {
            existingItem.IsComplete = false;
            existingItem.UpdatedAt = DateTime.UtcNow;

            await _repository.UpdateAsync(existingItem);
            
            // Log activity
            await _activityLogger.LogAsync(existingItem.UserId, "Marked todo item incomplete", new { TodoItemId = id, Title = existingItem.Title });
            
            // Invalidate caches
            await InvalidateItemCaches(id, existingItem.UserId);

            _logger.LogUserAction("UncompleteTodoItem", existingItem.UserId, new { TodoItemId = id, Title = existingItem.Title });
        }

        return true;
    }

    public async Task<bool> AssignTagsAsync(int id, string[] tagNames)
    {
        using var scope = _logger.BeginScope("AssignTagsToTodoItem", new { Id = id, TagNames = tagNames });

        var existingItem = await _repository.GetByIdAsync(id);
        if (existingItem == null)
        {
            return false;
        }

        var tags = await ProcessTagsAsync(tagNames);
        
        // Add new tags to existing ones
        if (existingItem.Tags == null)
        {
            existingItem.Tags = new List<Tag>();
        }

        foreach (var tag in tags)
        {
            if (!existingItem.Tags.Any(t => t.Name == tag.Name))
            {
                existingItem.Tags.Add(tag);
            }
        }

        existingItem.UpdatedAt = DateTime.UtcNow;
        await _repository.UpdateAsync(existingItem);

        // Log activity
        await _activityLogger.LogAsync(existingItem.UserId, "Assigned tags to todo item", 
            new { TodoItemId = id, Title = existingItem.Title, AddedTags = tagNames });

        // Invalidate caches
        await InvalidateItemCaches(id, existingItem.UserId);

        _logger.LogUserAction("AssignTags", existingItem.UserId, new { TodoItemId = id, Tags = tagNames });
        
        return true;
    }

    public async Task<bool> RemoveTagsAsync(int id, string[] tagNames)
    {
        using var scope = _logger.BeginScope("RemoveTagsFromTodoItem", new { Id = id, TagNames = tagNames });

        var existingItem = await _repository.GetByIdAsync(id);
        if (existingItem == null || existingItem.Tags == null)
        {
            return false;
        }

        var tagsToRemove = existingItem.Tags.Where(t => tagNames.Contains(t.Name)).ToList();
        
        foreach (var tag in tagsToRemove)
        {
            existingItem.Tags.Remove(tag);
        }

        existingItem.UpdatedAt = DateTime.UtcNow;
        await _repository.UpdateAsync(existingItem);

        // Log activity
        await _activityLogger.LogAsync(existingItem.UserId, "Removed tags from todo item", 
            new { TodoItemId = id, Title = existingItem.Title, RemovedTags = tagNames });

        // Invalidate caches
        await InvalidateItemCaches(id, existingItem.UserId);

        _logger.LogUserAction("RemoveTags", existingItem.UserId, new { TodoItemId = id, Tags = tagNames });
        
        return true;
    }

    public async Task<bool> SetPriorityAsync(int id, int priority)
    {
        using var scope = _logger.BeginScope("SetTodoItemPriority", new { Id = id, Priority = priority });

        var existingItem = await _repository.GetByIdAsync(id);
        if (existingItem == null)
        {
            return false;
        }

        var oldPriority = existingItem.Priority;
        existingItem.Priority = priority;
        existingItem.UpdatedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(existingItem);

        // Log activity
        await _activityLogger.LogAsync(existingItem.UserId, "Changed todo item priority", 
            new { TodoItemId = id, Title = existingItem.Title, OldPriority = oldPriority, NewPriority = priority });

        // Invalidate caches
        await InvalidateItemCaches(id, existingItem.UserId);

        _logger.LogUserAction("SetPriority", existingItem.UserId, new { TodoItemId = id, Priority = priority });
        
        return true;
    }

    public async Task<bool> SetDueDateAsync(int id, DateTime? dueDate)
    {
        using var scope = _logger.BeginScope("SetTodoItemDueDate", new { Id = id, DueDate = dueDate });

        var existingItem = await _repository.GetByIdAsync(id);
        if (existingItem == null)
        {
            return false;
        }

        var oldDueDate = existingItem.DueDate;
        existingItem.DueDate = dueDate;
        existingItem.UpdatedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(existingItem);

        // Log activity
        await _activityLogger.LogAsync(existingItem.UserId, "Changed todo item due date", 
            new { TodoItemId = id, Title = existingItem.Title, OldDueDate = oldDueDate, NewDueDate = dueDate });

        // Invalidate caches
        await InvalidateItemCaches(id, existingItem.UserId);

        _logger.LogUserAction("SetDueDate", existingItem.UserId, new { TodoItemId = id, DueDate = dueDate });
        
        return true;
    }

    public async Task<TodoItemDto> DuplicateAsync(int id)
    {
        using var scope = _logger.BeginScope("DuplicateTodoItem", new { Id = id });

        var originalItem = await _repository.GetByIdAsync(id);
        if (originalItem == null)
        {
            throw new KeyNotFoundException($"TodoItem with ID {id} not found");
        }

        var duplicatedItem = new TodoItem
        {
            Title = $"Copy of {originalItem.Title}",
            Description = originalItem.Description,
            DueDate = originalItem.DueDate,
            Priority = originalItem.Priority,
            UserId = originalItem.UserId,
            IsComplete = false, // Always start as incomplete
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Tags = originalItem.Tags?.ToList() // Copy tags
        };

        var createdItem = await _repository.CreateAsync(duplicatedItem);
        var dto = _mapper.Map<TodoItemDto>(createdItem);

        // Log activity
        await _activityLogger.LogAsync(originalItem.UserId, "Duplicated todo item", 
            new { OriginalId = id, NewId = createdItem.Id, Title = duplicatedItem.Title });

        // Record metrics
        _metrics.RecordTodoItemCreated(originalItem.UserId);

        // Invalidate caches
        await InvalidateUserCaches(originalItem.UserId);

        _logger.LogUserAction("DuplicateTodoItem", originalItem.UserId, 
            new { OriginalId = id, NewId = createdItem.Id, Title = duplicatedItem.Title });
        
        return dto;
    }

    private async Task<List<Tag>> ProcessTagsAsync(string[] tagNames)
    {
        var tags = new List<Tag>();
        
        foreach (var tagName in tagNames)
        {
            if (string.IsNullOrWhiteSpace(tagName))
                continue;

            var existingTag = await _tagRepository.GetByNameAsync(tagName.Trim());
            if (existingTag != null)
            {
                tags.Add(existingTag);
            }
            else
            {
                var newTag = await _tagRepository.CreateAsync(new Tag 
                { 
                    Name = tagName.Trim(),
                    CreatedAt = DateTime.UtcNow 
                });
                tags.Add(newTag);
            }
        }

        return tags;
    }

    private async Task InvalidateItemCaches(int todoItemId, int userId)
    {
        await _cache.InvalidateTodoItemCacheAsync(todoItemId);
        await InvalidateUserCaches(userId);
    }

    private async Task InvalidateUserCaches(int userId)
    {
        await _cache.InvalidateUserTodoItemsCacheAsync(userId);
        
        // Invalidate specific cache patterns
        var patterns = new[]
        {
            $"user_completed_todos:{userId}",
            $"user_pending_todos:{userId}",
            $"user_stats:{userId}",
            $"recent_todos:{userId}:*",
            $"tagged_todos:{userId}:*",
            $"search_todos:{userId}:*"
        };

        foreach (var pattern in patterns)
        {
            await _cache.RemoveByPatternAsync(pattern);
        }
    }
}