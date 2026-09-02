using FluentValidation;
using HMS.Modules.Laboratory.Application;
using HMS.Modules.Laboratory.Application.Abstractions;
using HMS.Modules.Laboratory.Application.Validators;
using HMS.Modules.Laboratory.Contracts;
using HMS.Modules.Laboratory.Infrastructure;
using HMS.Modules.Laboratory.Infrastructure.Repositories;
using HMS.Shared.Kernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HMS.Modules.Laboratory;

public static class LaboratoryModule
{
    public static IServiceCollection AddLaboratoryModule(this IServiceCollection services, IConfiguration configuration)
    {
        // HMS Multi-Tenancy Phase C: resolved per-request from ITenantContext — see
        // HMS.Modules.Identity.IdentityModule's identical registration for the full
        // rationale.
        services.AddDbContext<LaboratoryDbContext>((sp, options) =>
        {
            var tenantContext = sp.GetRequiredService<ITenantContext>();
            if (!tenantContext.IsResolved)
            {
                throw new InvalidOperationException(
                    "LaboratoryDbContext was resolved without a tenant having been established for this request.");
            }

            options.UseNpgsql(tenantContext.ConnectionString, npgsql =>
            {
                npgsql.MigrationsHistoryTable("__ef_migrations_history", LaboratoryDbContext.SchemaName);
                npgsql.MigrationsAssembly("HMS.Database.Migrations");
            });
        });

        services.AddScoped<ILabOrderRepository, LabOrderRepository>();
        services.AddScoped<ILabOrderNumberGenerator, LabOrderNumberGenerator>();
        services.AddScoped<ILabOrderService, LabOrderService>();

        services.AddScoped<IValidator<CollectSampleRequest>, CollectSampleRequestValidator>();
        services.AddScoped<IValidator<RejectSampleRequest>, RejectSampleRequestValidator>();
        services.AddScoped<IValidator<SaveResultDraftRequest>, SaveResultDraftRequestValidator>();
        services.AddScoped<IValidator<RejectForCorrectionRequest>, RejectForCorrectionRequestValidator>();

        return services;
    }
}
