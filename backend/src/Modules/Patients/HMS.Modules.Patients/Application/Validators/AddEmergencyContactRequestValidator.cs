using FluentValidation;
using HMS.Modules.Patients.Contracts;

namespace HMS.Modules.Patients.Application.Validators;

internal class AddEmergencyContactRequestValidator : AbstractValidator<AddEmergencyContactRequest>
{
    public AddEmergencyContactRequestValidator()
    {
        RuleFor(x => x.Relationship).IsInEnum();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150).Matches(CreatePatientRequestValidator.NamePattern).WithMessage(CreatePatientRequestValidator.NamePatternMessage);
        RuleFor(x => x.Phone).NotEmpty().Matches(CreatePatientRequestValidator.PhonePattern).WithMessage(CreatePatientRequestValidator.PhonePatternMessage);
    }
}
