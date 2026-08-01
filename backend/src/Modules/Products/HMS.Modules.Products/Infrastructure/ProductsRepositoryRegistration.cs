using HMS.Modules.Products.Application.Abstractions;
using HMS.Modules.Products.Infrastructure.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace HMS.Modules.Products.Infrastructure;

/// <summary>
/// Repository DI registrations, split out of <see cref="ProductsModule"/> — mirrors
/// HMS.Modules.Masters.Infrastructure.MastersRepositoryRegistration (this module has 8
/// entities, enough to keep ProductsModule.AddProductsModule itself short).
/// </summary>
internal static class ProductsRepositoryRegistration
{
    public static IServiceCollection AddProductsRepositories(this IServiceCollection services)
    {
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IProductBarcodeRepository, ProductBarcodeRepository>();
        services.AddScoped<IProductBatchRepository, ProductBatchRepository>();
        services.AddScoped<IProductPriceRepository, ProductPriceRepository>();
        services.AddScoped<IProductImageRepository, ProductImageRepository>();
        services.AddScoped<IProductAttributeRepository, ProductAttributeRepository>();
        services.AddScoped<IProductAttributeValueRepository, ProductAttributeValueRepository>();
        services.AddScoped<IProductTaxMappingRepository, ProductTaxMappingRepository>();

        return services;
    }
}
