using HMS.Modules.ActivityLog.Application;
using HMS.Modules.ActivityLog.Application.Abstractions;
using HMS.Modules.ActivityLog.Infrastructure;
using HMS.Modules.ActivityLog.Infrastructure.Repositories;
using HMS.Shared.Kernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HMS.Modules.ActivityLog;

/// <summary>
/// Single composition entry point for this module, called once from
/// HMS.Api/Configuration/ModuleRegistration.cs — mirrors every other module's AddXModule.
/// </summary>
public static class ActivityLogModule
{
    public static IServiceCollection AddActivityLogModule(this IServiceCollection services, IConfiguration configuration)
    {
        // Tenant-aware, resolved per-request from ITenantContext — see MessagingModule's
        // identical registration for the full rationale.
        services.AddDbContext<ActivityLogDbContext>((sp, options) =>
        {
            var tenantContext = sp.GetRequiredService<ITenantContext>();
            if (!tenantContext.IsResolved)
            {
                throw new InvalidOperationException(
                    "ActivityLogDbContext was resolved without a tenant having been established for this request.");
            }

            options.UseNpgsql(tenantContext.ConnectionString, npgsql =>
            {
                npgsql.MigrationsHistoryTable("__ef_migrations_history", ActivityLogDbContext.SchemaName);
                npgsql.MigrationsAssembly("HMS.Database.Migrations");
            });
        });

        // Idempotent; supplies the caller's IP/User-Agent/user id to the context provider.
        services.AddHttpContextAccessor();
        services.AddScoped<IActivityLogContextProvider, HttpActivityLogContextProvider>();

        services.AddScoped<IActivityLogRepository, ActivityLogRepository>();
        services.AddScoped<IActivityLogService, ActivityLogService>();
        services.AddScoped<IActivityLogQueryService, ActivityLogQueryService>();

        return services;
    }
}
