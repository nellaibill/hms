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

        // HMS.Modules.DischargeSummary, schema "discharge_summary" — a doctor-authored
        // discharge document (clinical summary, medications, advice) for an already-
        // Discharged IPD admission. See docs/DecisionLog.md.
        "discharge-summary",

        // HMS.Modules.OpdConsultation, schema "opd_consultation" — the clinical note (vitals,
        // diagnosis, investigations, plan) behind the OPD Patient List's "Consult" action.
        // Mandatory (see that list below), not merely Optional, for the same reason "billing"
        // is: the existing "Consult" button unconditionally navigates every tenant with the
        // (UiOnly) "opd" key enabled straight into this module's form now, with no more
        // placeholder fallback — making it optional would mean a tenant could have "opd"
        // enabled but not this, breaking that button outright unless an admin also separately
        // remembered to toggle this on.
        "opd-consultation",

        // HMS.Modules.ActivityLog, schema "activity_log" — the centralized audit trail.
        // Formerly a UiOnly key (nav visibility only); now backed by a real schema and
        // Mandatory (below), since an audit trail a tenant could switch off would defeat its
        // purpose and IActivityLogService is called unconditionally by Patients/Identity/Billing.
        "activity-log",

        // HMS.Modules.Radiology, schema "radiology" — saved AI-assisted reads of a patient's stored
        // X-ray images. Formerly a UiOnly key (sidebar/route visibility only); now backed by a real
        // schema, so toggling it on provisions that schema. Optional, not Mandatory: nothing else
        // depends on it.
        "radiology",
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
        "blood-bank",
        "ambulance",
        "finance",
        "records-and-certificates",
        "reports",
        "e-mrd",

        // Gates HMS.Modules.OpdConsultation's "ai/structure-note" endpoint and the OPD
        // Consultation form's AI dictation panel — turning a transcript into draft narrative
        // note fields via the Anthropic API. UiOnly (not SchemaBacked): nothing is persisted
        // by this feature itself, so there's no schema for ITenantMigrationService to
        // provision — only the endpoint/UI need gating. Optional (not Mandatory) despite
        // living inside the Mandatory "opd-consultation" module: unlike that module's own
        // core note-taking, sending a transcript to a third-party AI provider is a distinct,
        // separately-consentable capability a hospital may not want on by default.
        "opd-ambient-notes",

        // Gates HMS.Modules.DischargeSummary's "ai/draft" endpoint and the Discharge Summary
        // form's "Draft with AI" action — same UiOnly/Optional reasoning as "opd-ambient-notes"
        // above (nothing persisted, sends admission data to a third-party AI provider). Requires
        // "discharge-summary" itself (see Dependencies).
        "discharge-summary-ai-draft",

        // Cloud Sync (HMS.Modules.Sync, ADR-133): the Cloud Sync page and this server sending the
        // hospital's patient changes to the cloud (ADR-145). UiOnly: the outbox schema is plumbing
        // migrated for every tenant whatever this says, and changes keep being saved to it while
        // this is off — they simply wait there until it's switched back on. In OnWithoutRow, so
        // hospitals that existed before this key keep Cloud Sync.
        "cloud-sync",

        // The hospital's Settings area (ADR-145). Mandatory — Settings holds Roles & Permissions,
        // so switching the whole area off would lock a hospital's admins out of the one screen
        // that manages access. It exists as a key so the Settings pages a Platform Admin *can*
        // switch off (Theme & Branding, Backup, …) have a parent in SubFeatures below.
        "settings",
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
        "opd-consultation",
        "activity-log",
        "settings",
    ];

    /// <summary>Platform-admin toggleable per tenant — every catalog key that isn't mandatory,
    /// schema-backed or not.</summary>
    public static readonly IReadOnlyList<string> Optional = [.. All.Where(key => !Mandatory.Contains(key))];

    /// <summary>Optional modules a hospital has unless a row says otherwise — for keys added after
    /// hospitals already had their platform.tenant_features rows, where "no row" would otherwise
    /// switch an existing page off on deploy. Only keys with nothing to provision belong here
    /// (a SchemaBacked key needs its schema migrated before it counts as on).</summary>
    public static readonly IReadOnlyList<string> OnWithoutRow =
    [
        "cloud-sync",
    ];

    /// <summary>Feature key → the other Optional feature keys it functionally requires to be
    /// enabled at the same time, because its module's code calls straight into the
    /// dependency's schema with no safety net. Only real, confirmed runtime dependencies
    /// belong here — a dependency on a Mandatory key never needs an entry, since Mandatory
    /// keys are always present for every tenant.
    ///
    /// Enforced by <c>CreateHospitalRequestValidator</c> (hospital creation) and
    /// <c>UpdateTenantFeaturesRequestValidator</c> (feature toggling), so a tenant can never
    /// be put into a state where one enabled module unconditionally queries another module's
    /// schema that was never provisioned — confirmed live as a real gap: Pharmacy's
    /// DispenseService/StockReceiptService/StockBalanceService call IProductService/
    /// IProductBatchService directly and unconditionally (unlike Billing's deliberately
    /// best-effort, try/catch-wrapped call into Laboratory — see InvoiceService.CreateAsync's
    /// own comment), so enabling "pharmacy" without "products" previously had no guard at
    /// all and would 500 on the first real dispense/stock operation against a
    /// "relation products.products does not exist" error.</summary>
    public static readonly IReadOnlyDictionary<string, IReadOnlyList<string>> Dependencies =
        new Dictionary<string, IReadOnlyList<string>>
        {
            ["pharmacy"] = ["products"],
            ["discharge-summary-ai-draft"] = ["discharge-summary"],
        };

    /// <summary>
    /// The parts of a module a Platform Admin can switch off on their own (ADR-145) — e.g. HR's
    /// Leave Requests, or Theme &amp; Branding's Logo configuration. Each names its parent: a
    /// module key above, or another sub-feature (Theme &amp; Branding's tabs sit under
    /// "settings.branding"). Listed parent before child, which <see cref="ResolveEffective"/>
    /// relies on.
    ///
    /// Unlike modules, a sub-feature is ON unless a platform.tenant_features row says otherwise:
    /// adding one here never hides anything from an existing hospital, and none of them has a
    /// schema to provision (they live inside their parent module's). A sub-feature only takes
    /// effect while its parent chain is on — switching HR off hides every HR part, and switching
    /// it back on restores the parts exactly as they were.
    /// </summary>
    public static readonly IReadOnlyList<SubFeature> SubFeatures =
    [
        // HR & Staffing — Employee Management
        new("hr.dashboard", "hr"),
        new("hr.employees", "hr"),
        new("hr.attendance", "hr"),
        new("hr.leave-requests", "hr"),
        new("hr.leave-types", "hr"),

        // HR & Staffing — Duty Roster
        new("hr.shifts", "hr"),
        new("hr.staff-availability", "hr"),
        new("hr.weekly-rosters", "hr"),
        new("hr.shift-assignments", "hr"),
        new("hr.shift-swap-requests", "hr"),
        new("hr.monthly-calendar", "hr"),

        // Settings. Roles & Permissions is deliberately not here: it can never be switched off
        // (see the "settings" key's own comment).
        new("settings.reference-data", "settings"),
        new("settings.branding", "settings"),
        new("settings.reports-visibility", "settings"),
        new("settings.patient-fields", "settings"),
        new("settings.billing-approvals", "settings"),
        new("settings.backup", "settings"),

        // Theme & Branding — the page's tabs, plus Logo configuration inside the Identity tab.
        new("settings.branding.hospital-info", "settings.branding"),
        new("settings.branding.logo", "settings.branding"),
        new("settings.branding.core-colors", "settings.branding"),
        new("settings.branding.top-bar", "settings.branding"),
        new("settings.branding.left-nav", "settings.branding"),
        new("settings.branding.section-headers", "settings.branding"),
        new("settings.branding.buttons", "settings.branding"),
        new("settings.branding.typography", "settings.branding"),
        new("settings.branding.print", "settings.branding"),

        // Patients — Bulk Patient Import (upload, review, import, and Import History).
        new("patients.bulk-import", "patients"),

        // Cloud Sync — the page's actions. Without these the page still shows the sync status,
        // patients and failed changes, read-only.
        new("cloud-sync.controls", "cloud-sync"),
        new("cloud-sync.job-dashboard", "cloud-sync"),
    ];

    public static readonly IReadOnlyList<string> SubFeatureKeys = [.. SubFeatures.Select(sub => sub.Key)];

    /// <summary>Sub-feature key → the other sub-features it needs, because its pages read their
    /// data (e.g. Attendance picks from the Employees list, a Weekly Roster shows its Shift
    /// Assignments). Kept apart from <see cref="Dependencies"/>, which is about schemas between
    /// modules. Enforced by UpdateTenantFeaturesRequestValidator.</summary>
    public static readonly IReadOnlyDictionary<string, IReadOnlyList<string>> SubFeatureDependencies =
        new Dictionary<string, IReadOnlyList<string>>
        {
            ["hr.attendance"] = ["hr.employees"],
            ["hr.leave-requests"] = ["hr.employees", "hr.leave-types"],
            ["hr.shift-assignments"] = ["hr.shifts"],
            ["hr.weekly-rosters"] = ["hr.shifts", "hr.shift-assignments"],
            ["hr.shift-swap-requests"] = ["hr.shift-assignments"],
            ["hr.monthly-calendar"] = ["hr.weekly-rosters"],
        };

    /// <summary>Every module and sub-feature key — what a tenant with no restrictions has.</summary>
    public static readonly IReadOnlyList<string> AllWithSubFeatures = [.. All, .. SubFeatureKeys];

    /// <summary>The modules a tenant has chosen, from its platform.tenant_features rows. No rows
    /// at all (a tenant provisioned before the table existed) means every module; otherwise a
    /// module needs an enabled row, except an <see cref="OnWithoutRow"/> key, which is on until
    /// a row switches it off. Mandatory modules are always included — a tenant created before a
    /// key became Mandatory has no row for it.</summary>
    public static IReadOnlyList<string> ResolveSelectedModules(IReadOnlyCollection<(string Key, bool IsEnabled)> rows)
    {
        if (rows.Count == 0)
        {
            return All;
        }

        var selected = rows.Where(row => row.IsEnabled && All.Contains(row.Key)).Select(row => row.Key).ToHashSet();
        var withRow = rows.Select(row => row.Key).ToHashSet();
        selected.UnionWith(OnWithoutRow.Where(key => !withRow.Contains(key)));
        selected.UnionWith(Mandatory);
        return [.. All.Where(selected.Contains)];
    }

    /// <summary>The sub-features a tenant has left on, from its rows — ignoring whether their
    /// parent is on (see <see cref="ResolveEffective"/> for that). A sub-feature with no row is on.</summary>
    public static IReadOnlyList<string> ResolveSelectedSubFeatures(IReadOnlyCollection<(string Key, bool IsEnabled)> rows)
    {
        var disabled = rows.Where(row => !row.IsEnabled).Select(row => row.Key).ToHashSet();
        return [.. SubFeatureKeys.Where(key => !disabled.Contains(key))];
    }

    /// <summary>What a tenant can actually use: its modules, plus each selected sub-feature whose
    /// parent chain is on. This is the set ITenantContext.EnabledFeatures carries and
    /// [RequireFeature] checks.</summary>
    public static IReadOnlyList<string> ResolveEffective(IReadOnlyCollection<string> modules, IReadOnlyCollection<string> subFeatures)
    {
        var effective = modules.ToHashSet();
        var selectedSubs = subFeatures.ToHashSet();

        // Parent before child (see SubFeatures), so a parent's own result is known by the time
        // its children are checked.
        foreach (var sub in SubFeatures)
        {
            if (selectedSubs.Contains(sub.Key) && effective.Contains(sub.Parent))
            {
                effective.Add(sub.Key);
            }
        }

        return [.. AllWithSubFeatures.Where(effective.Contains)];
    }

    /// <summary>Shortcut for <see cref="ResolveEffective"/> straight from a tenant's rows.</summary>
    public static IReadOnlyList<string> ResolveEffective(IReadOnlyCollection<(string Key, bool IsEnabled)> rows)
        => ResolveEffective(ResolveSelectedModules(rows), ResolveSelectedSubFeatures(rows));
}

/// <summary>A part of a module that can be switched off on its own — see FeatureCatalog.SubFeatures.</summary>
public sealed record SubFeature(string Key, string Parent);
