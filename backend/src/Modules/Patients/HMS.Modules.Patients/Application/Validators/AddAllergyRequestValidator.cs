using FluentValidation;
using HMS.Modules.Patients.Contracts;

namespace HMS.Modules.Patients.Application.Validators;

internal class AddAllergyRequestValidator : AbstractValidator<AddAllergyRequest>
{
    public AddAllergyRequestValidator()
    {
        RuleFor(x => x.AllergyType).IsInEnum();
        // The allergen is what clinicians act on (regression report REG-01).
        RuleFor(x => x.Specify).NotEmpty().WithMessage("Specify what the patient is allergic to (e.g. Penicillin).").MaximumLength(200);
        RuleFor(x => x.Severity).IsInEnum();
    }
}
