using FluentValidation;
using HMS.Modules.Masters.Contracts;

namespace HMS.Modules.Masters.Application.Validators;

internal class CreateConsultantRequestValidator : AbstractValidator<CreateConsultantRequest>
{
    public CreateConsultantRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Specialization).MaximumLength(150);
        RuleFor(x => x.Priority).GreaterThanOrEqualTo(1).When(x => x.Priority.HasValue)
            .WithMessage("Priority must be 1 or greater.");
    }
}

internal class UpdateConsultantRequestValidator : AbstractValidator<UpdateConsultantRequest>
{
    public UpdateConsultantRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Specialization).MaximumLength(150);
        RuleFor(x => x.Priority).GreaterThanOrEqualTo(1).When(x => x.Priority.HasValue)
            .WithMessage("Priority must be 1 or greater.");
    }
}
