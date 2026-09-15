using FluentValidation;
using HMS.Modules.OpdConsultation.Application;
using HMS.Modules.OpdConsultation.Application.Abstractions;
using HMS.Modules.OpdConsultation.Application.Validators;
using HMS.Modules.OpdConsultation.Contracts;
using HMS.Modules.OpdConsultation.Infrastructure;
using HMS.Modules.OpdConsultation.Infrastructure.Repositories;
using HMS.Shared.Kernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HMS.Modules.OpdConsultation;

/// <summary>
/// Single composition entry point for this module, called once from
/// HMS.Api/Configuration/ModuleRegistration.cs — mirrors every other module's AddXModule.
/// </summary>
public static class OpdConsultationModule
{
    public static IServiceCollection AddOpdConsultationModule(this IServiceCollection services, IConfiguration configuration)
    {
        // HMS Multi-Tenancy Phase C: resolved per-request from ITenantContext — see
        // HMS.Modules.Identity.IdentityModule's identical registration for the full
        // rationale.
        services.AddDbContext<OpdConsultationDbContext>((sp, options) =>
        {
            var tenantContext = sp.GetRequiredService<ITenantContext>();
            if (!tenantContext.IsResolved)
            {
                throw new InvalidOperationException(
                    "OpdConsultationDbContext was resolved without a tenant having been established for this request.");
            }

            options.UseNpgsql(tenantContext.ConnectionString, npgsql =>
            {
                npgsql.MigrationsHistoryTable("__ef_migrations_history", OpdConsultationDbContext.SchemaName);
                npgsql.MigrationsAssembly("HMS.Database.Migrations");
            });
        });

        services.AddScoped<IOpdConsultationRepository, OpdConsultationRepository>();
        services.AddScoped<IOpdConsultationService, OpdConsultationService>();

        // Registered explicitly, not AddValidatorsFromAssemblyContaining — that scanner only
        // finds *public* IValidator<T> implementations, and this module's validators are
        // internal by design (docs/DeveloperHandbook.md §8/§20).
        services.AddScoped<IValidator<SaveOpdConsultationRequest>, SaveOpdConsultationRequestValidator>();

        return services;
    }
}
