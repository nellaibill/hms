using FluentValidation;
using HMS.Modules.Billing.Contracts;

namespace HMS.Modules.Billing.Application.Validators;

internal class CreateInvoiceDiscountRequestValidator : AbstractValidator<CreateInvoiceDiscountRequest>
{
    public CreateInvoiceDiscountRequestValidator()
    {
        RuleFor(x => x.Reason).NotEmpty().WithMessage("A reason is required to request a discount.").MaximumLength(500);
        RuleFor(x => x.DiscountType).IsInEnum();
        RuleFor(x => x.DiscountValue).GreaterThan(0);
        RuleFor(x => x.DiscountValue).LessThanOrEqualTo(100).When(x => x.DiscountType == InvoiceDiscountType.Percent).WithMessage("A percentage discount can't be more than 100%.");
        RuleFor(x => x.Lines).NotEmpty().WithMessage("Pick at least one bill line to discount.");
        RuleFor(x => x.Lines)
            .Must(lines => lines.Select(l => l.LineItemId).Distinct().Count() == lines.Count)
            .WithMessage("A bill line can only appear once in a discount request.");
        RuleForEach(x => x.Lines).ChildRules(line =>
        {
            line.RuleFor(l => l.LineItemId).NotEmpty();
            line.RuleFor(l => l.Discount).GreaterThan(0);
        });
    }
}

internal class ApproveInvoiceDiscountRequestValidator : AbstractValidator<ApproveInvoiceDiscountRequest>
{
    public ApproveInvoiceDiscountRequestValidator()
    {
        RuleFor(x => x.Notes).MaximumLength(500);
    }
}

internal class RejectInvoiceDiscountRequestValidator : AbstractValidator<RejectInvoiceDiscountRequest>
{
    public RejectInvoiceDiscountRequestValidator()
    {
        RuleFor(x => x.Notes).NotEmpty().WithMessage("A reason is required to reject a discount request.").MaximumLength(500);
    }
}

internal class PayInvoiceRequestValidator : AbstractValidator<PayInvoiceRequest>
{
    public PayInvoiceRequestValidator()
    {
        // Per-row shape only — whether the rows cover what's owed is InvoiceService's check.
        RuleForEach(x => x.Payments).SetValidator(new CreateInvoicePaymentRequestValidator());
    }
}
