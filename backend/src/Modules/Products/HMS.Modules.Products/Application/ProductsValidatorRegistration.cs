using FluentValidation;
using HMS.Modules.Products.Application.Validators;
using HMS.Modules.Products.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace HMS.Modules.Products.Application;

/// <summary>
/// Validator DI registrations — see <see cref="Infrastructure.ProductsRepositoryRegistration"/>
/// for why this is split out. Registered explicitly rather than via
/// AddValidatorsFromAssemblyContaining: that scanner only finds *public* IValidator&lt;T&gt;
/// implementations, and this module's validators are internal by design.
/// </summary>
internal static class ProductsValidatorRegistration
{
    public static IServiceCollection AddProductsValidators(this IServiceCollection services)
    {
        services.AddScoped<IValidator<CreateProductRequest>, CreateProductRequestValidator>();
        services.AddScoped<IValidator<UpdateProductRequest>, UpdateProductRequestValidator>();
        services.AddScoped<IValidator<CreateProductBarcodeRequest>, CreateProductBarcodeRequestValidator>();
        services.AddScoped<IValidator<UpdateProductBarcodeRequest>, UpdateProductBarcodeRequestValidator>();
        services.AddScoped<IValidator<CreateProductBatchRequest>, CreateProductBatchRequestValidator>();
        services.AddScoped<IValidator<UpdateProductBatchRequest>, UpdateProductBatchRequestValidator>();
        services.AddScoped<IValidator<CreateProductPriceRequest>, CreateProductPriceRequestValidator>();
        services.AddScoped<IValidator<UpdateProductPriceRequest>, UpdateProductPriceRequestValidator>();
        services.AddScoped<IValidator<UpdateProductImageRequest>, UpdateProductImageRequestValidator>();
        services.AddScoped<IValidator<CreateProductAttributeRequest>, CreateProductAttributeRequestValidator>();
        services.AddScoped<IValidator<UpdateProductAttributeRequest>, UpdateProductAttributeRequestValidator>();
        services.AddScoped<IValidator<CreateProductAttributeValueRequest>, CreateProductAttributeValueRequestValidator>();
        services.AddScoped<IValidator<UpdateProductAttributeValueRequest>, UpdateProductAttributeValueRequestValidator>();
        services.AddScoped<IValidator<CreateProductTaxMappingRequest>, CreateProductTaxMappingRequestValidator>();
        services.AddScoped<IValidator<UpdateProductTaxMappingRequest>, UpdateProductTaxMappingRequestValidator>();

        return services;
    }
}
