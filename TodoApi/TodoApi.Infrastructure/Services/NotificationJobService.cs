using Microsoft.Extensions.Logging;
using TodoApi.Application.Interfaces;
using TodoApi.Infrastructure.Logging;

namespace TodoApi.Infrastructure.Services;

public class NotificationJobService : INotificationJobService
{
    private readonly ITodoItemRepository _todoItemRepository;
    private readonly IUserRepository _userRepository;
    private readonly IStructuredLogger<NotificationJobService> _logger;
    private readonly IMetricsCollector _metrics;
    private readonly ITodoItemNotificationService? _notificationService;

    public NotificationJobService(
        ITodoItemRepository todoItemRepository,
        IUserRepository userRepository,
        IStructuredLogger<NotificationJobService> logger,
        IMetricsCollector metrics,
        ITodoItemNotificationService? notificationService = null)
    {
        _todoItemRepository = todoItemRepository;
        _userRepository = userRepository;
        _logger = logger;
        _metrics = metrics;
        _notificationService = notificationService;
    }

    public async Task SendOverdueNotificationsAsync()
    {
        using var scope = _logger.BeginScope("SendOverdueNotifications");
        var startTime = DateTime.UtcNow;

        try
        {
            _logger.LogInformation("Starting overdue notifications job");

            var users = await _userRepository.GetAllAsync();
            var notificationsSent = 0;

            foreach (var user in users)
            {
                var overdueTodos = await _todoItemRepository.GetOverdueByUserIdAsync(user.Id);
                
                if (overdueTodos.Any())
                {
                    await SendOverdueNotificationToUser(user.Id, overdueTodos.ToList());
                    notificationsSent++;
                }
            }

            var duration = DateTime.UtcNow - startTime;
            _metrics.RecordBackgroundJobDuration("SendOverdueNotifications", duration, true);
            
            _logger.LogInformation("Completed overdue notifications job. Sent {NotificationCount} notifications in {Duration}ms",
                notificationsSent, duration.TotalMilliseconds);
        }
        catch (Exception ex)
        {
            var duration = DateTime.UtcNow - startTime;
            _metrics.RecordBackgroundJobDuration("SendOverdueNotifications", duration, false);
            _logger.LogError("Failed to send overdue notifications: {Error}", ex, ex.Message);
            throw;
        }
    }

    public async Task SendDueTodayNotificationsAsync()
    {
        using var scope = _logger.BeginScope("SendDueTodayNotifications");
        var startTime = DateTime.UtcNow;

        try
        {
            _logger.LogInformation("Starting due today notifications job");

            var users = await _userRepository.GetAllAsync();
            var notificationsSent = 0;

            foreach (var user in users)
            {
                var dueTodayTodos = await _todoItemRepository.GetDueTodayByUserIdAsync(user.Id);
                
                if (dueTodayTodos.Any())
                {
                    await SendDueTodayNotificationToUser(user.Id, dueTodayTodos.ToList());
                    notificationsSent++;
                }
            }

            var duration = DateTime.UtcNow - startTime;
            _metrics.RecordBackgroundJobDuration("SendDueTodayNotifications", duration, true);
            
            _logger.LogInformation("Completed due today notifications job. Sent {NotificationCount} notifications in {Duration}ms",
                notificationsSent, duration.TotalMilliseconds);
        }
        catch (Exception ex)
        {
            var duration = DateTime.UtcNow - startTime;
            _metrics.RecordBackgroundJobDuration("SendDueTodayNotifications", duration, false);
            _logger.LogError("Failed to send due today notifications: {Error}", ex, ex.Message);
            throw;
        }
    }

    public async Task SendWeeklyDigestAsync()
    {
        using var scope = _logger.BeginScope("SendWeeklyDigest");
        var startTime = DateTime.UtcNow;

        try
        {
            _logger.LogInformation("Starting weekly digest job");

            var users = await _userRepository.GetAllAsync();
            var digestsSent = 0;

            foreach (var user in users)
            {
                await SendWeeklyDigestToUser(user.Id);
                digestsSent++;
            }

            var duration = DateTime.UtcNow - startTime;
            _metrics.RecordBackgroundJobDuration("SendWeeklyDigest", duration, true);
            
            _logger.LogInformation("Completed weekly digest job. Sent {DigestCount} digests in {Duration}ms",
                digestsSent, duration.TotalMilliseconds);
        }
        catch (Exception ex)
        {
            var duration = DateTime.UtcNow - startTime;
            _metrics.RecordBackgroundJobDuration("SendWeeklyDigest", duration, false);
            _logger.LogError("Failed to send weekly digest: {Error}", ex, ex.Message);
            throw;
        }
    }

