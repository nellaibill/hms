using FluentValidation;
using HMS.Modules.HR.Contracts;

namespace HMS.Modules.HR.Application.Validators;

internal class CreateLeaveTypeRequestValidator : AbstractValidator<CreateLeaveTypeRequest>
{
    public CreateLeaveTypeRequestValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(30);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.MaxDaysPerYear).GreaterThan(0).When(x => x.MaxDaysPerYear.HasValue);
    }
}

internal class UpdateLeaveTypeRequestValidator : AbstractValidator<UpdateLeaveTypeRequest>
{
    public UpdateLeaveTypeRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.MaxDaysPerYear).GreaterThan(0).When(x => x.MaxDaysPerYear.HasValue);
    }
}
