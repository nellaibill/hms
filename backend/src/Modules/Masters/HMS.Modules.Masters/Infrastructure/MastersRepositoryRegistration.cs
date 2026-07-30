using HMS.Modules.Masters.Application.Abstractions;
using HMS.Modules.Masters.Infrastructure.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace HMS.Modules.Masters.Infrastructure;

/// <summary>
/// Repository DI registrations, split out of <see cref="MastersModule"/> only because this
/// module has far more entities (~16, per docs/03_Masters_ERD) than any other — one line
/// per entity here keeps MastersModule.AddMastersModule itself short. Every other module
/// registers its (1-2) repositories inline; this split is Masters-specific.
/// </summary>
internal static class MastersRepositoryRegistration
{
    public static IServiceCollection AddMastersRepositories(this IServiceCollection services)
    {
        services.AddScoped<ICurrencyRepository, CurrencyRepository>();
        services.AddScoped<IPaymentMethodRepository, PaymentMethodRepository>();
        services.AddScoped<IPaymentTermRepository, PaymentTermRepository>();
        services.AddScoped<IStockAdjustmentReasonRepository, StockAdjustmentReasonRepository>();
        services.AddScoped<IBrandRepository, BrandRepository>();
        services.AddScoped<IManufacturerRepository, ManufacturerRepository>();
        services.AddScoped<IUnitOfMeasureRepository, UnitOfMeasureRepository>();
        services.AddScoped<ITaxRepository, TaxRepository>();
        services.AddScoped<IWarehouseRepository, WarehouseRepository>();
        services.AddScoped<IProductCategoryRepository, ProductCategoryRepository>();
        services.AddScoped<IProductSubCategoryRepository, ProductSubCategoryRepository>();
        services.AddScoped<IProductGroupRepository, ProductGroupRepository>();
        services.AddScoped<IUnitConversionRepository, UnitConversionRepository>();
        services.AddScoped<IStorageLocationRepository, StorageLocationRepository>();
        services.AddScoped<ISupplierRepository, SupplierRepository>();
        services.AddScoped<ICustomerRepository, CustomerRepository>();

        return services;
    }
}
