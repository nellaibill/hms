using FluentValidation;
using HMS.Modules.Patients.Contracts;

namespace HMS.Modules.Patients.Application.Validators;

/// <summary>
/// Server-side validation — the authoritative check. Client-side validation
/// (frontend/shared/validation) is a UX convenience only.
/// </summary>
internal class CreatePatientRequestValidator : AbstractValidator<CreatePatientRequest>
{
    // Phone numbers in this system are always exactly 10 digits — no country code, no
    // formatting characters (per explicit product decision: store the raw 10-digit number
    // only, no +91 prefix).
    internal const string PhonePattern = @"^[0-9]{10}$";
    internal const string PhonePatternMessage = "Phone number must be exactly 10 digits.";

    // \p{L}/\p{M} (Unicode letter/mark categories) rather than A-Za-z so names in Indian
    // scripts (Devanagari, Tamil, etc.) are accepted; still rejects digits and most symbols.
    // Allows spaces/apostrophes/periods/hyphens for names like "Mary-Jane O'Brien".
    internal const string NamePattern = @"^[\p{L}\p{M}][\p{L}\p{M}\s'.-]*$";
    internal const string NamePatternMessage = "Must contain letters only.";

    internal const string PincodePattern = @"^[0-9]{6}$";

    // Aadhaar is the one ID proof type with a fixed, checkable format (12 digits) — the
    // others (Passport/DrivingLicense/VoterId/Other) vary too much by issuing state/country
    // to usefully pattern-match here.
    internal const string AadhaarPattern = @"^[0-9]{12}$";

    // Generous enough to never reject a real patient (oldest verified humans are ~120) while
    // still catching an obvious data-entry slip like typing "1023" instead of "2023".
    internal static readonly DateOnly MinDateOfBirth = DateOnly.FromDateTime(DateTime.UtcNow).AddYears(-130);

    internal static int CalculateAge(DateOnly dateOfBirth, DateOnly asOf)
    {
        var age = asOf.Year - dateOfBirth.Year;
        if (dateOfBirth > asOf.AddYears(-age))
        {
            age--;
        }
        return age;
    }

    // Title is an age-category, not a free-text honorific — it must stay consistent with the
    // patient's actual age. Deliberately not coupled to Gender: a separate, more sensitive check.
    internal static bool IsTitleConsistentWithAge(Title title, DateOnly dateOfBirth)
    {
        var age = CalculateAge(dateOfBirth, DateOnly.FromDateTime(DateTime.UtcNow));
        return title switch
        {
            Title.Baby => age < 2,
            Title.Master or Title.Miss => age < 18,
            Title.Mr or Title.Mrs or Title.Ms or Title.Dr => age >= 18,
            _ => true,
        };
    }

    // Mr/Master are conventionally masculine, Mrs/Ms/Miss conventionally feminine — Dr and
    // Baby are gender-neutral and never flagged. Transgender/NA are never flagged against any
    // title: there's no universal convention to check them against.
    internal static bool IsTitleConsistentWithGender(Title title, Gender gender)
    {
        if (gender is Gender.Transgender or Gender.NA)
        {
            return true;
        }
        return title switch
        {
            Title.Mr or Title.Master => gender == Gender.Male,
            Title.Mrs or Title.Ms or Title.Miss => gender == Gender.Female,
            _ => true,
        };
    }

