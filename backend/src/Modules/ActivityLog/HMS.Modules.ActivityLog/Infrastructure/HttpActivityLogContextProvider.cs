using HMS.Modules.ActivityLog.Application.Abstractions;
using HMS.Shared.Infrastructure;
using HMS.Shared.Kernel;
using Microsoft.AspNetCore.Http;

namespace HMS.Modules.ActivityLog.Infrastructure;

/// <summary>Reads TenantId from the request-scoped ITenantContext and the rest from the
/// current HttpContext — the same sources the rest of the codebase already uses
/// (ClaimsPrincipalExtensions.GetUserId, HttpContextExtensions.GetCorrelationId). Every
/// value is null when there is no HTTP request (background work).</summary>
internal class HttpActivityLogContextProvider : IActivityLogContextProvider
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ITenantContext _tenantContext;

    public HttpActivityLogContextProvider(IHttpContextAccessor httpContextAccessor, ITenantContext tenantContext)
    {
        _httpContextAccessor = httpContextAccessor;
        _tenantContext = tenantContext;
    }

    public ActivityLogContext GetCurrent()
    {
        var http = _httpContextAccessor.HttpContext;
        if (http is null)
        {
            return new ActivityLogContext(_tenantContext.TenantId, null, null, null, null);
        }

        var correlationId = http.GetCorrelationId();
        var userAgent = http.Request.Headers.UserAgent.ToString();

        return new ActivityLogContext(
            _tenantContext.TenantId,
            http.User.GetUserId(),
            http.Connection.RemoteIpAddress?.ToString(),
            string.IsNullOrWhiteSpace(userAgent) ? null : userAgent,
            string.IsNullOrEmpty(correlationId) ? null : correlationId);
    }
}
