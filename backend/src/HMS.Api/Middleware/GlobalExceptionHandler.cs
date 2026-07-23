using HMS.Shared.Infrastructure;
using HMS.Shared.Kernel;
using Microsoft.AspNetCore.Diagnostics;

namespace HMS.Api.Middleware;

/// <summary>
/// The single global exception handler for the whole host, per docs/ErrorHandling.md —
/// no module implements its own try/catch-to-HTTP translation. Expected business
/// failures never reach here; they're returned as <see cref="Result"/> failures and
/// mapped to a response directly by the controller (see UsersController.MapFailure).
/// This handler only sees genuinely unexpected exceptions.
/// </summary>
public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        _logger.LogError(exception, "Unhandled exception while processing {Path}", httpContext.Request.Path);

        var error = new ApiErrorResponse
        {
            ErrorCode = "UNEXPECTED_ERROR",
            Message = "An unexpected error occurred.",
            CorrelationId = httpContext.GetCorrelationId(),
            Timestamp = DateTime.UtcNow,
        };

        httpContext.Response.ContentType = "application/json";
        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await httpContext.Response.WriteAsJsonAsync(error, cancellationToken);

        return true;
    }
}
