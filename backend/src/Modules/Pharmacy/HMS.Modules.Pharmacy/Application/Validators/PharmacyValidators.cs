using FluentValidation;
using HMS.Modules.Pharmacy.Contracts;

namespace HMS.Modules.Pharmacy.Application.Validators;

internal class CreateStockReceiptRequestValidator : AbstractValidator<CreateStockReceiptRequest>
{
    public CreateStockReceiptRequestValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.ProductBatchId).NotEmpty();
        RuleFor(x => x.Quantity).GreaterThan(0);
        RuleFor(x => x.Remarks).MaximumLength(500);
    }
}

internal class CreateDispenseRequestValidator : AbstractValidator<CreateDispenseRequest>
{
    public CreateDispenseRequestValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.ProductBatchId).NotEmpty();
        RuleFor(x => x.PatientId).NotEmpty();
        RuleFor(x => x.Quantity).GreaterThan(0);
        RuleFor(x => x.Remarks).MaximumLength(500);
    }
}
