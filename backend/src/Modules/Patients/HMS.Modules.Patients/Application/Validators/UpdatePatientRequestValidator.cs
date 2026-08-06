using FluentValidation;
using HMS.Modules.Patients.Contracts;

namespace HMS.Modules.Patients.Application.Validators;

internal class UpdatePatientRequestValidator : AbstractValidator<UpdatePatientRequest>
{
    public UpdatePatientRequestValidator()
    {
        RuleFor(x => x.Title).IsInEnum();
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.DateOfBirth).NotEmpty().LessThanOrEqualTo(DateOnly.FromDateTime(DateTime.UtcNow)).WithMessage("Date of birth cannot be in the future.");
        RuleFor(x => x.Gender).IsInEnum();
        RuleFor(x => x.BloodGroup).IsInEnum().When(x => x.BloodGroup.HasValue);

        RuleFor(x => x.AddressLine1).NotEmpty().MaximumLength(200);
        RuleFor(x => x.District).NotEmpty().MaximumLength(100);
        RuleFor(x => x.State).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Pincode).NotEmpty().Matches(@"^[0-9]{6}$").WithMessage("Pincode must be 6 digits.");

        RuleFor(x => x.PrimaryPhone).NotEmpty().MaximumLength(20).Matches(CreatePatientRequestValidator.PhonePattern).WithMessage("Phone number must contain at least one digit and only digits/+/-/()/spaces.");
        RuleFor(x => x.AlternatePhone).MaximumLength(20).Matches(CreatePatientRequestValidator.PhonePattern).WithMessage("Phone number must contain at least one digit and only digits/+/-/()/spaces.").When(x => !string.IsNullOrWhiteSpace(x.AlternatePhone));
        RuleFor(x => x.Email).EmailAddress().MaximumLength(256).When(x => !string.IsNullOrWhiteSpace(x.Email));
        RuleFor(x => x.Profession).MaximumLength(100);

        RuleFor(x => x.EmergencyContactRelationship).NotEmpty().MaximumLength(50);
        RuleFor(x => x.EmergencyContactName).NotEmpty().MaximumLength(150);
        RuleFor(x => x.EmergencyContactPhone).NotEmpty().MaximumLength(20).Matches(CreatePatientRequestValidator.PhonePattern).WithMessage("Phone number must contain at least one digit and only digits/+/-/()/spaces.");

        RuleFor(x => x.AllergyType).NotEmpty().MaximumLength(200).When(x => x.HasKnownAllergy);
        RuleFor(x => x.AllergySeverity).NotNull().IsInEnum().When(x => x.HasKnownAllergy);
    }
}
