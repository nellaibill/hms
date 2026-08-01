using FluentValidation;
using HMS.Modules.Products.Contracts;

namespace HMS.Modules.Products.Application.Validators;

internal class CreateProductTaxMappingRequestValidator : AbstractValidator<CreateProductTaxMappingRequest>
{
    public CreateProductTaxMappingRequestValidator()
    {
        RuleFor(x => x.TaxId).NotEmpty();
        RuleFor(x => x.TaxType).NotEmpty().MaximumLength(20);
    }
}

internal class UpdateProductTaxMappingRequestValidator : AbstractValidator<UpdateProductTaxMappingRequest>
{
}
