using FluentValidation;
using HMS.Modules.Patients.Contracts;

namespace HMS.Modules.Patients.Application.Validators;

internal class AddAllergyRequestValidator : AbstractValidator<AddAllergyRequest>
{
    public AddAllergyRequestValidator()
    {
        RuleFor(x => x.AllergyType).IsInEnum();
        RuleFor(x => x.Specify).MaximumLength(200);
        RuleFor(x => x.Severity).IsInEnum();
    }
}
