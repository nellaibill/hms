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
/// - <see cref="SchemaBacked"/>: the 11 real modules with an actual database schema — only
///   these are ever passed to ITenantMigrationService, so enabling/disabling anything outside
///   this list never provisions or migrates anything (see that interface's own doc comment).
/// - <see cref="UiOnly"/>: modules with a frontend page today but no backend module/schema yet
///   (e.g. OPD, Central Laboratory) — toggling these only controls sidebar/route visibility;
///   there's nothing server-side to provision or enforce for them. Appointments remains
///   excluded entirely — a placeholder project with no DbContext/migrations and no frontend
///   page either. "finance" (UiOnly, below) is a distinct, broader nav-level key for the
///   Accounts/Finance section and is NOT the same thing as "billing" here — Billing
///   (HMS.Modules.Billing, schema "billing") is the real Invoice/InvoiceLineItem module,
///   already used unconditionally by Patient Registration's own Billing step and by every
///   Pharmacy dispense (ADR-028) regardless of any per-tenant toggle, which is exactly why
///   it's Mandatory rather than merely SchemaBacked below — see that list's own doc comment.
///   (Discovered missing here — and so never migrated for any tenant, this tenant included —
///   during Pharmacy billing's live regression test; see docs/DecisionLog.md ADR-028.)
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
        "pharmacy",
        "billing",

        // The real backend workflow module (HMS.Modules.Laboratory, schema "laboratory") —
        // sample collection through result entry, verification, and report generation/
        // release. Deliberately a distinct key from "central-laboratory" below: that one is
        // the pre-existing UiOnly key gating Masters' diagnostics-admin frontend pages (test/
        // package/tariff catalog management), a separate concern from this workflow module,
        // which only ever references Masters' DiagnosticService/DiagnosticPackage by Guid.
        // Both keys can be enabled independently; a tenant needs "central-laboratory" for the
        // admin catalog pages and "laboratory" for the actual order/worklist backend.
        "laboratory",

        // Gates two DbContexts (HMS.Modules.Notifications' "notifications" schema and
        // HMS.Modules.Messaging's "messaging" schema) behind one toggle — presented to a
        // Platform Admin as a single feature since they're one product surface
        // (/engagement/messages), same as how Mandatory below already groups multiple
        // schemas under one always-on umbrella. See docs/DecisionLog.md ADR-035.
        "messages-and-notifications",
    ];

    /// <summary>UI-only — no real backend module/schema behind these yet. Kept as a separate
    /// list (rather than folded silently into <see cref="SchemaBacked"/>) so it's always
    /// obvious from the code, not just tribal knowledge, which keys ITenantMigrationService
    /// actually acts on.</summary>
    public static readonly IReadOnlyList<string> UiOnly =
    [
        "opd",
        "ot",

        // Superseded/paired, not replaced, by the new SchemaBacked "laboratory" key above:
        // this one still legitimately gates Masters' pre-existing diagnostics-admin frontend
        // pages (DiagnosticService/DiagnosticPackage/DiagnosticCategory/DiagnosticProvider
        // catalog management) — a separate concern from the new workflow module, so it stays
        // here rather than being renamed or removed.
        "central-laboratory",
        "radiology",
        "blood-bank",
        "ambulance",
        "finance",
        "records-and-certificates",
        "activity-log",
        "reports",
        "e-mrd",
    ];

    public static readonly IReadOnlyList<string> All = [.. SchemaBacked, .. UiOnly];

    /// <summary>Always provisioned/enabled, never toggleable — identity/masters are
    /// foundational dependencies of nearly every other module; documents is a hard
    /// compile-time dependency of patients; patients and branding are core to every existing
    /// tenant; billing is used unconditionally by Patient Registration's own Billing step and
    /// by every Pharmacy dispense (ADR-028) — there is no code path that treats it as
    /// optional, so it can't be a togglable Optional entry the way a genuinely
    /// business-domain module like Pharmacy or IPD is.</summary>
    public static readonly IReadOnlyList<string> Mandatory =
    [
        "identity",
        "masters",
        "patients",
        "documents",
        "branding",
        "billing",
    ];

    /// <summary>Platform-admin toggleable per tenant — every catalog key that isn't mandatory,
    /// schema-backed or not.</summary>
    public static readonly IReadOnlyList<string> Optional = [.. All.Where(key => !Mandatory.Contains(key))];
}
