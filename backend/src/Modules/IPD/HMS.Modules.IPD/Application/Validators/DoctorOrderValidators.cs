using FluentValidation;
using HMS.Modules.IPD.Contracts;

namespace HMS.Modules.IPD.Application.Validators;

internal class CreateDoctorOrderRequestValidator : AbstractValidator<CreateDoctorOrderRequest>
{
    public CreateDoctorOrderRequestValidator()
    {
        RuleFor(x => x.OrderType).IsInEnum();
        RuleFor(x => x.Description).NotEmpty().MaximumLength(1000);
        RuleFor(x => x.Instructions).MaximumLength(2000);
        RuleFor(x => x.OrderedAt).NotEqual(default(DateTime));
    }
}

internal class CancelDoctorOrderRequestValidator : AbstractValidator<CancelDoctorOrderRequest>
{
    public CancelDoctorOrderRequestValidator()
    {
        RuleFor(x => x.Reason).MaximumLength(1000);
    }
}
