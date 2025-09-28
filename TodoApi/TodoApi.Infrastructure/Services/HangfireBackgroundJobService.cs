using Hangfire;
using Microsoft.Extensions.Logging;

namespace TodoApi.Infrastructure.Services;

public class HangfireBackgroundJobService : IBackgroundJobService
{
    private readonly ILogger<HangfireBackgroundJobService> _logger;
    private readonly IMetricsCollector _metrics;

    public HangfireBackgroundJobService(ILogger<HangfireBackgroundJobService> logger, IMetricsCollector metrics)
    {
        _logger = logger;
        _metrics = metrics;
    }

    public void EnqueueJob<T>(System.Linq.Expressions.Expression<Func<T, Task>> methodCall)
    {
        try
        {
            BackgroundJob.Enqueue(methodCall);
            _logger.LogInformation("Enqueued background job: {JobType}", typeof(T).Name);
            _metrics.IncrementCounter("background_jobs_enqueued", 1, ("type", typeof(T).Name));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to enqueue background job: {JobType}", typeof(T).Name);
            _metrics.IncrementErrorCounter("background_job_enqueue_error", typeof(T).Name);
            throw;
        }
    }

    public void EnqueueJob<T>(System.Linq.Expressions.Expression<Action<T>> methodCall)
    {
        try
        {
            BackgroundJob.Enqueue(methodCall);
            _logger.LogInformation("Enqueued background job: {JobType}", typeof(T).Name);
            _metrics.IncrementCounter("background_jobs_enqueued", 1, ("type", typeof(T).Name));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to enqueue background job: {JobType}", typeof(T).Name);
            _metrics.IncrementErrorCounter("background_job_enqueue_error", typeof(T).Name);
            throw;
        }
    }

    public string EnqueueJobWithId<T>(System.Linq.Expressions.Expression<Func<T, Task>> methodCall)
    {
        try
        {
            var jobId = BackgroundJob.Enqueue(methodCall);
            _logger.LogInformation("Enqueued background job with ID {JobId}: {JobType}", jobId, typeof(T).Name);
            _metrics.IncrementCounter("background_jobs_enqueued", 1, ("type", typeof(T).Name));
            return jobId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to enqueue background job: {JobType}", typeof(T).Name);
            _metrics.IncrementErrorCounter("background_job_enqueue_error", typeof(T).Name);
            throw;
        }
    }

    public void ScheduleJob<T>(System.Linq.Expressions.Expression<Func<T, Task>> methodCall, TimeSpan delay)
    {
        try
        {
            BackgroundJob.Schedule(methodCall, delay);
            _logger.LogInformation("Scheduled background job to run in {Delay}: {JobType}", delay, typeof(T).Name);
            _metrics.IncrementCounter("background_jobs_scheduled", 1, ("type", typeof(T).Name));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to schedule background job: {JobType}", typeof(T).Name);
            _metrics.IncrementErrorCounter("background_job_schedule_error", typeof(T).Name);
            throw;
        }
    }

    public void ScheduleJob<T>(System.Linq.Expressions.Expression<Func<T, Task>> methodCall, DateTime scheduleAt)
    {
        try
        {
            BackgroundJob.Schedule(methodCall, scheduleAt);
            _logger.LogInformation("Scheduled background job to run at {ScheduleTime}: {JobType}", scheduleAt, typeof(T).Name);
            _metrics.IncrementCounter("background_jobs_scheduled", 1, ("type", typeof(T).Name));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to schedule background job: {JobType}", typeof(T).Name);
            _metrics.IncrementErrorCounter("background_job_schedule_error", typeof(T).Name);
            throw;
        }
    }

    public void AddOrUpdateRecurringJob<T>(string jobId, System.Linq.Expressions.Expression<Func<T, Task>> methodCall, string cronExpression)
    {
        try
        {
            RecurringJob.AddOrUpdate(jobId, methodCall, cronExpression);
            _logger.LogInformation("Added/Updated recurring job {JobId} with cron '{CronExpression}': {JobType}", 
                jobId, cronExpression, typeof(T).Name);
            _metrics.IncrementCounter("recurring_jobs_configured", 1, ("type", typeof(T).Name));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add/update recurring job {JobId}: {JobType}", jobId, typeof(T).Name);
            _metrics.IncrementErrorCounter("recurring_job_config_error", typeof(T).Name);
            throw;
        }
    }

    public void RemoveRecurringJob(string jobId)
    {
        try
        {
            RecurringJob.RemoveIfExists(jobId);
            _logger.LogInformation("Removed recurring job {JobId}", jobId);
            _metrics.IncrementCounter("recurring_jobs_removed", 1);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove recurring job {JobId}", jobId);
            _metrics.IncrementErrorCounter("recurring_job_remove_error");
            throw;
        }
    }

    public bool DeleteJob(string jobId)
    {
        try
        {
            var deleted = BackgroundJob.Delete(jobId);
            if (deleted)
            {
                _logger.LogInformation("Deleted background job {JobId}", jobId);
                _metrics.IncrementCounter("background_jobs_deleted", 1);
            }
            return deleted;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete background job {JobId}", jobId);
            _metrics.IncrementErrorCounter("background_job_delete_error");
            return false;
        }
    }

    public void TriggerRecurringJob(string jobId)
    {
        try
        {
            RecurringJob.Trigger(jobId);
            _logger.LogInformation("Triggered recurring job {JobId}", jobId);
            _metrics.IncrementCounter("recurring_jobs_triggered", 1);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to trigger recurring job {JobId}", jobId);
            _metrics.IncrementErrorCounter("recurring_job_trigger_error");
            throw;
        }
    }

    public async Task<int> GetPendingJobsCountAsync()
    {
        try
        {
            using var connection = JobStorage.Current.GetConnection();
            var monitoring = JobStorage.Current.GetMonitoringApi();
            var statistics = monitoring.GetStatistics();
            return (int)statistics.Enqueued;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get pending jobs count");
            return 0;
        }
    }

    public async Task<int> GetFailedJobsCountAsync()
    {
        try
        {
            using var connection = JobStorage.Current.GetConnection();
            var monitoring = JobStorage.Current.GetMonitoringApi();
            var statistics = monitoring.GetStatistics();
            return (int)statistics.Failed;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get failed jobs count");
            return 0;
        }
    }

    public async Task<int> GetSucceededJobsCountAsync()
    {
        try
        {
            using var connection = JobStorage.Current.GetConnection();
            var monitoring = JobStorage.Current.GetMonitoringApi();
            var statistics = monitoring.GetStatistics();
            return (int)statistics.Succeeded;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get succeeded jobs count");
            return 0;
        }
    }
}