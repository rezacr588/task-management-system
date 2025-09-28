namespace TodoApi.Infrastructure.Services;

public interface IBackgroundJobService
{
    // Immediate jobs
    void EnqueueJob<T>(System.Linq.Expressions.Expression<Func<T, Task>> methodCall);
    void EnqueueJob<T>(System.Linq.Expressions.Expression<Action<T>> methodCall);
    string EnqueueJobWithId<T>(System.Linq.Expressions.Expression<Func<T, Task>> methodCall);
    
    // Delayed jobs
    void ScheduleJob<T>(System.Linq.Expressions.Expression<Func<T, Task>> methodCall, TimeSpan delay);
    void ScheduleJob<T>(System.Linq.Expressions.Expression<Func<T, Task>> methodCall, DateTime scheduleAt);
    
    // Recurring jobs
    void AddOrUpdateRecurringJob<T>(string jobId, System.Linq.Expressions.Expression<Func<T, Task>> methodCall, string cronExpression);
    void RemoveRecurringJob(string jobId);
    
    // Job management
    bool DeleteJob(string jobId);
    void TriggerRecurringJob(string jobId);
    
    // Job monitoring
    Task<int> GetPendingJobsCountAsync();
    Task<int> GetFailedJobsCountAsync();
    Task<int> GetSucceededJobsCountAsync();
}

public interface INotificationJobService
{
    Task SendOverdueNotificationsAsync();
    Task SendDueTodayNotificationsAsync();
    Task SendWeeklyDigestAsync();
    Task SendDailyReminderAsync(int userId);
    Task ProcessEmailQueueAsync();
    Task CleanupOldDataAsync();
    Task GenerateReportsAsync();
    Task SyncExternalSystemsAsync();
}

public interface IMaintenanceJobService
{
    Task DatabaseCleanupAsync();
    Task CacheWarmupAsync();
    Task BackupDataAsync();
    Task AnalyzePerformanceAsync();
    Task UpdateSearchIndexAsync();
    Task OptimizeDatabaseAsync();
    Task PurgeOldLogsAsync();
    Task ValidateDataIntegrityAsync();
}