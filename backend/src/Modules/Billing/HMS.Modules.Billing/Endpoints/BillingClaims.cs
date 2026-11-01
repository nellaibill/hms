using System.Security.Claims;
using HMS.Modules.Billing.Contracts;
using HMS.Shared.Infrastructure;

namespace HMS.Modules.Billing.Endpoints;

/// <summary>
/// Reads who the caller is for Billing's approval rules. "Super Admin" is the caller's login
/// type — the login page only lets a user sign in as superAdmin when their role is the Super
/// Admin role (HMS.Modules.Identity's LoginTypes), the same claim InvoiceRevertRequestsController
/// and OpdConsultationsController read.
/// </summary>
internal static class BillingClaims
{
    private const string SuperAdminLoginType = "superAdmin";

    public static bool IsSuperAdmin(this ClaimsPrincipal user)
        => string.Equals(user.FindFirst("LoginType")?.Value, SuperAdminLoginType, StringComparison.OrdinalIgnoreCase);

    public static string? GetUsername(this ClaimsPrincipal user) => user.FindFirst("Username")?.Value;

    /// <summary>ConsultantId is the caller's own consultant only for a consultant's login
    /// (GetScopedConsultantId) — a Super Admin or receptionist linked to a consultant record
    /// doesn't decide that consultant's discounts as them.</summary>
    public static BillingActor GetBillingActor(this ClaimsPrincipal user)
        => new(user.GetUserId(), user.GetUsername(), user.IsSuperAdmin(), user.GetScopedConsultantId());
}
