using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using TodoApi.Infrastructure.Logging;
using TodoApi.Infrastructure.Services;

namespace TodoApi.WebApi;

[Authorize]
public class TodoItemHub : Hub
{
    private readonly ILogger<TodoItemHub> _logger;
    private readonly IMetricsCollector _metrics;

    public TodoItemHub(ILogger<TodoItemHub> logger, IMetricsCollector metrics)
    {
        _logger = logger;
        _metrics = metrics;
    }

    public override async Task OnConnectedAsync()
    {
        var userId = GetUserId();
        if (userId != null)
        {
            // Join user-specific group
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");
            
            _logger.LogInformation("User {UserId} connected to TodoItem hub with connection {ConnectionId}", 
                userId, Context.ConnectionId);
            
            _metrics.IncrementCounter("signalr_connections", 1, ("event", "connected"));
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = GetUserId();
        if (userId != null)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"user_{userId}");
            
            _logger.LogInformation("User {UserId} disconnected from TodoItem hub with connection {ConnectionId}", 
                userId, Context.ConnectionId);
            
            _metrics.IncrementCounter("signalr_connections", 1, ("event", "disconnected"));
        }

        if (exception != null)
        {
            _logger.LogError(exception, "SignalR disconnection error for user {UserId}", userId);
            _metrics.IncrementErrorCounter("signalr_disconnect_error");
        }

        await base.OnDisconnectedAsync(exception);
    }

    public async Task JoinProjectGroup(string projectId)
    {
        var userId = GetUserId();
        
        // In a real application, you'd verify the user has access to this project
        await Groups.AddToGroupAsync(Context.ConnectionId, $"project_{projectId}");
        
        _logger.LogInformation("User {UserId} joined project group {ProjectId}", userId, projectId);
    }

    public async Task LeaveProjectGroup(string projectId)
    {
        var userId = GetUserId();
        
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"project_{projectId}");
        
        _logger.LogInformation("User {UserId} left project group {ProjectId}", userId, projectId);
    }

    public async Task SendTypingIndicator(string action)
    {
        var userId = GetUserId();
        if (userId != null)
        {
            // Broadcast typing indicator to other users (not to self)
            await Clients.GroupExcept($"user_{userId}", Context.ConnectionId).SendAsync("UserTyping", new
            {
                UserId = userId,
                Action = action,
                Timestamp = DateTime.UtcNow
            });
        }
    }

    private string? GetUserId()
    {
        return Context.User?.FindFirst("userId")?.Value ?? 
               Context.User?.FindFirst("sub")?.Value ??
               Context.User?.Identity?.Name;
    }
}

/// <summary>
/// Service for sending real-time notifications to connected clients
/// </summary>
public interface ITodoItemNotificationService
{
    Task NotifyTodoItemCreatedAsync(int userId, object todoItem);
    Task NotifyTodoItemUpdatedAsync(int userId, object todoItem);
    Task NotifyTodoItemDeletedAsync(int userId, int todoItemId);
    Task NotifyTodoItemCompletedAsync(int userId, object todoItem);
    Task NotifyUserActivityAsync(int userId, string activity, object? details = null);
    Task NotifySystemMessageAsync(string message, object? data = null);
    Task NotifyProjectMembersAsync(string projectId, string message, object? data = null);
}

public class TodoItemNotificationService : ITodoItemNotificationService
{
    private readonly IHubContext<TodoItemHub> _hubContext;
    private readonly ILogger<TodoItemNotificationService> _logger;
    private readonly IMetricsCollector _metrics;

    public TodoItemNotificationService(
        IHubContext<TodoItemHub> hubContext,
        ILogger<TodoItemNotificationService> logger,
        IMetricsCollector metrics)
    {
        _hubContext = hubContext;
        _logger = logger;
        _metrics = metrics;
    }

