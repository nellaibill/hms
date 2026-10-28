using FluentValidation;
using HMS.Modules.Billing.Contracts;

namespace HMS.Modules.Billing.Application.Validators;

internal class CreateInvoiceRevertRequestValidator : AbstractValidator<CreateInvoiceRevertRequest>
{
    public CreateInvoiceRevertRequestValidator()
    {
        // Becomes the reverted invoice's VoidReason (max 500), so the same limit applies here.
        RuleFor(x => x.Reason).NotEmpty().WithMessage("A reason is required to request a bill revert.").MaximumLength(500);
    }
}

internal class ApproveInvoiceRevertRequestValidator : AbstractValidator<ApproveInvoiceRevertRequest>
{
    public ApproveInvoiceRevertRequestValidator()
    {
        RuleFor(x => x.Notes).MaximumLength(500);
    }
}

internal class RejectInvoiceRevertRequestValidator : AbstractValidator<RejectInvoiceRevertRequest>
{
    public RejectInvoiceRevertRequestValidator()
    {
        RuleFor(x => x.Notes).NotEmpty().WithMessage("A reason is required to reject a revert request.").MaximumLength(500);
    }
}
