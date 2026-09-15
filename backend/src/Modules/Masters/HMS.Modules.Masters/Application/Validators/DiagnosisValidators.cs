using FluentValidation;
using HMS.Modules.Masters.Contracts;

namespace HMS.Modules.Masters.Application.Validators;

internal class CreateDiagnosisRequestValidator : AbstractValidator<CreateDiagnosisRequest>
{
    public CreateDiagnosisRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.IcdCode).MaximumLength(20);
    }
}

internal class UpdateDiagnosisRequestValidator : AbstractValidator<UpdateDiagnosisRequest>
{
    public UpdateDiagnosisRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.IcdCode).MaximumLength(20);
    }
}
