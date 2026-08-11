using FluentValidation;
using HMS.Modules.IPD.Contracts;

namespace HMS.Modules.IPD.Application.Validators;

internal class CreateAdmissionChargeRequestValidator : AbstractValidator<CreateAdmissionChargeRequest>
{
    public CreateAdmissionChargeRequestValidator()
    {
        RuleFor(x => x.ChargeType).IsInEnum();
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.Remarks).MaximumLength(500);
    }
}
