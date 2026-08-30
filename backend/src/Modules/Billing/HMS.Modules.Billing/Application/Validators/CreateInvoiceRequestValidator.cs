using FluentValidation;
using HMS.Modules.Billing.Contracts;

namespace HMS.Modules.Billing.Application.Validators;

internal class CreateInvoiceRequestValidator : AbstractValidator<CreateInvoiceRequest>
{
    public CreateInvoiceRequestValidator()
    {
        RuleFor(x => x.PatientId).NotEmpty();
        RuleFor(x => x.VisitId).NotEmpty();
        RuleFor(x => x.PatientName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.PatientUhid).NotEmpty().MaximumLength(30);

        // Mirrors mockBillingStore.ts's saveBillingForPatient: an invoice with nothing
        // billed isn't a record worth keeping.
        RuleFor(x => x.Items).NotEmpty().WithMessage("At least one billing item is required.");

        RuleForEach(x => x.Items).SetValidator(new CreateInvoiceLineItemRequestValidator());

        // Per-row shape only (Method/Amount/ReferenceNumber) — whether the rows' amounts add
        // up correctly (exactly NetAmount when split across more than one row, at least
        // NetAmount for a single row) depends on the invoice's own computed total, which isn't
        // known until InvoiceService builds it; that cross-field check lives there instead.
        When(x => x.Payments is { Count: > 0 }, () =>
        {
            RuleForEach(x => x.Payments!).SetValidator(new CreateInvoicePaymentRequestValidator());
        });
    }
}

internal class CreateInvoicePaymentRequestValidator : AbstractValidator<CreateInvoicePaymentRequest>
{
    public CreateInvoicePaymentRequestValidator()
    {
        RuleFor(x => x.Method).IsInEnum();
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.ReferenceNumber).MaximumLength(100);
    }
}

internal class CreateInvoiceLineItemRequestValidator : AbstractValidator<CreateInvoiceLineItemRequest>
{
    public CreateInvoiceLineItemRequestValidator()
    {
        RuleFor(x => x.BillingType).IsInEnum();
        RuleFor(x => x.Quantity).GreaterThan(0);
        RuleFor(x => x.UnitPrice).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Discount).GreaterThanOrEqualTo(0);
        RuleFor(x => x.DepartmentId).MaximumLength(100);
        RuleFor(x => x.ConsultantId).MaximumLength(100);
        RuleFor(x => x.ServiceId).MaximumLength(100);
        RuleFor(x => x.DiscountApprovedBy).MaximumLength(150);

        // Mirrors billingValidation.ts's consultationBillingSchema/serviceBillingSchema:
        // "Discount cannot exceed the charge".
        RuleFor(x => x)
            .Must(x => x.Discount <= x.Quantity * x.UnitPrice)
            .WithMessage("Discount cannot exceed the charge.")
            .WithName("Discount");
    }
}

internal class VoidInvoiceRequestValidator : AbstractValidator<VoidInvoiceRequest>
{
    public VoidInvoiceRequestValidator()
    {
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
    }
}
