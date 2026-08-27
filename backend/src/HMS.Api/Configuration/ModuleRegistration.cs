using HMS.Api.Provisioning;
using HMS.Modules.Billing;
using HMS.Modules.Branding;
using HMS.Modules.Calendar;
using HMS.Modules.Documents;
using HMS.Modules.HR;
using HMS.Modules.Identity;
using HMS.Modules.IPD;
using HMS.Modules.Masters;
using HMS.Modules.Messaging;
using HMS.Modules.Notifications;
using HMS.Modules.Patients;
using HMS.Modules.Pharmacy;
using HMS.Modules.Platform;
using HMS.Modules.Platform.Application.Abstractions;
using HMS.Modules.Products;
using HMS.Shared.Kernel;

namespace HMS.Api.Configuration;

/// <summary>
/// Single composition root for every module. Adding a new module means adding one
/// line here — HMS.Api is the only project allowed to know every module exists.
/// </summary>
public static class ModuleRegistration
{
    public static IServiceCollection AddHmsModules(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // HMS Multi-Tenancy Phase C's request-scoped tenant seam — registered once, here,
        // before any module (every tenant-aware hospital DbContext's registration reads it
        // via sp.GetRequiredService<ITenantContext>()). Scoped, not singleton: a fresh
        // instance per request/DI-scope is what keeps tenant selection request-safe.
        services.AddScoped<ITenantContext, TenantContext>();

        services.AddIdentityModule(configuration);
        // Documents registers before Patients only for readability here — DI registration
        // order doesn't affect resolution; Patients' PatientDocumentOwnerExistenceChecker
        // registration works the same regardless of which AddXxxModule call runs first.
        services.AddDocumentsModule(configuration);
        services.AddPatientsModule(configuration);
        services.AddBrandingModule(configuration);
        services.AddMastersModule(configuration);
        // Products depends on Masters' public service seam (classification/unit reference
        // validation), so it must register after AddMastersModule.
        services.AddProductsModule(configuration);
        services.AddHRModule(configuration);
        // Calendar depends on Masters' public service seam (Department reference
        // validation, now consolidated there — see docs/DecisionLog.md), so it must
        // register after AddMastersModule (already satisfied above).
        services.AddCalendarModule(configuration);
        // IPD depends on Masters' (Department/Consultant) and Patients' public service
        // seams for cross-module admission reference validation, so it must register
        // after both.
        services.AddIPDModule(configuration);

        // Billing depends on Patients' (PatientId existence) and Masters' (Department/
        // Consultant existence) public service seams for invoice-creation validation, so it
        // must register after both — same reasoning as IPD above.
        services.AddBillingModule(configuration);

        // Pharmacy depends on Products' (Product/ProductBatch existence), Patients'
        // (PatientId existence), and now Billing's IInvoiceService (best-effort dispense
        // billing — ADR-028) public service seams, so it must register after all three —
        // same reasoning as IPD above.
        services.AddPharmacyModule(configuration);

        // Notifications and Messaging have no dependency on any other module's public
        // service seam yet (Phase 1 is DbContext + repositories only — see each module's
        // own AddXModule doc comment), so registration order relative to the modules above
        // doesn't matter; placed here because a later phase's INotificationService will be
        // the thing Appointments/Patients/Billing/Pharmacy/IPD call into, so it's read
        // naturally as "the last thing every other module depends on."
        services.AddNotificationsModule(configuration);
        services.AddMessagingModule(configuration);

        // Platform is deliberately last and self-contained: it owns a separate physical
        // database (hms_platform via ConnectionStrings:Platform), not another schema in the
        // shared hospital database, and has no dependency on any hospital-facing module's
        // public service seam — see docs/DecisionLog.md's SaaS provisioning ADR.
        services.AddPlatformModule(configuration);

        // Implements Platform's ITenantMigrationService/ITenantProvisioner seams here (not
        // inside AddPlatformModule) because both need every module's DbContext type — see
        // each interface's own doc comment. ITenantMigrationService is registered first:
        // TenantProvisioningService now depends on it (reused for a new tenant's initial
        // migrate step, not duplicated).
        services.AddScoped<ITenantMigrationService, TenantMigrationService>();
        services.AddScoped<ITenantProvisioner, TenantProvisioningService>();

        // Future modules register here, e.g.:
        // services.AddAppointmentsModule(configuration);

        return services;
    }
}