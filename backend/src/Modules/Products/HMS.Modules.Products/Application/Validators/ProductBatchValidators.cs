using FluentValidation;
using HMS.Modules.Products.Contracts;

namespace HMS.Modules.Products.Application.Validators;

internal class CreateProductBatchRequestValidator : AbstractValidator<CreateProductBatchRequest>
{
    public CreateProductBatchRequestValidator()
    {
        RuleFor(x => x.BatchNo).NotEmpty().MaximumLength(100);
        RuleFor(x => x.ExpiryDate).GreaterThanOrEqualTo(x => x.ManufactureDate);
    }
}

internal class UpdateProductBatchRequestValidator : AbstractValidator<UpdateProductBatchRequest>
{
    public UpdateProductBatchRequestValidator()
    {
        RuleFor(x => x.ExpiryDate).GreaterThanOrEqualTo(x => x.ManufactureDate);
    }
}
