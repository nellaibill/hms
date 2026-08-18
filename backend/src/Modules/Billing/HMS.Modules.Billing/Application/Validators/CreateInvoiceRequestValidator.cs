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
