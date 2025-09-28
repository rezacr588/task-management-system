using System.Diagnostics;
using System.Text;
using System.Text.Json;
using TodoApi.Infrastructure.Logging;
using TodoApi.Infrastructure.Services;

namespace TodoApi.WebApi;

public class RequestResponseLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestResponseLoggingMiddleware> _logger;
    private readonly IMetricsCollector _metrics;

    public RequestResponseLoggingMiddleware(
        RequestDelegate next,
        ILogger<RequestResponseLoggingMiddleware> logger,
        IMetricsCollector metrics)
    {
        _next = next;
        _logger = logger;
        _metrics = metrics;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        var requestId = Guid.NewGuid().ToString();

        // Set trace ID in response headers for debugging
        context.Response.Headers.Add("X-Request-Id", requestId);

        try
        {
            // Log incoming request
            await LogRequestAsync(context, requestId);

            // Capture original response body stream
            var originalBodyStream = context.Response.Body;

            using var responseBody = new MemoryStream();
            context.Response.Body = responseBody;

            // Process the request
            await _next(context);

            // Log outgoing response
            await LogResponseAsync(context, requestId, stopwatch.Elapsed);

            // Copy response back to original stream
            await responseBody.CopyToAsync(originalBodyStream);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            await LogErrorAsync(context, requestId, stopwatch.Elapsed, ex);
            throw;
        }
    }

    private async Task LogRequestAsync(HttpContext context, string requestId)
    {
        var request = context.Request;
        
        var requestInfo = new
        {
            RequestId = requestId,
            Method = request.Method,
            Path = request.Path,
            QueryString = request.QueryString.ToString(),
            Headers = GetSafeHeaders(request.Headers),
            UserAgent = request.Headers.UserAgent.ToString(),
            RemoteIpAddress = context.Connection.RemoteIpAddress?.ToString(),
            UserId = GetUserId(context),
            ContentType = request.ContentType,
            ContentLength = request.ContentLength
        };

        // Read request body if it's a POST/PUT/PATCH
        string? requestBody = null;
        if (request.Method is "POST" or "PUT" or "PATCH" && request.ContentLength > 0)
        {
            request.EnableBuffering();
            requestBody = await ReadRequestBodyAsync(request);
        }

        using var scope = _logger.BeginScope(requestInfo);
        
        _logger.LogInformation("Incoming {Method} request to {Path} from {RemoteIp} (RequestId: {RequestId})",
            request.Method, request.Path, context.Connection.RemoteIpAddress, requestId);

        if (!string.IsNullOrEmpty(requestBody) && IsLoggableContentType(request.ContentType))
        {
            _logger.LogDebug("Request body: {RequestBody}", SanitizeRequestBody(requestBody));
        }
    }

    private async Task LogResponseAsync(HttpContext context, string requestId, TimeSpan duration)
    {
        var response = context.Response;
        
        var responseInfo = new
        {
            RequestId = requestId,
            StatusCode = response.StatusCode,
            ContentType = response.ContentType,
            ContentLength = response.ContentLength,
            Duration = duration.TotalMilliseconds,
            Headers = GetSafeHeaders(response.Headers)
        };

        // Read response body for logging (only for errors or debug level)
        string? responseBody = null;
        if (response.StatusCode >= 400)
        {
            responseBody = await ReadResponseBodyAsync(context);
        }

        using var scope = _logger.BeginScope(responseInfo);

        var logLevel = response.StatusCode >= 500 ? LogLevel.Error :
                      response.StatusCode >= 400 ? LogLevel.Warning :
                      LogLevel.Information;

        _logger.Log(logLevel, 
            "Completed {Method} {Path} with {StatusCode} in {Duration}ms (RequestId: {RequestId})",
            context.Request.Method, context.Request.Path, response.StatusCode, duration.TotalMilliseconds, requestId);

        if (!string.IsNullOrEmpty(responseBody) && response.StatusCode >= 400)
        {
            _logger.LogDebug("Response body: {ResponseBody}", responseBody);
        }

        // Record metrics
        _metrics.RecordRequestDuration(context.Request.Path, context.Request.Method, duration);
        _metrics.IncrementRequestCounter(context.Request.Path, context.Request.Method, response.StatusCode);
    }

    private async Task LogErrorAsync(HttpContext context, string requestId, TimeSpan duration, Exception exception)
    {
        var errorInfo = new
        {
            RequestId = requestId,
            Method = context.Request.Method,
            Path = context.Request.Path,
            Duration = duration.TotalMilliseconds,
            ExceptionType = exception.GetType().Name,
            ExceptionMessage = exception.Message,
            StackTrace = exception.StackTrace,
            UserId = GetUserId(context)
        };

        using var scope = _logger.BeginScope(errorInfo);
        
        _logger.LogError(exception, "Unhandled exception in {Method} {Path} after {Duration}ms (RequestId: {RequestId})",
            context.Request.Method, context.Request.Path, duration.TotalMilliseconds, requestId);

        // Record error metrics
        _metrics.IncrementErrorCounter(exception.GetType().Name, context.Request.Path);
        
        // Set error response
        context.Response.StatusCode = 500;
        context.Response.ContentType = "application/json";

        var errorResponse = new
        {
            error = "Internal server error",
            requestId = requestId,
            timestamp = DateTimeOffset.UtcNow
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(errorResponse));
    }

    private static async Task<string> ReadRequestBodyAsync(HttpRequest request)
    {
        request.Body.Position = 0;
        using var reader = new StreamReader(request.Body, Encoding.UTF8, leaveOpen: true);
        var body = await reader.ReadToEndAsync();
        request.Body.Position = 0;
        return body;
    }

    private static async Task<string> ReadResponseBodyAsync(HttpContext context)
    {
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(context.Response.Body, leaveOpen: true);
        var body = await reader.ReadToEndAsync();
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        return body;
    }

    private static Dictionary<string, string> GetSafeHeaders(IHeaderDictionary headers)
    {
        var sensitiveHeaders = new[] { "authorization", "cookie", "x-api-key", "x-auth-token" };
        
        return headers
            .Where(h => !sensitiveHeaders.Contains(h.Key.ToLowerInvariant()))
            .ToDictionary(h => h.Key, h => h.Value.ToString());
    }

    private static string SanitizeRequestBody(string body)
    {
        // Remove sensitive data from request body logs
        if (string.IsNullOrEmpty(body))
            return body;

        try
        {
            using var document = JsonDocument.Parse(body);
            var root = document.RootElement.Clone();
            return root.ToString();
        }
        catch
        {
            // If not valid JSON, just return truncated version
            return body.Length > 1000 ? body[..1000] + "..." : body;
        }
    }

    private static string? GetUserId(HttpContext context)
    {
        return context.User?.FindFirst("sub")?.Value ?? 
               context.User?.FindFirst("userId")?.Value ??
               context.User?.Identity?.Name;
    }

    private static bool IsLoggableContentType(string? contentType)
    {
        if (string.IsNullOrEmpty(contentType))
            return false;

        var loggableTypes = new[] { "application/json", "application/xml", "text/plain", "text/xml" };
        return loggableTypes.Any(type => contentType.StartsWith(type, StringComparison.OrdinalIgnoreCase));
    }
}