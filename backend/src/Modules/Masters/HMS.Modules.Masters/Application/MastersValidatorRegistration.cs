using FluentValidation;
using HMS.Modules.Masters.Application.Validators;
using HMS.Modules.Masters.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace HMS.Modules.Masters.Application;

/// <summary>
/// Validator DI registrations — see <see cref="Infrastructure.MastersRepositoryRegistration"/>
/// for why this is split out. Registered explicitly rather than via
/// AddValidatorsFromAssemblyContaining: that scanner only finds *public* IValidator&lt;T&gt;
/// implementations, and this module's validators are internal by design
/// (docs/DeveloperHandbook.md §8/§20).
/// </summary>
internal static class MastersValidatorRegistration
{
    public static IServiceCollection AddMastersValidators(this IServiceCollection services)
    {
        services.AddScoped<IValidator<CreateCurrencyRequest>, CreateCurrencyRequestValidator>();
        services.AddScoped<IValidator<UpdateCurrencyRequest>, UpdateCurrencyRequestValidator>();
        services.AddScoped<IValidator<CreatePaymentMethodRequest>, CreatePaymentMethodRequestValidator>();
        services.AddScoped<IValidator<UpdatePaymentMethodRequest>, UpdatePaymentMethodRequestValidator>();
        services.AddScoped<IValidator<CreatePaymentTermRequest>, CreatePaymentTermRequestValidator>();
        services.AddScoped<IValidator<UpdatePaymentTermRequest>, UpdatePaymentTermRequestValidator>();
        services.AddScoped<IValidator<CreateStockAdjustmentReasonRequest>, CreateStockAdjustmentReasonRequestValidator>();
        services.AddScoped<IValidator<UpdateStockAdjustmentReasonRequest>, UpdateStockAdjustmentReasonRequestValidator>();
        services.AddScoped<IValidator<CreateBrandRequest>, CreateBrandRequestValidator>();
        services.AddScoped<IValidator<UpdateBrandRequest>, UpdateBrandRequestValidator>();
        services.AddScoped<IValidator<CreateManufacturerRequest>, CreateManufacturerRequestValidator>();
        services.AddScoped<IValidator<UpdateManufacturerRequest>, UpdateManufacturerRequestValidator>();
        services.AddScoped<IValidator<CreateUnitOfMeasureRequest>, CreateUnitOfMeasureRequestValidator>();
        services.AddScoped<IValidator<UpdateUnitOfMeasureRequest>, UpdateUnitOfMeasureRequestValidator>();
        services.AddScoped<IValidator<CreateTaxRequest>, CreateTaxRequestValidator>();
        services.AddScoped<IValidator<UpdateTaxRequest>, UpdateTaxRequestValidator>();
        services.AddScoped<IValidator<CreateWarehouseRequest>, CreateWarehouseRequestValidator>();
        services.AddScoped<IValidator<UpdateWarehouseRequest>, UpdateWarehouseRequestValidator>();
        services.AddScoped<IValidator<CreateProductCategoryRequest>, CreateProductCategoryRequestValidator>();
        services.AddScoped<IValidator<UpdateProductCategoryRequest>, UpdateProductCategoryRequestValidator>();
        services.AddScoped<IValidator<CreateProductSubCategoryRequest>, CreateProductSubCategoryRequestValidator>();
        services.AddScoped<IValidator<UpdateProductSubCategoryRequest>, UpdateProductSubCategoryRequestValidator>();
        services.AddScoped<IValidator<CreateProductGroupRequest>, CreateProductGroupRequestValidator>();
        services.AddScoped<IValidator<UpdateProductGroupRequest>, UpdateProductGroupRequestValidator>();
        services.AddScoped<IValidator<CreateUnitConversionRequest>, CreateUnitConversionRequestValidator>();
        services.AddScoped<IValidator<UpdateUnitConversionRequest>, UpdateUnitConversionRequestValidator>();
        services.AddScoped<IValidator<CreateStorageLocationRequest>, CreateStorageLocationRequestValidator>();
        services.AddScoped<IValidator<UpdateStorageLocationRequest>, UpdateStorageLocationRequestValidator>();
        services.AddScoped<IValidator<CreateSupplierRequest>, CreateSupplierRequestValidator>();
        services.AddScoped<IValidator<UpdateSupplierRequest>, UpdateSupplierRequestValidator>();
        services.AddScoped<IValidator<CreateCustomerRequest>, CreateCustomerRequestValidator>();
        services.AddScoped<IValidator<UpdateCustomerRequest>, UpdateCustomerRequestValidator>();
        services.AddScoped<IValidator<CreateDepartmentRequest>, CreateDepartmentRequestValidator>();
        services.AddScoped<IValidator<UpdateDepartmentRequest>, UpdateDepartmentRequestValidator>();
        services.AddScoped<IValidator<CreateConsultantRequest>, CreateConsultantRequestValidator>();
        services.AddScoped<IValidator<UpdateConsultantRequest>, UpdateConsultantRequestValidator>();

        return services;
    }
}
