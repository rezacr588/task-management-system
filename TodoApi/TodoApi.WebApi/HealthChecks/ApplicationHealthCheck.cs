using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace TodoApi.WebApi.HealthChecks
{
    public class ApplicationHealthCheck : IHealthCheck
    {
        public Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            // Basic application health check
            // In a real application, you might check:
            // - Memory usage
            // - CPU usage
            // - External service connectivity
            // - Application-specific metrics

            var isHealthy = true;
            var description = "Application is running normally";

            // Example: Check if the application has been running for a reasonable time
            var uptime = DateTime.UtcNow - System.Diagnostics.Process.GetCurrentProcess().StartTime.ToUniversalTime();

            if (uptime.TotalMinutes < 1)
            {
                isHealthy = false;
                description = "Application has been running for less than 1 minute";
            }

            return Task.FromResult(
                isHealthy
                    ? HealthCheckResult.Healthy(description)
                    : HealthCheckResult.Unhealthy(description));
        }
    }
}