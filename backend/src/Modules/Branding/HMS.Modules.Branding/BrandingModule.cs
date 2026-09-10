using HMS.Modules.Branding.Application;
using HMS.Modules.Branding.Application.Abstractions;
using HMS.Modules.Branding.Infrastructure;
using HMS.Modules.Branding.Infrastructure.Repositories;
using HMS.Shared.Kernel;
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
        // Now tenant-aware like every other hospital module (see IdentityModule's identical
        // registration for the full rationale) — reversing the original Phase C decision to
        // keep this one module on a single shared ConnectionStrings:Default database. That
        // decision assumed the anonymous pre-login screen needed a tenant-independent
        // branding source; in practice it meant every tenant read/wrote the same singleton
        // row, so one hospital's saved colors/logo leaked into every other hospital's app.
        // The pre-login screen now themes itself with the static frontend defaults
        // (config/branding.ts) instead — see BrandingController.Get()'s updated doc comment.
        services.AddDbContext<BrandingDbContext>((sp, options) =>
        {
            var tenantContext = sp.GetRequiredService<ITenantContext>();
            if (!tenantContext.IsResolved)
            {
                throw new InvalidOperationException(
                    "BrandingDbContext was resolved without a tenant having been established for this request.");
            }

            options.UseNpgsql(tenantContext.ConnectionString, npgsql =>
            {
                npgsql.MigrationsHistoryTable("__ef_migrations_history", BrandingDbContext.SchemaName);

                // Migration classes live in HMS.Database.Migrations (per
                // docs/DatabaseArchitecture.md), not in this module's own assembly.
                npgsql.MigrationsAssembly("HMS.Database.Migrations");
            });
        });

        services.AddScoped<IBrandingRepository, BrandingRepository>();
        services.AddScoped<IBrandingLogoStorage, BrandingLogoStorage>();
        services.AddScoped<IBrandingService, BrandingService>();

        return services;
    }
}
