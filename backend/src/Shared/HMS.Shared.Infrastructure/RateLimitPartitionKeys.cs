using Microsoft.AspNetCore.Http;

namespace HMS.Shared.Infrastructure;

/// <summary>
/// Partition keys for the host's rate limiters (see HMS.Api's RateLimitingConfiguration,
/// ADR-084). Lives here rather than in HMS.Api so it's unit-testable without the host.
/// </summary>
public static class RateLimitPartitionKeys
{
    /// <summary>
    /// The verified caller when there is one ("user:{id}" / "platform:{id}"), otherwise the
    /// client IP ("ip:{address}"). A whole hospital typically sits behind one NAT'd public
    /// IP, so partitioning authenticated traffic by IP alone made every workstation share a
    /// single budget. Only reads <see cref="HttpContext.User"/>, which UseAuthentication()
    /// populates from a signature-validated JWT — a forged or expired token leaves the
    /// request anonymous and it falls back to the IP key, so it can't mint fresh partitions.
    /// </summary>
    public static string Caller(HttpContext context)
    {
        if (context.User.GetUserId() is { } userId)
        {
            return $"user:{userId}";
        }

        if (context.User.GetPlatformUserId() is { } platformUserId)
        {
            return $"platform:{platformUserId}";
        }

        return ClientIp(context);
    }

    /// <summary>
    /// The client IP, regardless of authentication. RemoteIpAddress, not X-Forwarded-For
    /// directly — ForwardedHeadersMiddleware rewrites it only for loopback proxies (ADR-076).
    /// </summary>
    public static string ClientIp(HttpContext context) =>
        $"ip:{context.Connection.RemoteIpAddress?.ToString() ?? "unknown"}";
}
