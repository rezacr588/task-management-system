using MediatR;

namespace TodoApi.Domain.Events;

// Event Sourcing Events for TodoItems
public abstract record TodoItemEvent(int TodoItemId, int UserId, DateTime Timestamp) : INotification;

public record TodoItemCreatedEvent(
    int TodoItemId,
    int UserId,
    string Title,
    string Description,
    DateTime? DueDate,
    int Priority,
    DateTime Timestamp
) : TodoItemEvent(TodoItemId, UserId, Timestamp);

public record TodoItemUpdatedEvent(
    int TodoItemId,
    int UserId,
    string? OldTitle,
    string? NewTitle,
    string? OldDescription,
    string? NewDescription,
    bool? OldIsComplete,
    bool? NewIsComplete,
    DateTime? OldDueDate,
    DateTime? NewDueDate,
    int? OldPriority,
    int? NewPriority,
    DateTime Timestamp
) : TodoItemEvent(TodoItemId, UserId, Timestamp);

public record TodoItemCompletedEvent(
    int TodoItemId,
    int UserId,
    DateTime CompletedAt,
    DateTime Timestamp
) : TodoItemEvent(TodoItemId, UserId, Timestamp);

public record TodoItemDeletedEvent(
    int TodoItemId,
    int UserId,
    string Title,
    DateTime Timestamp
) : TodoItemEvent(TodoItemId, UserId, Timestamp);

public record TodoItemTagsAssignedEvent(
    int TodoItemId,
    int UserId,
    string[] AddedTags,
    DateTime Timestamp
) : TodoItemEvent(TodoItemId, UserId, Timestamp);

public record TodoItemTagsRemovedEvent(
    int TodoItemId,
    int UserId,
    string[] RemovedTags,
    DateTime Timestamp
) : TodoItemEvent(TodoItemId, UserId, Timestamp);

public record TodoItemPriorityChangedEvent(
    int TodoItemId,
    int UserId,
    int OldPriority,
    int NewPriority,
    DateTime Timestamp
) : TodoItemEvent(TodoItemId, UserId, Timestamp);

public record TodoItemDueDateChangedEvent(
    int TodoItemId,
    int UserId,
    DateTime? OldDueDate,
    DateTime? NewDueDate,
    DateTime Timestamp
) : TodoItemEvent(TodoItemId, UserId, Timestamp);