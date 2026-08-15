using HMS.Modules.Products.Application;
using HMS.Modules.Products.Application.Abstractions;
using HMS.Modules.Products.Infrastructure;
using HMS.Shared.Kernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HMS.Modules.Products;

/// <summary>
/// Single composition entry point for this module, called once from
/// HMS.Api/Configuration — mirrors HMS.Modules.Masters.MastersModule. Product Management
/// depends on the Masters module's public service seam (registered by AddMastersModule) for
/// classification/unit reference validation — HMS.Api's ModuleRegistration must call
/// AddMastersModule before AddProductsModule.
/// </summary>
public static class ProductsModule
{
    public static IServiceCollection AddProductsModule(this IServiceCollection services, IConfiguration configuration)
    {
        // HMS Multi-Tenancy Phase C: resolved per-request from ITenantContext — see
        // HMS.Modules.Identity.IdentityModule's identical registration for the full
        // rationale.
        services.AddDbContext<ProductsDbContext>((sp, options) =>
        {
            var tenantContext = sp.GetRequiredService<ITenantContext>();
            if (!tenantContext.IsResolved)
            {
                throw new InvalidOperationException(
                    "ProductsDbContext was resolved without a tenant having been established for this request.");
            }

            options.UseNpgsql(tenantContext.ConnectionString, npgsql =>
            {
                npgsql.MigrationsHistoryTable("__ef_migrations_history", ProductsDbContext.SchemaName);

                // Migration classes live in HMS.Database.Migrations (per
                // docs/DatabaseArchitecture.md), not in this module's own assembly.
                npgsql.MigrationsAssembly("HMS.Database.Migrations");
            });
        });

        services.AddProductsRepositories();
        services.AddScoped<IProductImageStorage, ProductImageStorage>();
        services.AddProductsServices();
        services.AddProductsValidators();

        return services;
    }
}
