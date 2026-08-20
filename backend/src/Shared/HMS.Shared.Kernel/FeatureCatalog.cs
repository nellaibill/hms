namespace HMS.Shared.Kernel;

/// <summary>
/// Tenant Feature/Module Management's full catalog — distinct from <see cref="ModuleCatalog"/>,
/// which is the 11 RBAC permission categories governing what a user can *do* inside modules
/// the tenant already has. This catalog governs whether a module is available to a tenant at
/// all, and is the single source of truth for the Platform Portal's "Manage Features" screen
/// (which now replaces the old per-tenant RBAC-category "Configure" screen as the one place a
/// Platform Admin controls hospital-wide module availability — see docs/DecisionLog.md).
///
/// Split into two groups:
/// - <see cref="SchemaBacked"/>: the 9 real modules with an actual database schema — only
///   these are ever passed to ITenantMigrationService, so enabling/disabling anything outside
///   this list never provisions or migrates anything (see that interface's own doc comment).
/// - <see cref="UiOnly"/>: modules with a frontend page today but no backend module/schema yet
///   (e.g. OPD, Central Laboratory) — toggling these only controls sidebar/route visibility;
///   there's nothing server-side to provision or enforce for them. Appointments and Billing
///   remain excluded entirely — placeholder projects with no DbContext/migrations and no
///   frontend page either.
/// </summary>
public static class FeatureCatalog
{
    public static readonly IReadOnlyList<string> SchemaBacked =
    [
        "identity",
        "masters",
        "patients",
        "documents",
        "branding",
        "hr",
        "calendar",
        "products",
        "ipd",
    ];

    /// <summary>UI-only — no real backend module/schema behind these yet. Kept as a separate
    /// list (rather than folded silently into <see cref="SchemaBacked"/>) so it's always
    /// obvious from the code, not just tribal knowledge, which keys ITenantMigrationService
    /// actually acts on.</summary>
    public static readonly IReadOnlyList<string> UiOnly =
    [
        "opd",
        "ot",
        "pharmacy",
        "central-laboratory",
        "radiology",
        "blood-bank",
        "ambulance",
        "finance",
        "records-and-certificates",
        "activity-log",
        "messages-and-notifications",
        "reports",
        "e-mrd",
    ];

    public static readonly IReadOnlyList<string> All = [.. SchemaBacked, .. UiOnly];

    /// <summary>Always provisioned/enabled, never toggleable — identity/masters are
    /// foundational dependencies of nearly every other module; documents is a hard
    /// compile-time dependency of patients; patients and branding are core to every existing
    /// tenant.</summary>
    public static readonly IReadOnlyList<string> Mandatory =
    [
        "identity",
        "masters",
        "patients",
        "documents",
        "branding",
    ];

    /// <summary>Platform-admin toggleable per tenant — every catalog key that isn't mandatory,
    /// schema-backed or not.</summary>
    public static readonly IReadOnlyList<string> Optional = [.. All.Where(key => !Mandatory.Contains(key))];
}
