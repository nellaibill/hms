using HMS.Modules.Patients.Application;
using HMS.Shared.Infrastructure;
using HMS.Shared.Kernel;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;

namespace HMS.Modules.Sync.Infrastructure;

/// <summary>
/// Decision 1 of ADR-133, Cloud server only: once a hospital is cut over to its local server
/// (<c>sync-cutover &lt;code&gt; on</c>), the local server is the system of record for patients, so
/// the cloud refuses patient registration, edits, deletes, allergy/contact changes and bulk import
/// for that hospital (409). Reads are unaffected. This is what keeps UHIDs from colliding and makes
/// sync conflicts rare. The sync receiver writes patients through IPatientSyncService, not through
/// these controllers, so it is never blocked. Registered as a global MVC filter only when
/// Sync:Role is Cloud, so other servers never run it.
/// </summary>
internal sealed class CloudPatientWriteGuardFilter : IAsyncActionFilter
{
    public const string ErrorCode = "PATIENTS.MANAGED_BY_LOCAL_SERVER";

    private static readonly TimeSpan CacheDuration = TimeSpan.FromSeconds(30);
    private static readonly HashSet<string> GuardedControllers = new(StringComparer.Ordinal) { "Patients", "PatientImport" };

    private readonly ITenantContext _tenantContext;
    private readonly IMemoryCache _cache;

    public CloudPatientWriteGuardFilter(ITenantContext tenantContext, IMemoryCache cache)
    {
        _tenantContext = tenantContext;
        _cache = cache;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (!IsPatientWrite(context) || _tenantContext.TenantId is not { } tenantId)
        {
            await next();
            return;
        }

        var ownedByEdge = await _cache.GetOrCreateAsync($"sync:patients-owned-by-edge:{tenantId}", async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = CacheDuration;
            var registry = context.HttpContext.RequestServices.GetRequiredService<SyncRegistryDbContext>();
            return await registry.TenantSettings.AsNoTracking().AnyAsync(s => s.TenantId == tenantId && s.PatientsOwnedByEdge, context.HttpContext.RequestAborted);
        });

        if (!ownedByEdge)
        {
            await next();
            return;
        }

        context.Result = new ObjectResult(new ApiErrorResponse
        {
            ErrorCode = ErrorCode,
            Message = "This hospital's patient records are managed on its local server. Make this change there; it reaches the cloud automatically.",
            CorrelationId = context.HttpContext.GetCorrelationId(),
            Timestamp = DateTime.UtcNow,
        })
        {
            StatusCode = StatusCodes.Status409Conflict,
        };
    }

    private static bool IsPatientWrite(ActionExecutingContext context) =>
        !HttpMethods.IsGet(context.HttpContext.Request.Method)
        && !HttpMethods.IsHead(context.HttpContext.Request.Method)
        && !HttpMethods.IsOptions(context.HttpContext.Request.Method)
        && context.ActionDescriptor is ControllerActionDescriptor action
        && action.ControllerTypeInfo.Assembly == typeof(IPatientService).Assembly
        && GuardedControllers.Contains(action.ControllerName);
}
