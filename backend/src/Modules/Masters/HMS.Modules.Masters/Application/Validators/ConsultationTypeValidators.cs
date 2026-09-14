using FluentValidation;
using HMS.Modules.Masters.Contracts;

namespace HMS.Modules.Masters.Application.Validators;

internal class CreateConsultationTypeRequestValidator : AbstractValidator<CreateConsultationTypeRequest>
{
    public CreateConsultationTypeRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Amount).GreaterThanOrEqualTo(0).When(x => x.Amount.HasValue).WithMessage("Amount cannot be negative.");
    }
}

internal class UpdateConsultationTypeRequestValidator : AbstractValidator<UpdateConsultationTypeRequest>
{
    public UpdateConsultationTypeRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Amount).GreaterThanOrEqualTo(0).When(x => x.Amount.HasValue).WithMessage("Amount cannot be negative.");
    }
}
