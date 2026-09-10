using System.Text.Json;
using System.Threading.RateLimiting;
using HMS.Shared.Infrastructure;
using HMS.Shared.Kernel;
using Microsoft.AspNetCore.RateLimiting;

namespace HMS.Api.Configuration;

/// <summary>
/// Rate limiting for the whole host (HMS Security Hardening: "no rate limiting anywhere on
/// the API host"). Three layers, all partitioned per client IP:
///   - A global limiter applied to every request by default — generous enough not to
///     interfere with normal UI usage (dashboard polling, React Query refetches), but stops
///     a flood.
///   - A stricter named "Login" policy, applied explicitly via
///     <c>[EnableRateLimiting(LoginPolicyName)]</c> to both login actions — brute-force
///     throttling (ADR-015) is per-account, this is the complementary per-IP layer that
///     still applies even across many different usernames/emails.
///   - A "Write" policy (ADR-076) for sensitive-but-not-login write endpoints the global
///     200/min limit doesn't specifically protect — message-send, notification broadcast,
///     file uploads.
/// Registered here (not ModuleRegistration), same reasoning as CorsConfiguration/
/// JwtConfiguration: host-level pipeline configuration, not a business module.
/// </summary>
public static class RateLimitingConfiguration
{
    public static IServiceCollection AddHmsRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    ClientKey(context),
                    _ => new FixedWindowRateLimiterOptions
                    {
                        Window = TimeSpan.FromMinutes(1),
                        PermitLimit = 200,
                        QueueLimit = 0,
                    }));

            options.AddPolicy(RateLimitingPolicyNames.Login, context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    ClientKey(context),
                    _ => new FixedWindowRateLimiterOptions
                    {
                        Window = TimeSpan.FromMinutes(1),
                        PermitLimit = 10,
                        QueueLimit = 0,
                    }));

            options.AddPolicy(RateLimitingPolicyNames.Write, context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    ClientKey(context),
                    _ => new FixedWindowRateLimiterOptions
                    {
                        Window = TimeSpan.FromMinutes(1),
                        PermitLimit = 60,
                        QueueLimit = 0,
                    }));

            options.OnRejected = async (rejectedContext, cancellationToken) =>
            {
                rejectedContext.HttpContext.Response.ContentType = "application/json";
                var error = new ApiErrorResponse
                {
                    ErrorCode = "RATE_LIMIT.TOO_MANY_REQUESTS",
                    Message = "Too many requests. Please try again shortly.",
                    CorrelationId = rejectedContext.HttpContext.GetCorrelationId(),
                    Timestamp = DateTime.UtcNow,
                };
                await rejectedContext.HttpContext.Response.WriteAsync(JsonSerializer.Serialize(error), cancellationToken);
            };
        });

        return services;
    }

    public static WebApplication UseHmsRateLimiting(this WebApplication app)
    {
        app.UseRateLimiter();
        return app;
    }

    // RemoteIpAddress, not X-Forwarded-For directly — this method never reads that header
    // itself. In the Docker Compose deployment (browser talks straight to the API container),
    // RemoteIpAddress already is the real client. In the Windows/nginx reverse-proxy
    // deployment, ForwardedHeadersMiddleware (Program.cs, registered as the very first
    // middleware, ADR-076) rewrites RemoteIpAddress in place from X-Forwarded-For — but only
    // when the immediate connection is a KnownProxy/KnownNetwork (defaults to loopback only,
    // never overridden here), so a client can't spoof the header to claim a different address
    // unless it's already relayed through the trusted local nginx. Before ADR-076, this
    // deployment path collapsed every real client into nginx's own loopback address here,
    // defeating per-client partitioning entirely (see ADR-076 for the full analysis).
    private static string ClientKey(HttpContext context) =>
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
}
