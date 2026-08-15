using HMS.Modules.Branding.Application;
using HMS.Modules.Branding.Application.Abstractions;
using HMS.Modules.Branding.Infrastructure;
using HMS.Modules.Branding.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HMS.Modules.Branding;

/// <summary>
/// Single composition entry point for this module, called once from
/// HMS.Api/Configuration — mirrors HMS.Modules.Patients.PatientsModule.
/// </summary>
public static class BrandingModule
{
    public static IServiceCollection AddBrandingModule(this IServiceCollection services, IConfiguration configuration)
    {
        // Deliberately NOT made tenant-aware in HMS Multi-Tenancy Phase C, unlike every
        // other hospital module: BrandingController.Get() is anonymous (the pre-login
        // screen themes itself before any JWT/HospitalCode exists), so there is no tenant
        // signal available at all for that call — per-tenant branding needs its own
        // resolution mechanism (a public hospital identifier reachable pre-login), which is
        // explicitly out of scope this phase (see Phase C spec's "tenant configuration...
        // separate phases" carve-out). Authenticated PUT/POST actions here still work, just
        // against this single static database, exactly as before this phase.
        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("Missing 'ConnectionStrings:Default' configuration value.");

        services.AddDbContext<BrandingDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql =>
            {
                npgsql.MigrationsHistoryTable("__ef_migrations_history", BrandingDbContext.SchemaName);

                // Migration classes live in HMS.Database.Migrations (per
                // docs/DatabaseArchitecture.md), not in this module's own assembly.
                npgsql.MigrationsAssembly("HMS.Database.Migrations");
            }));

        services.AddScoped<IBrandingRepository, BrandingRepository>();
        services.AddScoped<IBrandingLogoStorage, BrandingLogoStorage>();
        services.AddScoped<IBrandingService, BrandingService>();

        return services;
    }
}