    public async Task NotifyTodoItemCreatedAsync(int userId, object todoItem)
    {
        try
        {
            var notification = new
            {
                Type = "todo_item_created",
                UserId = userId,
                TodoItem = todoItem,
                Timestamp = DateTime.UtcNow
            };

            await _hubContext.Clients.Group($"user_{userId}").SendAsync("TodoItemCreated", notification);
            
            _logger.LogInformation("Sent TodoItemCreated notification to user {UserId}", userId);
            _metrics.IncrementCounter("signalr_notifications", 1, ("type", "todo_item_created"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send TodoItemCreated notification");
            _metrics.IncrementErrorCounter("signalr_notification_error", "NotifyTodoItemCreated");
        }
    }

    public async Task NotifyTodoItemUpdatedAsync(int userId, object todoItem)
    {
        try
        {
            var notification = new
            {
                Type = "todo_item_updated",
                UserId = userId,
                TodoItem = todoItem,
                Timestamp = DateTime.UtcNow
            };

            await _hubContext.Clients.Group($"user_{userId}").SendAsync("TodoItemUpdated", notification);
            
            _logger.LogInformation("Sent TodoItemUpdated notification to user {UserId}", userId);
            _metrics.IncrementCounter("signalr_notifications", 1, ("type", "todo_item_updated"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send TodoItemUpdated notification");
            _metrics.IncrementErrorCounter("signalr_notification_error", "NotifyTodoItemUpdated");
        }
    }

    public async Task NotifyTodoItemDeletedAsync(int userId, int todoItemId)
    {
        try
        {
            var notification = new
            {
                Type = "todo_item_deleted",
                UserId = userId,
                TodoItemId = todoItemId,
                Timestamp = DateTime.UtcNow
            };

            await _hubContext.Clients.Group($"user_{userId}").SendAsync("TodoItemDeleted", notification);
            
            _logger.LogInformation("Sent TodoItemDeleted notification to user {UserId} for item {TodoItemId}", userId, todoItemId);
            _metrics.IncrementCounter("signalr_notifications", 1, ("type", "todo_item_deleted"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send TodoItemDeleted notification");
            _metrics.IncrementErrorCounter("signalr_notification_error", "NotifyTodoItemDeleted");
        }
    }

    public async Task NotifyTodoItemCompletedAsync(int userId, object todoItem)
    {
        try
        {
            var notification = new
            {
                Type = "todo_item_completed",
                UserId = userId,
                TodoItem = todoItem,
                Timestamp = DateTime.UtcNow
            };

            await _hubContext.Clients.Group($"user_{userId}").SendAsync("TodoItemCompleted", notification);
            
            _logger.LogInformation("Sent TodoItemCompleted notification to user {UserId}", userId);
            _metrics.IncrementCounter("signalr_notifications", 1, ("type", "todo_item_completed"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send TodoItemCompleted notification");
            _metrics.IncrementErrorCounter("signalr_notification_error", "NotifyTodoItemCompleted");
        }
    }

    public async Task NotifyUserActivityAsync(int userId, string activity, object? details = null)
    {
        try
        {
            var notification = new
            {
                Type = "user_activity",
                UserId = userId,
                Activity = activity,
                Details = details,
                Timestamp = DateTime.UtcNow
            };

            await _hubContext.Clients.Group($"user_{userId}").SendAsync("UserActivity", notification);
            
            _logger.LogTrace("Sent UserActivity notification to user {UserId}: {Activity}", userId, activity);
            _metrics.IncrementCounter("signalr_notifications", 1, ("type", "user_activity"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send UserActivity notification");
            _metrics.IncrementErrorCounter("signalr_notification_error", "NotifyUserActivity");
        }
    }

    public async Task NotifySystemMessageAsync(string message, object? data = null)
    {
        try
        {
            var notification = new
            {
                Type = "system_message",
                Message = message,
                Data = data,
                Timestamp = DateTime.UtcNow
            };

            await _hubContext.Clients.All.SendAsync("SystemMessage", notification);
            
            _logger.LogInformation("Sent SystemMessage notification: {Message}", message);
            _metrics.IncrementCounter("signalr_notifications", 1, ("type", "system_message"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send SystemMessage notification");
            _metrics.IncrementErrorCounter("signalr_notification_error", "NotifySystemMessage");
        }
    }

    public async Task NotifyProjectMembersAsync(string projectId, string message, object? data = null)
    {
        try
        {
            var notification = new
            {
                Type = "project_message",
                ProjectId = projectId,
                Message = message,
                Data = data,
                Timestamp = DateTime.UtcNow
            };

            await _hubContext.Clients.Group($"project_{projectId}").SendAsync("ProjectMessage", notification);
            
            _logger.LogInformation("Sent ProjectMessage notification to project {ProjectId}: {Message}", projectId, message);
            _metrics.IncrementCounter("signalr_notifications", 1, ("type", "project_message"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send ProjectMessage notification");
            _metrics.IncrementErrorCounter("signalr_notification_error", "NotifyProjectMembers");
        }
    }
}