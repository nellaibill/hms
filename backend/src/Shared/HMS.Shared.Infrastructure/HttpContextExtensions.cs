using Microsoft.AspNetCore.Http;

namespace HMS.Shared.Infrastructure;

public static class HttpContextExtensions
{
    public static string GetCorrelationId(this HttpContext context)
        => context.Items.TryGetValue(CorrelationIdMiddleware.HeaderName, out var value) && value is string correlationId
            ? correlationId
            : string.Empty;
}
