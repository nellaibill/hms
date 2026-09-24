using HMS.Modules.Radiology.Application;
using HMS.Modules.Radiology.Application.Abstractions;
using HMS.Modules.Radiology.Infrastructure;
using HMS.Modules.Radiology.Infrastructure.Repositories;
using HMS.Shared.Kernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HMS.Modules.Radiology;

/// <summary>Single composition entry point for this module, called once from HMS.Api/Configuration.
/// Images are read through Documents' public seam; this module's own database only holds the saved
/// AI analyses.</summary>
public static class RadiologyModule
{
    public static IServiceCollection AddRadiologyModule(this IServiceCollection services, IConfiguration configuration)
    {
        // HMS Multi-Tenancy Phase C: resolved per-request from ITenantContext — see
        // HMS.Modules.Identity.IdentityModule's identical registration for the full rationale.
        services.AddDbContext<RadiologyDbContext>((sp, options) =>
        {
            var tenantContext = sp.GetRequiredService<ITenantContext>();
            if (!tenantContext.IsResolved)
            {
                throw new InvalidOperationException(
                    "RadiologyDbContext was resolved without a tenant having been established for this request.");
            }

            options.UseNpgsql(tenantContext.ConnectionString, npgsql =>
            {
                npgsql.MigrationsHistoryTable("__ef_migrations_history", RadiologyDbContext.SchemaName);
                npgsql.MigrationsAssembly("HMS.Database.Migrations");
            });
        });

        // Timeout is enforced per call from Ai:Radiology:TimeoutSeconds, not by HttpClient's fixed 100s.
        services.AddHttpClient<IXrayImageAnalyzer, OpenAiCompatibleXrayImageAnalyzer>(client => client.Timeout = Timeout.InfiniteTimeSpan);
        services.AddScoped<IXrayAiAnalysisRepository, XrayAiAnalysisRepository>();
        services.AddScoped<IRadiologyAiService, RadiologyAiService>();

        return services;
    }
}
