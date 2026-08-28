using FluentValidation;
using HMS.Modules.Masters.Contracts;

namespace HMS.Modules.Masters.Application.Validators;

internal class CreateDiagnosticTestRequestValidator : AbstractValidator<CreateDiagnosticTestRequest>
{
    public CreateDiagnosticTestRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.ServiceType).IsInEnum();
        RuleFor(x => x.Category).MaximumLength(100);
        RuleFor(x => x.Price).GreaterThanOrEqualTo(0).WithMessage("Price cannot be negative.");
        RuleFor(x => x.ReferenceLab).MaximumLength(100);
    }
}

internal class UpdateDiagnosticTestRequestValidator : AbstractValidator<UpdateDiagnosticTestRequest>
{
    public UpdateDiagnosticTestRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.ServiceType).IsInEnum();
        RuleFor(x => x.Category).MaximumLength(100);
        RuleFor(x => x.Price).GreaterThanOrEqualTo(0).WithMessage("Price cannot be negative.");
        RuleFor(x => x.ReferenceLab).MaximumLength(100);
    }
}
