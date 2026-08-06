using HMS.Modules.Branding;
using HMS.Modules.Calendar;
using HMS.Modules.Documents;
using HMS.Modules.HR;
using HMS.Modules.Identity;
using HMS.Modules.Masters;
using HMS.Modules.Patients;
using HMS.Modules.Products;

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
        // Calendar depends on HR's public service seam (Department reference
        // validation), so it must register after AddHRModule.
        services.AddCalendarModule(configuration);

        // Future modules register here, e.g.:
        // services.AddAppointmentsModule(configuration);

        return services;
    }
}