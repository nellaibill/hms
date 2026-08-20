using FluentValidation;
using HMS.Modules.Pharmacy.Application;
using HMS.Modules.Pharmacy.Application.Abstractions;
using HMS.Modules.Pharmacy.Application.Validators;
using HMS.Modules.Pharmacy.Contracts;
using HMS.Modules.Pharmacy.Infrastructure;
using HMS.Modules.Pharmacy.Infrastructure.Repositories;
using HMS.Shared.Kernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HMS.Modules.Pharmacy;

public static class PharmacyModule
{
    public static IServiceCollection AddPharmacyModule(this IServiceCollection services, IConfiguration configuration)
    {
        // HMS Multi-Tenancy Phase C: resolved per-request from ITenantContext — see
        // HMS.Modules.Identity.IdentityModule's identical registration for the full
        // rationale (mirrors HMS.Modules.IPD.IPDModule's registration exactly).
        services.AddDbContext<PharmacyDbContext>((sp, options) =>
        {
            var tenantContext = sp.GetRequiredService<ITenantContext>();
            if (!tenantContext.IsResolved)
            {
                throw new InvalidOperationException(
                    "PharmacyDbContext was resolved without a tenant having been established for this request.");
            }

            options.UseNpgsql(tenantContext.ConnectionString, npgsql =>
            {
                npgsql.MigrationsHistoryTable("__ef_migrations_history", PharmacyDbContext.SchemaName);
                npgsql.MigrationsAssembly("HMS.Database.Migrations");
            });
        });

        services.AddScoped<IPharmacyStockBalanceRepository, PharmacyStockBalanceRepository>();
        services.AddScoped<IPharmacyStockTransactionRepository, PharmacyStockTransactionRepository>();

        services.AddScoped<IStockReceiptService, StockReceiptService>();
        services.AddScoped<IDispenseService, DispenseService>();
        services.AddScoped<IStockBalanceService, StockBalanceService>();
        services.AddScoped<IStockLedgerService, StockLedgerService>();

        // Explicit registration, not AddValidatorsFromAssemblyContaining — that scanner only
        // finds *public* IValidator<T> implementations, and this module's validators are
        // internal by design, so it would silently register nothing
        // (docs/DeveloperHandbook.md §8/§20 — the single highest-impact bug hit building the
        // Users module).
        services.AddScoped<IValidator<CreateStockReceiptRequest>, CreateStockReceiptRequestValidator>();
        services.AddScoped<IValidator<CreateDispenseRequest>, CreateDispenseRequestValidator>();
        services.AddScoped<IValidator<CreateDispenseCartRequest>, CreateDispenseCartRequestValidator>();

        return services;
    }
}