    public CreatePatientRequestValidator()
    {
        RuleFor(x => x.Title).IsInEnum();
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100).Matches(NamePattern).WithMessage(NamePatternMessage);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100).Matches(NamePattern).WithMessage(NamePatternMessage);
        RuleFor(x => x.DateOfBirth)
            .NotEmpty()
            .LessThanOrEqualTo(DateOnly.FromDateTime(DateTime.UtcNow)).WithMessage("Date of birth cannot be in the future.")
            .GreaterThanOrEqualTo(MinDateOfBirth).WithMessage("Date of birth is too far in the past — please check the year.");
        RuleFor(x => x)
            .Must(x => IsTitleConsistentWithAge(x.Title, x.DateOfBirth))
            .WithName("Title")
            .WithMessage("Title does not match the patient's age (Baby: under 2, Master/Miss: under 18, Mr/Mrs/Ms/Dr: 18 or older).")
            .When(x => Enum.IsDefined(x.Title));
        RuleFor(x => x)
            .Must(x => IsTitleConsistentWithGender(x.Title, x.Gender))
            .WithName("Title")
            .WithMessage("Title does not match the patient's gender (Mr/Master: Male, Mrs/Ms/Miss: Female).")
            .When(x => Enum.IsDefined(x.Title) && Enum.IsDefined(x.Gender));
        RuleFor(x => x.Gender).IsInEnum();
        RuleFor(x => x.BloodGroup).IsInEnum();
        RuleFor(x => x.MaritalStatus).IsInEnum();

        RuleFor(x => x.PrimaryPhone).NotEmpty().Matches(PhonePattern).WithMessage(PhonePatternMessage);
        RuleFor(x => x.SecondaryPhone).Matches(PhonePattern).WithMessage(PhonePatternMessage).When(x => !string.IsNullOrWhiteSpace(x.SecondaryPhone));
        RuleFor(x => x.Email).EmailAddress().MaximumLength(256).When(x => !string.IsNullOrWhiteSpace(x.Email));
        RuleFor(x => x.Profession).MaximumLength(100);

        RuleFor(x => x.IdProofNumber)
            .NotEmpty().WithMessage("ID proof number is required when an ID proof type is selected.")
            .When(x => x.IdProofType.HasValue);
        RuleFor(x => x.IdProofNumber)
            .Matches(AadhaarPattern).WithMessage("Aadhaar number must be exactly 12 digits.")
            .When(x => x.IdProofType == IdProofType.Aadhaar && !string.IsNullOrWhiteSpace(x.IdProofNumber));

        RuleFor(x => x.ModeOfArrivalSource).IsInEnum();
        RuleFor(x => x.ModeOfArrivalChannel)
            .NotEmpty().WithMessage("Channel is required for this arrival source.")
            .When(x => x.ModeOfArrivalSource is ModeOfArrivalSource.OnlineAdvertisement or ModeOfArrivalSource.OfflineAdvertisement);
        RuleFor(x => x.ModeOfArrivalSpecify)
            .NotEmpty().WithMessage("Please specify.")
            .When(x => !string.IsNullOrWhiteSpace(x.ModeOfArrivalChannel) && x.ModeOfArrivalChannel == "Other");

        RuleFor(x => x.Address).NotNull().SetValidator(new AddressRequestValidator());

        RuleForEach(x => x.Allergies).SetValidator(new AllergyRequestValidator());

        // A patient must have at least one emergency contact — same requirement the old
        // single-field design enforced, carried forward now that it's a repeatable list.
        RuleFor(x => x.EmergencyContacts).NotEmpty().WithMessage("At least one emergency contact is required.");
        RuleForEach(x => x.EmergencyContacts).SetValidator(new EmergencyContactRequestValidator());
    }
}

internal class AddressRequestValidator : AbstractValidator<AddressRequest>
{
    public AddressRequestValidator()
    {
        RuleFor(x => x.AddressLine1).NotEmpty().MaximumLength(200);
        RuleFor(x => x.AddressLine2).MaximumLength(200);
        RuleFor(x => x.AddressLine3).MaximumLength(200);
        RuleFor(x => x.StateId).NotEmpty();
        RuleFor(x => x.DistrictId).NotEmpty();
        RuleFor(x => x.Pincode).NotEmpty().Matches(CreatePatientRequestValidator.PincodePattern).WithMessage("Pincode must be 6 digits.");
    }
}

internal class AllergyRequestValidator : AbstractValidator<AllergyRequest>
{
    public AllergyRequestValidator()
    {
        RuleFor(x => x.AllergyType).IsInEnum();
        RuleFor(x => x.Specify).MaximumLength(200);
        RuleFor(x => x.Severity).IsInEnum();
    }
}

internal class EmergencyContactRequestValidator : AbstractValidator<EmergencyContactRequest>
{
    public EmergencyContactRequestValidator()
    {
        RuleFor(x => x.Relationship).IsInEnum();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150).Matches(CreatePatientRequestValidator.NamePattern).WithMessage(CreatePatientRequestValidator.NamePatternMessage);
        RuleFor(x => x.Phone).NotEmpty().Matches(CreatePatientRequestValidator.PhonePattern).WithMessage(CreatePatientRequestValidator.PhonePatternMessage);
    }
}
