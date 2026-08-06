using FluentValidation;
using HMS.Modules.Patients.Contracts;

namespace HMS.Modules.Patients.Application.Validators;

/// <summary>
/// Server-side validation per docs/PatientRegistrationModule.md §4's validation table.
/// The authoritative check — client-side validation (frontend/shared/validation) is a
/// UX convenience only, per docs/ApiStandards.md §7.
/// </summary>
internal class CreatePatientRequestValidator : AbstractValidator<CreatePatientRequest>
{
    // Requires at least one digit (via lookahead) so a symbol-only string like "----------"
    // — which the character class alone would accept, since '*' permits zero digits — is
    // rejected; still permits the digits/+/-/()/space characters a real phone number uses.
    internal const string PhonePattern = @"^(?=.*[0-9])[0-9+\-() ]*$";

    public CreatePatientRequestValidator()
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

        RuleFor(x => x.PrimaryPhone).NotEmpty().MaximumLength(20).Matches(PhonePattern).WithMessage("Phone number must contain at least one digit and only digits/+/-/()/spaces.");
        RuleFor(x => x.AlternatePhone).MaximumLength(20).Matches(PhonePattern).WithMessage("Phone number must contain at least one digit and only digits/+/-/()/spaces.").When(x => !string.IsNullOrWhiteSpace(x.AlternatePhone));
        RuleFor(x => x.Email).EmailAddress().MaximumLength(256).When(x => !string.IsNullOrWhiteSpace(x.Email));
        RuleFor(x => x.Profession).MaximumLength(100);

        RuleFor(x => x.EmergencyContactRelationship).NotEmpty().MaximumLength(50);
        RuleFor(x => x.EmergencyContactName).NotEmpty().MaximumLength(150);
        RuleFor(x => x.EmergencyContactPhone).NotEmpty().MaximumLength(20).Matches(PhonePattern).WithMessage("Phone number must contain at least one digit and only digits/+/-/()/spaces.");

        RuleFor(x => x.AllergyType).NotEmpty().MaximumLength(200).When(x => x.HasKnownAllergy);
        RuleFor(x => x.AllergySeverity).NotNull().IsInEnum().When(x => x.HasKnownAllergy);

        RuleFor(x => x.Registration).NotNull().SetValidator(new PatientRegistrationDetailsValidator());
    }
}

internal class PatientRegistrationDetailsValidator : AbstractValidator<PatientRegistrationDetails>
{
    public PatientRegistrationDetailsValidator()
    {
        RuleFor(x => x.EncounterType).IsInEnum();
        RuleFor(x => x.ModeOfArrival).IsInEnum();
        RuleFor(x => x.Department).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Consultant).NotEmpty().MaximumLength(200);
        RuleFor(x => x.ReferralSource).MaximumLength(200);
        RuleFor(x => x.Category).MaximumLength(100);

        // Admission Type is required only for IP/Emergency encounters — the one piece of
        // progressive disclosure kept from docs/PatientRegistrationModule.md §5.
        RuleFor(x => x.AdmissionType)
            .NotNull()
            .WithMessage("Admission type (MLC/NMLC) is required for IP and Emergency encounters.")
            .When(x => x.EncounterType is EncounterType.IP or EncounterType.Emergency);
    }
}
