using System.Security.Cryptography;
using Hangfire.Dashboard;
using HMS.Modules.Sync.Application;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HMS.Modules.Sync.Infrastructure;

/// <summary>
/// Gatekeeper for the Hangfire dashboard (ADR-133 §10.3). The React app authenticates with a
/// bearer token in a header, which a browser tab opening <c>/hangfire</c> cannot send, so:
/// <list type="number">
/// <item>The Cloud Sync page calls <c>POST /api/v1/sync-monitor/dashboard-session</c>, which needs a
/// hospital token with identity-administration.edit, and gets a one-time link valid for 60 seconds
/// (DataProtection-encrypted, single use).</item>
/// <item>Opening the link exchanges it for an HttpOnly, SameSite=Strict cookie scoped to the
/// dashboard path, valid for 15 minutes, and redirects to the dashboard.</item>
/// <item><see cref="HangfireDashboardAuthorizationFilter"/> admits only requests with a valid cookie.
/// Without one the dashboard answers 401; it is never public.</item>
/// </list>
/// </summary>
internal sealed class HangfireDashboardAccess
{
    public const string CookieName = "hms_sync_dashboard";
    public const string AccessQueryKey = "access";

    private static readonly TimeSpan HandoffLifetime = TimeSpan.FromSeconds(60);
    private static readonly TimeSpan SessionLifetime = TimeSpan.FromMinutes(15);

    private readonly ITimeLimitedDataProtector _handoffProtector;
    private readonly ITimeLimitedDataProtector _sessionProtector;
    private readonly IMemoryCache _redeemedHandoffs;
    private readonly string _dashboardPath;

    public HangfireDashboardAccess(IDataProtectionProvider dataProtection, IMemoryCache cache, IOptions<SyncHangfireOptions> options)
    {
        _handoffProtector = dataProtection.CreateProtector("HMS.Sync.HangfireDashboard.Handoff.v1").ToTimeLimitedDataProtector();
        _sessionProtector = dataProtection.CreateProtector("HMS.Sync.HangfireDashboard.Session.v1").ToTimeLimitedDataProtector();
        _redeemedHandoffs = cache;
        _dashboardPath = options.Value.DashboardPath;
    }

    public string DashboardPath => _dashboardPath;

    public (string Url, DateTime SessionExpiresAt) CreateHandoff(Guid userId, Guid tenantId)
    {
        var nonce = Convert.ToHexString(RandomNumberGenerator.GetBytes(16));
        var token = _handoffProtector.Protect($"{nonce}|{userId}|{tenantId}", HandoffLifetime);
        return ($"{_dashboardPath}?{AccessQueryKey}={Uri.EscapeDataString(token)}", DateTime.UtcNow.Add(SessionLifetime));
    }

    /// <summary>Exchanges a one-time link for a session cookie value. False if the link is
    /// forged, expired or already used.</summary>
    public bool TryRedeemHandoff(string token, out string sessionValue, out DateTimeOffset sessionExpiresAt)
    {
        sessionValue = string.Empty;
        sessionExpiresAt = default;

        string payload;
        try
        {
            payload = _handoffProtector.Unprotect(token);
        }
        catch (CryptographicException)
        {
            return false;
        }

        var parts = payload.Split('|');
        if (parts.Length != 3)
        {
            return false;
        }

        // Single use: remember redeemed nonces for longer than a link can live.
        var cacheKey = "sync-dashboard-handoff:" + parts[0];
        if (_redeemedHandoffs.TryGetValue(cacheKey, out _))
        {
            return false;
        }

        _redeemedHandoffs.Set(cacheKey, true, HandoffLifetime * 2);

        sessionExpiresAt = DateTimeOffset.UtcNow.Add(SessionLifetime);
        sessionValue = _sessionProtector.Protect($"{parts[1]}|{parts[2]}", SessionLifetime);
        return true;
    }

    public bool IsValidSession(string? cookieValue)
    {
        if (string.IsNullOrEmpty(cookieValue))
        {
            return false;
        }

        try
        {
            _sessionProtector.Unprotect(cookieValue);
            return true;
        }
        catch (CryptographicException)
        {
            return false;
        }
    }
}

/// <summary>Turns a one-time dashboard link into the session cookie (see
/// <see cref="HangfireDashboardAccess"/>). Runs only for the dashboard path.</summary>
internal sealed class HangfireDashboardHandoffMiddleware
{
    private readonly RequestDelegate _next;

    public HangfireDashboardHandoffMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, HangfireDashboardAccess access, ILogger<HangfireDashboardHandoffMiddleware> logger)
    {
        if (!context.Request.Path.StartsWithSegments(access.DashboardPath)
            || !context.Request.Query.TryGetValue(HangfireDashboardAccess.AccessQueryKey, out var token))
        {
            await _next(context);
            return;
        }

        if (!access.TryRedeemHandoff(token.ToString(), out var session, out var expiresAt))
        {
            logger.LogWarning("Rejected an invalid, expired or reused Hangfire dashboard link from {RemoteIp}.", context.Connection.RemoteIpAddress);
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.Response.ContentType = "text/plain; charset=utf-8";
            await context.Response.WriteAsync("This dashboard link has expired or was already used. Open the dashboard again from the Cloud Sync page.");
            return;
        }

        context.Response.Cookies.Append(HangfireDashboardAccess.CookieName, session, new CookieOptions
        {
            HttpOnly = true,
            Secure = context.Request.IsHttps,
            SameSite = SameSiteMode.Strict,
            Path = access.DashboardPath,
            Expires = expiresAt,
            IsEssential = true,
        });

        // Drop the one-time token from the address bar and history.
        context.Response.Redirect(context.Request.PathBase + access.DashboardPath);
    }
}

/// <summary>Hangfire's own authorization hook: only a valid dashboard session cookie gets in.</summary>
internal sealed class HangfireDashboardAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();
        var access = httpContext.RequestServices.GetRequiredService<HangfireDashboardAccess>();
        return access.IsValidSession(httpContext.Request.Cookies[HangfireDashboardAccess.CookieName]);
    }
}
