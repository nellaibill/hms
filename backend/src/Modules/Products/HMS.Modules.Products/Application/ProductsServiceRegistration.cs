using Microsoft.Extensions.DependencyInjection;

namespace HMS.Modules.Products.Application;

/// <summary>Service DI registrations — see <see cref="Infrastructure.ProductsRepositoryRegistration"/> for why this is split out.</summary>
internal static class ProductsServiceRegistration
{
    public static IServiceCollection AddProductsServices(this IServiceCollection services)
    {
        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<IProductBarcodeService, ProductBarcodeService>();
        services.AddScoped<IProductBatchService, ProductBatchService>();
        services.AddScoped<IProductPriceService, ProductPriceService>();
        services.AddScoped<IProductImageService, ProductImageService>();
        services.AddScoped<IProductAttributeService, ProductAttributeService>();
        services.AddScoped<IProductAttributeValueService, ProductAttributeValueService>();
        services.AddScoped<IProductTaxMappingService, ProductTaxMappingService>();

        return services;
    }
}
