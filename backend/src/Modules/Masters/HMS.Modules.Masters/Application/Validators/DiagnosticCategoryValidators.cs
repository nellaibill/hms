using FluentValidation;
using HMS.Modules.Masters.Contracts;

namespace HMS.Modules.Masters.Application.Validators;

internal class CreateDiagnosticCategoryRequestValidator : AbstractValidator<CreateDiagnosticCategoryRequest>
{
    public CreateDiagnosticCategoryRequestValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Description).MaximumLength(500);
    }
}

internal class UpdateDiagnosticCategoryRequestValidator : AbstractValidator<UpdateDiagnosticCategoryRequest>
{
    public UpdateDiagnosticCategoryRequestValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Description).MaximumLength(500);
    }
}
