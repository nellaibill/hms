using FluentValidation;
using HMS.Modules.Products.Contracts;

namespace HMS.Modules.Products.Application.Validators;

internal class CreateProductBatchRequestValidator : AbstractValidator<CreateProductBatchRequest>
{
    public CreateProductBatchRequestValidator()
    {
        RuleFor(x => x.BatchNo).NotEmpty().MaximumLength(100);
        RuleFor(x => x.ExpiryDate).GreaterThanOrEqualTo(x => x.ManufactureDate);
        RuleFor(x => x.Mrp).GreaterThanOrEqualTo(0);
        RuleFor(x => x.PurchaseRate).GreaterThanOrEqualTo(0);
        RuleFor(x => x.SellingRate).GreaterThanOrEqualTo(0);
        RuleFor(x => x.SellingRate)
            .LessThanOrEqualTo(x => x.Mrp)
            .When(x => x.Mrp.HasValue && x.SellingRate.HasValue)
            .WithMessage("Selling rate cannot be above MRP.");
    }
}

internal class UpdateProductBatchRequestValidator : AbstractValidator<UpdateProductBatchRequest>
{
    public UpdateProductBatchRequestValidator()
    {
        RuleFor(x => x.ExpiryDate).GreaterThanOrEqualTo(x => x.ManufactureDate);
        RuleFor(x => x.Mrp).GreaterThanOrEqualTo(0);
        RuleFor(x => x.PurchaseRate).GreaterThanOrEqualTo(0);
        RuleFor(x => x.SellingRate).GreaterThanOrEqualTo(0);
        RuleFor(x => x.SellingRate)
            .LessThanOrEqualTo(x => x.Mrp)
            .When(x => x.Mrp.HasValue && x.SellingRate.HasValue)
            .WithMessage("Selling rate cannot be above MRP.");
    }
}
