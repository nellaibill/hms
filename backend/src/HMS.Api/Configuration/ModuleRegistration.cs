using HMS.Api.Provisioning;
using HMS.Modules.Branding;
using HMS.Modules.Calendar;
using HMS.Modules.Documents;
using HMS.Modules.HR;
using HMS.Modules.Identity;
using HMS.Modules.IPD;
using HMS.Modules.Masters;
using HMS.Modules.Patients;
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