    public async Task SendDailyReminderAsync(int userId)
    {
        using var scope = _logger.BeginScope("SendDailyReminder", new { UserId = userId });
        var startTime = DateTime.UtcNow;

        try
        {
            _logger.LogInformation("Sending daily reminder to user {UserId}", userId);

            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("User {UserId} not found for daily reminder", userId);
                return;
            }

            var pendingTodos = await _todoItemRepository.GetPendingByUserIdAsync(userId);
            var dueTodayTodos = await _todoItemRepository.GetDueTodayByUserIdAsync(userId);
            var overdueTodos = await _todoItemRepository.GetOverdueByUserIdAsync(userId);

            var reminderData = new
            {
                PendingCount = pendingTodos.Count(),
                DueTodayCount = dueTodayTodos.Count(),
                OverdueCount = overdueTodos.Count(),
                DueTodayItems = dueTodayTodos.Take(5).Select(t => new { t.Id, t.Title, t.DueDate }).ToList(),
                OverdueItems = overdueTodos.Take(3).Select(t => new { t.Id, t.Title, t.DueDate }).ToList()
            };

            // Send real-time notification if user is online
            if (_notificationService != null)
            {
                await _notificationService.NotifyUserActivityAsync(userId, "daily_reminder", reminderData);
            }

            // Here you would typically send an email or push notification
            // For now, we'll just log it
            _logger.LogInformation("Daily reminder sent to user {UserId}: {ReminderData}", userId, reminderData);

            var duration = DateTime.UtcNow - startTime;
            _metrics.RecordBackgroundJobDuration("SendDailyReminder", duration, true);
        }
        catch (Exception ex)
        {
            var duration = DateTime.UtcNow - startTime;
            _metrics.RecordBackgroundJobDuration("SendDailyReminder", duration, false);
            _logger.LogError("Failed to send daily reminder to user {UserId}: {Error}", userId, ex, ex.Message);
            throw;
        }
    }

    public async Task ProcessEmailQueueAsync()
    {
        using var scope = _logger.BeginScope("ProcessEmailQueue");
        var startTime = DateTime.UtcNow;

        try
        {
            _logger.LogInformation("Starting email queue processing");

            // This is a placeholder for actual email queue processing
            // In a real implementation, you would:
            // 1. Fetch pending emails from queue
            // 2. Send them via email service (SendGrid, AWS SES, etc.)
            // 3. Mark as sent or failed
            // 4. Handle retries for failed emails

            await Task.Delay(100); // Simulate processing

            var duration = DateTime.UtcNow - startTime;
            _metrics.RecordBackgroundJobDuration("ProcessEmailQueue", duration, true);
            
            _logger.LogInformation("Completed email queue processing in {Duration}ms", duration.TotalMilliseconds);
        }
        catch (Exception ex)
        {
            var duration = DateTime.UtcNow - startTime;
            _metrics.RecordBackgroundJobDuration("ProcessEmailQueue", duration, false);
            _logger.LogError("Failed to process email queue: {Error}", ex, ex.Message);
            throw;
        }
    }

    public async Task CleanupOldDataAsync()
    {
        using var scope = _logger.BeginScope("CleanupOldData");
        var startTime = DateTime.UtcNow;

        try
        {
            _logger.LogInformation("Starting old data cleanup");

            var cutoffDate = DateTime.UtcNow.AddDays(-90); // Keep data for 90 days
            
            // Clean up completed todos older than cutoff
            var completedTodos = await _todoItemRepository.GetCompletedBeforeDateAsync(cutoffDate);
            var deletedCount = 0;

            foreach (var todo in completedTodos)
            {
                await _todoItemRepository.DeleteAsync(todo.Id);
                deletedCount++;
            }

            var duration = DateTime.UtcNow - startTime;
            _metrics.RecordBackgroundJobDuration("CleanupOldData", duration, true);
            
            _logger.LogInformation("Completed old data cleanup. Deleted {DeletedCount} old todos in {Duration}ms",
                deletedCount, duration.TotalMilliseconds);
        }
        catch (Exception ex)
        {
            var duration = DateTime.UtcNow - startTime;
            _metrics.RecordBackgroundJobDuration("CleanupOldData", duration, false);
            _logger.LogError("Failed to cleanup old data: {Error}", ex, ex.Message);
            throw;
        }
    }

    public async Task GenerateReportsAsync()
    {
        using var scope = _logger.BeginScope("GenerateReports");
        var startTime = DateTime.UtcNow;

        try
        {
            _logger.LogInformation("Starting report generation");

            var users = await _userRepository.GetAllAsync();
            var reportsGenerated = 0;

            foreach (var user in users)
            {
                await GenerateUserProductivityReport(user.Id);
                reportsGenerated++;
            }

            var duration = DateTime.UtcNow - startTime;
            _metrics.RecordBackgroundJobDuration("GenerateReports", duration, true);
            
            _logger.LogInformation("Completed report generation. Generated {ReportCount} reports in {Duration}ms",
                reportsGenerated, duration.TotalMilliseconds);
        }
        catch (Exception ex)
        {
            var duration = DateTime.UtcNow - startTime;
            _metrics.RecordBackgroundJobDuration("GenerateReports", duration, false);
            _logger.LogError("Failed to generate reports: {Error}", ex, ex.Message);
            throw;
        }
    }

    public async Task SyncExternalSystemsAsync()
    {
        using var scope = _logger.BeginScope("SyncExternalSystems");
        var startTime = DateTime.UtcNow;

        try
        {
            _logger.LogInformation("Starting external systems sync");

            // This is a placeholder for syncing with external systems
            // In a real implementation, you might sync with:
            // - Calendar systems (Google Calendar, Outlook)
            // - Project management tools (Jira, Trello)
            // - Communication platforms (Slack, Teams)
            // - Time tracking systems (Toggl, Harvest)

            await Task.Delay(200); // Simulate sync

            var duration = DateTime.UtcNow - startTime;
            _metrics.RecordBackgroundJobDuration("SyncExternalSystems", duration, true);
            
            _logger.LogInformation("Completed external systems sync in {Duration}ms", duration.TotalMilliseconds);
        }
        catch (Exception ex)
        {
            var duration = DateTime.UtcNow - startTime;
            _metrics.RecordBackgroundJobDuration("SyncExternalSystems", duration, false);
            _logger.LogError("Failed to sync external systems: {Error}", ex, ex.Message);
            throw;
        }
    }

    private async Task SendOverdueNotificationToUser(int userId, List<object> overdueTodos)
    {
        var notificationData = new
        {
            Type = "overdue_tasks",
            Count = overdueTodos.Count,
            Items = overdueTodos.Take(5) // Show top 5 overdue items
        };

        if (_notificationService != null)
        {
            await _notificationService.NotifyUserActivityAsync(userId, "overdue_notification", notificationData);
        }

        _logger.LogInformation("Sent overdue notification to user {UserId} for {Count} items", userId, overdueTodos.Count);
    }

    private async Task SendDueTodayNotificationToUser(int userId, List<object> dueTodayTodos)
    {
        var notificationData = new
        {
            Type = "due_today_tasks",
            Count = dueTodayTodos.Count,
            Items = dueTodayTodos.Take(10) // Show all due today items
        };

        if (_notificationService != null)
        {
            await _notificationService.NotifyUserActivityAsync(userId, "due_today_notification", notificationData);
        }

        _logger.LogInformation("Sent due today notification to user {UserId} for {Count} items", userId, dueTodayTodos.Count);
    }

    private async Task SendWeeklyDigestToUser(int userId)
    {
        var weekStart = DateTime.UtcNow.AddDays(-7);
        var completedThisWeek = await _todoItemRepository.GetCompletedByUserSinceDateAsync(userId, weekStart);
        var pendingTodos = await _todoItemRepository.GetPendingByUserIdAsync(userId);

        var digestData = new
        {
            WeekStart = weekStart,
            CompletedCount = completedThisWeek.Count(),
            PendingCount = pendingTodos.Count(),
            TopCompletedItems = completedThisWeek.Take(5).Select(t => new { t.Title, t.CompletedAt }).ToList()
        };

        if (_notificationService != null)
        {
            await _notificationService.NotifyUserActivityAsync(userId, "weekly_digest", digestData);
        }

        _logger.LogInformation("Sent weekly digest to user {UserId}: {DigestData}", userId, digestData);
    }

    private async Task GenerateUserProductivityReport(int userId)
    {
        var monthStart = DateTime.UtcNow.AddDays(-30);
        var completedThisMonth = await _todoItemRepository.GetCompletedByUserSinceDateAsync(userId, monthStart);
        var totalTodos = await _todoItemRepository.GetByUserIdAsync(userId);

        var report = new
        {
            UserId = userId,
            ReportDate = DateTime.UtcNow,
            CompletedThisMonth = completedThisMonth.Count(),
            TotalTodos = totalTodos.Count(),
            CompletionRate = totalTodos.Any() ? (double)completedThisMonth.Count() / totalTodos.Count() * 100 : 0
        };

        // In a real implementation, you would save this report to database or send via email
        _logger.LogInformation("Generated productivity report for user {UserId}: {Report}", userId, report);
    }
}