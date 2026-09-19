using FluentValidation;
using HMS.Modules.DischargeSummary.Application;
using HMS.Modules.DischargeSummary.Application.Abstractions;
using HMS.Modules.DischargeSummary.Application.Validators;
using HMS.Modules.DischargeSummary.Contracts;
using HMS.Modules.DischargeSummary.Infrastructure;
using HMS.Modules.DischargeSummary.Infrastructure.Repositories;
using HMS.Shared.Infrastructure.Ai;
using HMS.Shared.Kernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HMS.Modules.DischargeSummary;

/// <summary>
/// Single composition entry point for this module, called once from
/// HMS.Api/Configuration/ModuleRegistration.cs — mirrors every other module's AddXModule.
/// </summary>
public static class DischargeSummaryModule
{
    public static IServiceCollection AddDischargeSummaryModule(this IServiceCollection services, IConfiguration configuration)
    {
        // HMS Multi-Tenancy Phase C: resolved per-request from ITenantContext — see
        // HMS.Modules.Identity.IdentityModule's identical registration for the full
        // rationale.
        services.AddDbContext<DischargeSummaryDbContext>((sp, options) =>
        {
            var tenantContext = sp.GetRequiredService<ITenantContext>();
            if (!tenantContext.IsResolved)
            {
                throw new InvalidOperationException(
                    "DischargeSummaryDbContext was resolved without a tenant having been established for this request.");
            }

            options.UseNpgsql(tenantContext.ConnectionString, npgsql =>
            {
                npgsql.MigrationsHistoryTable("__ef_migrations_history", DischargeSummaryDbContext.SchemaName);
                npgsql.MigrationsAssembly("HMS.Database.Migrations");
            });
        });

        services.AddScoped<IDischargeSummaryRepository, DischargeSummaryRepository>();
        services.AddScoped<IDischargeSummaryService, DischargeSummaryService>();

        // Provider (Claude/OpenAI) is chosen once at startup from Ai:Provider — see AddHmsAiExtractor.
        services.AddHmsAiExtractor(configuration);
        services.AddScoped<IDischargeSummaryAiDraftService, DischargeSummaryAiDraftService>();

        // Registered explicitly, not AddValidatorsFromAssemblyContaining — that scanner only
        // finds *public* IValidator<T> implementations, and this module's validators are
        // internal by design (docs/DeveloperHandbook.md §8/§20).
        services.AddScoped<IValidator<UpdateDischargeSummaryRequest>, UpdateDischargeSummaryRequestValidator>();
        services.AddScoped<IValidator<FinalizeDischargeSummaryRequest>, FinalizeDischargeSummaryRequestValidator>();

        return services;
    }
}
