using FluentValidation;
using HMS.Modules.Products.Contracts;

namespace HMS.Modules.Products.Application.Validators;

internal class CreateProductBarcodeRequestValidator : AbstractValidator<CreateProductBarcodeRequest>
{
    public CreateProductBarcodeRequestValidator()
    {
        RuleFor(x => x.BarcodeType).NotEmpty().MaximumLength(20);
        RuleFor(x => x.BarcodeValue).NotEmpty().MaximumLength(100);
    }
}

internal class UpdateProductBarcodeRequestValidator : AbstractValidator<UpdateProductBarcodeRequest>
{
    public UpdateProductBarcodeRequestValidator()
    {
        RuleFor(x => x.BarcodeType).NotEmpty().MaximumLength(20);
    }
}
