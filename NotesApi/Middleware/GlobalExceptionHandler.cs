using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using NotesApi.Exceptions;

namespace NotesApi.Middleware
{
    /// <summary>
    /// Global exception handler registered via IExceptionHandler (.NET 8+).
    /// Catches all unhandled exceptions and maps them to a consistent
    /// RFC 7807 ProblemDetails response — no try/catch needed in controllers.
    /// </summary>
    public class GlobalExceptionHandler : IExceptionHandler
    {
        private readonly ILogger<GlobalExceptionHandler> _logger;

        public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
        {
            _logger = logger;
        }

        public async ValueTask<bool> TryHandleAsync(
            HttpContext httpContext,
            Exception exception,
            CancellationToken cancellationToken)
        {
            var (statusCode, errorCode, message) = MapException(exception);

            // Log with appropriate severity depending on whether it's expected or not.
            if (statusCode >= 500)
                _logger.LogError(exception, "Unhandled exception: {Message}", exception.Message);
            else
                _logger.LogWarning("Handled exception [{ErrorCode}]: {Message}", errorCode, exception.Message);

            var problem = new ProblemDetails
            {
                Status = statusCode,
                Title = errorCode,
                Detail = message,
                Instance = httpContext.Request.Path
            };

            httpContext.Response.StatusCode = statusCode;
            await httpContext.Response.WriteAsJsonAsync(problem, cancellationToken);

            // Return true — exception is handled, pipeline stops here.
            return true;
        }

        private static (int statusCode, string errorCode, string message) MapException(Exception exception)
        {
            // Known domain exceptions carry their own status code and error code.
            if (exception is AppException appEx)
                return (appEx.StatusCode, appEx.ErrorCode, appEx.Message);

            // JWT / claims issue in GetUserId() — treat as 401.
            if (exception is UnauthorizedAccessException)
                return (401, "unauthorized", "Authentication is required to access this resource.");

            // Anything else is a bug — return 500 with a generic message.
            return (500, "internal_error", "An unexpected error occurred. Please try again later.");
        }
    }
}