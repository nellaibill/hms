using Microsoft.Extensions.DependencyInjection;

namespace HMS.Modules.Masters.Application;

/// <summary>Service DI registrations — see <see cref="Infrastructure.MastersRepositoryRegistration"/> for why this is split out.</summary>
internal static class MastersServiceRegistration
{
    public static IServiceCollection AddMastersServices(this IServiceCollection services)
    {
        services.AddScoped<ICurrencyService, CurrencyService>();
        services.AddScoped<IPaymentMethodService, PaymentMethodService>();
        services.AddScoped<IPaymentTermService, PaymentTermService>();
        services.AddScoped<IStockAdjustmentReasonService, StockAdjustmentReasonService>();
        services.AddScoped<IBrandService, BrandService>();
        services.AddScoped<IManufacturerService, ManufacturerService>();
        services.AddScoped<IUnitOfMeasureService, UnitOfMeasureService>();
        services.AddScoped<ITaxService, TaxService>();
        services.AddScoped<IWarehouseService, WarehouseService>();
        services.AddScoped<IProductCategoryService, ProductCategoryService>();
        services.AddScoped<IProductSubCategoryService, ProductSubCategoryService>();
        services.AddScoped<IProductGroupService, ProductGroupService>();
        services.AddScoped<IUnitConversionService, UnitConversionService>();
        services.AddScoped<IStorageLocationService, StorageLocationService>();
        services.AddScoped<ISupplierService, SupplierService>();
        services.AddScoped<ICustomerService, CustomerService>();
        services.AddScoped<IDepartmentService, DepartmentService>();
        services.AddScoped<IConsultantService, ConsultantService>();

        return services;
    }
}
