using FluentValidation;
using HMS.Modules.Masters.Contracts;

namespace HMS.Modules.Masters.Application.Validators;

internal class CreateDiagnosticProviderRequestValidator : AbstractValidator<CreateDiagnosticProviderRequest>
{
    public CreateDiagnosticProviderRequestValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.ContactDetails).MaximumLength(500);
    }
}

internal class UpdateDiagnosticProviderRequestValidator : AbstractValidator<UpdateDiagnosticProviderRequest>
{
    public UpdateDiagnosticProviderRequestValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.ContactDetails).MaximumLength(500);
    }
}
