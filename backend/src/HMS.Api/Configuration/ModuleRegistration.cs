using HMS.Modules.Identity;

namespace HMS.Api.Configuration;

/// <summary>
/// Single composition root for every module. Adding a new module means adding one
/// line here — HMS.Api is the only project allowed to know every module exists.
/// </summary>
public static class ModuleRegistration
{
    public static IServiceCollection AddHmsModules(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddIdentityModule(configuration);

        // Future modules register here, e.g.:
        // services.AddPatientsModule(configuration);
        // services.AddAppointmentsModule(configuration);

        return services;
    }
}
