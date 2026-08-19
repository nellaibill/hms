using System.Text.Json;
using System.Threading.RateLimiting;
using HMS.Shared.Infrastructure;
using HMS.Shared.Kernel;
using Microsoft.AspNetCore.RateLimiting;

namespace HMS.Api.Configuration;

/// <summary>
/// Rate limiting for the whole host (HMS Security Hardening: "no rate limiting anywhere on
/// the API host"). Two layers, both partitioned per client IP:
///   - A global limiter applied to every request by default — generous enough not to
///     interfere with normal UI usage (dashboard polling, React Query refetches), but stops
///     a flood.
///   - A stricter named "Login" policy, applied explicitly via
///     <c>[EnableRateLimiting(LoginPolicyName)]</c> to both login actions — brute-force
///     throttling (ADR-015) is per-account, this is the complementary per-IP layer that
///     still applies even across many different usernames/emails.
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

    // The X-Forwarded-For header is deliberately not consulted here — this host isn't yet
    // known to sit behind a trusted reverse proxy that sets it, and trusting an
    // attacker-controlled header for rate-limit partitioning would let the limiter be
    // trivially bypassed. RemoteIpAddress is the real, un-spoofable connection source.
    private static string ClientKey(HttpContext context) =>
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
}
