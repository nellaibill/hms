using Microsoft.AspNetCore.Http;

namespace HMS.Shared.Infrastructure;

/// <summary>
/// Reads the caller's X-Correlation-Id (or generates one), stores it on
/// <see cref="HttpContext.Items"/>, and echoes it back on the response —
/// see docs/ApiStandards.md §3 and §5, and docs/Logging.md.
/// </summary>
public class CorrelationIdMiddleware
{
    public const string HeaderName = "X-Correlation-Id";

    private readonly RequestDelegate _next;

    public CorrelationIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers.TryGetValue(HeaderName, out var existing) && !string.IsNullOrWhiteSpace(existing)
            ? existing.ToString()
            : Guid.NewGuid().ToString();

        context.Items[HeaderName] = correlationId;
        context.Response.Headers[HeaderName] = correlationId;

        await _next(context);
    }
}
