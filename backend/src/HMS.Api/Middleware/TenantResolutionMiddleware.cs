using HMS.Modules.Platform.Application.Abstractions;
using HMS.Shared.Kernel;

namespace HMS.Api.Middleware;

/// <summary>
/// HMS Multi-Tenancy Phase C's single runtime tenant-resolution seam. Runs after
/// UseAuthentication() and before MapControllers() (see Program.cs). Handles two cases:
///
/// 1. Every request carrying a Hospital JWT (identified the same way JwtConfiguration's
///    "Hospital" policy does: a "UserId" claim, which only hospital tokens carry — Platform
///    tokens carry "PlatformUserId" instead) — reads the "TenantId" claim and resolves it.
///
/// 2. The hospital login request itself (POST /api/v1/auth/login), which is unauthenticated
///    by definition — no JWT/TenantId claim can exist yet. The caller identifies its tenant
///    via the X-Hospital-Code header instead. This has to happen here, in middleware, and
///    not inside AuthenticationService/AuthenticationController: ASP.NET Core constructs an
///    MVC controller's entire constructor-injection graph (controller → IAuthenticationService
///    → IUserRepository → IdentityDbContext) *before* any action-method code runs, so by the
///    time a controller method's body could call something like ITenantDirectory, the
///    tenant-aware IdentityDbContext has already been constructed — too late. Resolving
///    here, before _next(context) reaches MapControllers(), guarantees ITenantContext is
///    populated before that constructor graph is ever built.
///
/// Both branches populate the same request-scoped ITenantContext that every tenant-aware
/// hospital DbContext reads from. Requests that are neither of the above (Platform tokens,
/// other anonymous requests) pass through untouched — existing [Authorize]/
/// [Authorize(Policy=...)] attributes are still what decides whether the request is allowed
/// at all; this middleware only ever narrows an otherwise-permitted request further, never
/// widens access.
/// </summary>
public sealed class TenantResolutionMiddleware
{
    private const string TenantInvalidErrorCode = "TENANT.INVALID";
    private const string HospitalCodeHeaderName = "X-Hospital-Code";
    private const string LoginPath = "/api/v1/auth/login";

    private readonly RequestDelegate _next;

    public TenantResolutionMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, ITenantContext tenantContext, ITenantDirectory tenantDirectory)
    {
        var user = context.User;
        var isHospitalToken = user.Identity?.IsAuthenticated == true && user.HasClaim(c => c.Type == "UserId");

        if (isHospitalToken)
        {
            var tenantIdClaim = user.FindFirst("TenantId")?.Value;
            if (!Guid.TryParse(tenantIdClaim, out var tenantId))
            {
                // Tokens issued before this phase (or any other token missing the claim)
                // never resolve to a tenant — see Phase C's "existing tokens should not be
                // able to bypass the tenant status check indefinitely" requirement. The
                // caller must log in again to receive a tenant-aware token.
                await WriteRejectionAsync(context, "This token does not carry a valid tenant and can no longer be used. Please log in again.");
                return;
            }

            var tenant = await tenantDirectory.FindByIdAsync(tenantId, context.RequestAborted);
            if (tenant is null || !tenant.IsActive)
            {
                // Re-checked on every request (not cached beyond this one), so disabling a
                // tenant takes effect on that tenant's very next API call even though the
                // JWT itself remains cryptographically valid until it expires.
                await WriteRejectionAsync(context, "This hospital is not available.");
                return;
            }

            tenantContext.SetTenant(tenant.Id, tenant.ConnectionString, tenant.EnabledModules, tenant.EnabledFeatures);
        }
        else if (IsHospitalLoginRequest(context))
        {
            var hospitalCode = context.Request.Headers[HospitalCodeHeaderName].ToString();
            if (string.IsNullOrWhiteSpace(hospitalCode))
            {
                await WriteRejectionAsync(context, $"The '{HospitalCodeHeaderName}' header is required.");
                return;
            }

            var tenant = await tenantDirectory.FindByHospitalCodeAsync(hospitalCode, context.RequestAborted);
            if (tenant is null || !tenant.IsActive)
            {
                // Same generic wording AuthenticationService uses for every other login
                // failure reason — never reveal whether the hospital code itself was wrong
                // versus some other rule, per the existing login-security convention.
                await WriteRejectionAsync(context, "Invalid username or password.");
                return;
            }

            tenantContext.SetTenant(tenant.Id, tenant.ConnectionString, tenant.EnabledModules, tenant.EnabledFeatures);
        }

        await _next(context);
    }

    private static bool IsHospitalLoginRequest(HttpContext context) =>
        HttpMethods.IsPost(context.Request.Method) &&
        context.Request.Path.Equals(LoginPath, StringComparison.OrdinalIgnoreCase);

    private static Task WriteRejectionAsync(HttpContext context, string message)
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        context.Response.ContentType = "application/json";

        var error = new ApiErrorResponse
        {
            ErrorCode = TenantInvalidErrorCode,
            Message = message,
            CorrelationId = context.Items.TryGetValue("X-Correlation-Id", out var id) ? id?.ToString() ?? string.Empty : string.Empty,
            Timestamp = DateTime.UtcNow,
        };

        return context.Response.WriteAsJsonAsync(error);
    }
}
