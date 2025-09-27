using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;

namespace TodoApi.WebApi.Filters
{
    public class GlobalExceptionFilter : IExceptionFilter
    {
        private readonly ILogger<GlobalExceptionFilter> _logger;

        public GlobalExceptionFilter(ILogger<GlobalExceptionFilter> logger)
        {
            _logger = logger;
        }

        public void OnException(ExceptionContext context)
        {
            var exception = context.Exception;
            _logger.LogError(exception, "An unhandled exception occurred: {Message}", exception.Message);

            var problemDetails = new ProblemDetails
            {
                Title = "An error occurred while processing your request.",
                Status = StatusCodes.Status500InternalServerError,
                Detail = exception.Message,
                Instance = context.HttpContext.Request.Path
            };

            // Handle specific exception types
            switch (exception)
            {
                case KeyNotFoundException:
                    problemDetails.Title = "Resource not found";
                    problemDetails.Status = StatusCodes.Status404NotFound;
                    problemDetails.Detail = "The requested resource was not found.";
                    break;

                case ArgumentException:
                    problemDetails.Title = "Invalid request";
                    problemDetails.Status = StatusCodes.Status400BadRequest;
                    problemDetails.Detail = exception.Message;
                    break;

                case UnauthorizedAccessException:
                    problemDetails.Title = "Unauthorized";
                    problemDetails.Status = StatusCodes.Status401Unauthorized;
                    problemDetails.Detail = "You are not authorized to perform this action.";
                    break;

                case InvalidOperationException:
                    problemDetails.Title = "Operation not allowed";
                    problemDetails.Status = StatusCodes.Status409Conflict;
                    problemDetails.Detail = exception.Message;
                    break;

                default:
                    // For unexpected exceptions, don't expose internal details in production
                    problemDetails.Detail = "An unexpected error occurred. Please try again later.";
                    break;
            }

            // Add trace ID for debugging
            if (context.HttpContext.TraceIdentifier != null)
            {
                problemDetails.Extensions["traceId"] = context.HttpContext.TraceIdentifier;
            }

            context.Result = new ObjectResult(problemDetails)
            {
                StatusCode = problemDetails.Status
            };

            context.ExceptionHandled = true;
        }
    }
}