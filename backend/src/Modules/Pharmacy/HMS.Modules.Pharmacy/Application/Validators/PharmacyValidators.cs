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

internal class DispenseCartLineRequestValidator : AbstractValidator<DispenseCartLineRequest>
{
    public DispenseCartLineRequestValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.ProductBatchId).NotEmpty();
        RuleFor(x => x.Quantity).GreaterThan(0);
        RuleFor(x => x.Remarks).MaximumLength(500);
    }
}

internal class CreateDispenseCartRequestValidator : AbstractValidator<CreateDispenseCartRequest>
{
    public CreateDispenseCartRequestValidator()
    {
        RuleFor(x => x.PatientId).NotEmpty();
        RuleFor(x => x.Lines).NotEmpty().WithMessage("Add at least one item to the cart.");
        RuleForEach(x => x.Lines).SetValidator(new DispenseCartLineRequestValidator());

        // Two rows dispensing the same product/batch are rejected rather than silently merged
        // into one combined quantity — merging would hide what the operator actually typed.
        RuleFor(x => x.Lines)
            .Must(lines => lines.Select(l => (l.ProductId, l.ProductBatchId)).Distinct().Count() == lines.Count)
            .WithMessage("The same product/batch appears more than once in the cart.")
            .When(x => x.Lines.Count > 0);
    }
}
