namespace HMS.Shared.Kernel;

/// <summary>
/// The 11 business-domain module keys every permission belongs to (see
/// HMS.Modules.Identity.Infrastructure.Seed.PermissionSeedData, which is the actual seed —
/// this is a read-only mirror for code that needs the catalog but cannot depend on the
/// Identity module, e.g. Platform's per-tenant module-enablement store). Kept here rather
/// than duplicated only in Platform because Shared.Kernel has no dependency on any module
/// and both Platform (Tenant.EnabledModules) and, in principle, other future callers need
/// the same list — same reasoning as the frontend's independent ROLE_MODULES mirror
/// (frontend/web/src/features/roles/modules.ts).
/// </summary>
public static class ModuleCatalog
{
    public static readonly IReadOnlyList<string> All =
    [
        "patient-management",
        "clinical-care",
        "diagnostics",
        "pharmacy",
        "support-services",
        "finance-billing",
        "records-compliance",
        "workforce-admin",
        "engagement",
        "reports-analytics",
        "identity-administration",

        // HMS.Modules.DischargeSummary's own RBAC category (discharge-summary.view/create/
        // edit/finalize — see PermissionSeedData). A 12th category, not folded into
        // "clinical-care" (which already owns IPD/Admissions), because Finalize is a
        // genuinely new action outside the view/create/edit/delete set every other category
        // uses — see docs/DecisionLog.md for why this deliberately departs from ADR-022's
        // "reuse an existing category" default.
        "discharge-summary",
    ];
}
