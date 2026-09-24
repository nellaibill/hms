using System.Text.Json;
using System.Threading.RateLimiting;
using HMS.Shared.Infrastructure;
using HMS.Shared.Kernel;
using Microsoft.AspNetCore.RateLimiting;

namespace HMS.Api.Configuration;

/// <summary>
/// Rate limiting for the whole host (HMS Security Hardening: "no rate limiting anywhere on
/// the API host"). Limits come from the <c>RateLimiting</c> configuration section
/// (<see cref="RateLimitingOptions"/>) so staging can be loosened for load testing without a
/// code change. Layers:
///   - A global limiter on every request, chaining two fixed windows (ADR-084):
///       * per caller — the authenticated user, or the client IP for anonymous requests
///         (<see cref="RateLimitPartitionKeys.Caller"/>). Per-user rather than per-IP because
///         a hospital's workstations usually share one NAT'd public IP, which made every
///         staff member draw from a single 200/min budget.
///       * per IP ceiling — a much higher cap every request also counts against, so a single
///         address still can't flood the host even when spread across many valid tokens.
///   - A stricter named "Login" policy, applied explicitly via
///     <c>[EnableRateLimiting(LoginPolicyName)]</c> to both login actions — brute-force
///     throttling (ADR-015) is per-account, this is the complementary per-IP layer that
///     still applies even across many different usernames/emails.
///   - A "Write" policy (ADR-076) for sensitive-but-not-login write endpoints the global
///     limit doesn't specifically protect — message-send, notification broadcast, file
///     uploads. Per caller, same reasoning as the global per-caller window.
/// Registered here (not ModuleRegistration), same reasoning as CorsConfiguration/
/// JwtConfiguration: host-level pipeline configuration, not a business module.
/// </summary>
public static class RateLimitingConfiguration
{
    public static IServiceCollection AddHmsRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        var limits = configuration.GetSection(RateLimitingOptions.SectionName).Get<RateLimitingOptions>() ?? new RateLimitingOptions();

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.GlobalLimiter = PartitionedRateLimiter.CreateChained(
                PartitionedRateLimiter.Create<HttpContext, string>(context =>
                {
                    var key = RateLimitPartitionKeys.Caller(context);
                    var permitLimit = key.StartsWith("ip:", StringComparison.Ordinal)
                        ? limits.AnonymousPermitLimit
                        : limits.PerUserPermitLimit;
                    return FixedWindow(key, permitLimit);
                }),
                PartitionedRateLimiter.Create<HttpContext, string>(context =>
                    FixedWindow(RateLimitPartitionKeys.ClientIp(context), limits.PerIpCeilingPermitLimit)));

            options.AddPolicy(RateLimitingPolicyNames.Login, context =>
                FixedWindow(RateLimitPartitionKeys.ClientIp(context), limits.LoginPermitLimit));

            options.AddPolicy(RateLimitingPolicyNames.Write, context =>
                FixedWindow(RateLimitPartitionKeys.Caller(context), limits.WritePermitLimit));

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

    private static RateLimitPartition<string> FixedWindow(string key, int permitLimit) =>
        RateLimitPartition.GetFixedWindowLimiter(
            key,
            _ => new FixedWindowRateLimiterOptions
            {
                Window = TimeSpan.FromMinutes(1),
                PermitLimit = permitLimit,
                QueueLimit = 0,
            });
}

/// <summary>Per-minute permit limits, bound from the <c>RateLimiting</c> config section.</summary>
public sealed class RateLimitingOptions
{
    public const string SectionName = "RateLimiting";

    /// <summary>Per authenticated user (hospital or platform), across all endpoints.</summary>
    public int PerUserPermitLimit { get; set; } = 300;

    /// <summary>Per client IP for requests with no valid token.</summary>
    public int AnonymousPermitLimit { get; set; } = 200;

    /// <summary>Per client IP for every request — sized for a whole hospital behind one NAT.</summary>
    public int PerIpCeilingPermitLimit { get; set; } = 3000;

    /// <summary>Per client IP on the login actions.</summary>
    public int LoginPermitLimit { get; set; } = 10;

    /// <summary>Per caller on the "Write" policy's sensitive-write actions.</summary>
    public int WritePermitLimit { get; set; } = 60;
}
