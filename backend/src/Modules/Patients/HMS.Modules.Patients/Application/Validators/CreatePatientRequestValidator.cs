using System.Linq;
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

    // A real phone number (with or without a country code) never has fewer than 10 digits
    // in this system — the app's own seed/test data and UI hints are built around 10-digit
    // Indian mobile numbers. Counted separately from string length so formatting characters
    // like "+91-98765-43210" don't get penalized.
    internal const int MinPhoneDigits = 10;

    // \p{L}/\p{M} (Unicode letter/mark categories) rather than A-Za-z so names in Indian
    // scripts (Devanagari, Tamil, etc.) — not just ASCII — are accepted; still rejects
    // digits and most symbols. Allows spaces/apostrophes/periods/hyphens for names like
    // "Mary-Jane O'Brien" or "Dr. Rao".
    internal const string NamePattern = @"^[\p{L}\p{M}][\p{L}\p{M}\s'.-]*$";

    internal static bool HasMinimumDigitCount(string? value, int minimumDigits)
        => (value ?? string.Empty).Count(char.IsDigit) >= minimumDigits;

    // Generous enough to never reject a real patient (oldest verified humans are ~120) while
    // still catching an obvious data-entry slip like typing "1023" instead of "2023".
    internal static readonly DateOnly MinDateOfBirth = DateOnly.FromDateTime(DateTime.UtcNow).AddYears(-130);

    // Mirrors Patient.cs's domain CalculateAge — kept here (rather than referencing the
    // internal domain type) so validators stay a pure presentation-layer check with no
    // dependency on domain internals.
    internal static int CalculateAge(DateOnly dateOfBirth, DateOnly asOf)
    {
        var age = asOf.Year - dateOfBirth.Year;
        if (dateOfBirth > asOf.AddYears(-age))
        {
            age--;
        }
        return age;
    }

    // Title is a courtesy/age-category title, not a free-text honorific — it must stay
    // consistent with the patient's actual age so the record doesn't read as nonsensical
    // ("Mr" on a 1-day-old). Deliberately not coupled to Gender: title-gender association
    // is a separate, more sensitive judgment call that wasn't asked for here.
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
    // Baby are gender-neutral titles and never flagged. Transgender/NA are deliberately
    // never flagged against any title: unlike the Male/Female mismatches (which are always
    // wrong, e.g. "Ms" + Male), there's no universal convention for how they should pair
    // with a gendered title, so guessing would be presumptuous rather than catching a real error.
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
            Title.Dr or Title.Baby => true,
            _ => true,
        };
    }

    public CreatePatientRequestValidator()
    {
        RuleFor(x => x.Title).IsInEnum();
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100).Matches(NamePattern).WithMessage("First name must contain letters only.");
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100).Matches(NamePattern).WithMessage("Last name must contain letters only.");
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
        RuleFor(x => x.BloodGroup).IsInEnum().When(x => x.BloodGroup.HasValue);

        RuleFor(x => x.AddressLine1).NotEmpty().MaximumLength(200);
        RuleFor(x => x.AddressLine2).MaximumLength(200);
        RuleFor(x => x.AddressLine3).MaximumLength(200);
        RuleFor(x => x.District).NotEmpty().MaximumLength(100);
        RuleFor(x => x.State).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Pincode).NotEmpty().Matches(@"^[0-9]{6}$").WithMessage("Pincode must be 6 digits.");

        RuleFor(x => x.PrimaryPhone)
            .NotEmpty().MaximumLength(20)
            .Matches(PhonePattern).WithMessage("Phone number may only contain digits, spaces, and the characters + - ( ).")
            .Must(value => HasMinimumDigitCount(value, MinPhoneDigits)).WithMessage($"Phone number must contain at least {MinPhoneDigits} digits.");
        RuleFor(x => x.PrimaryPhoneRelation).MaximumLength(50).Matches(NamePattern).WithMessage("Relation must contain letters only.").When(x => !string.IsNullOrWhiteSpace(x.PrimaryPhoneRelation));
        RuleFor(x => x.AlternatePhone)
            .MaximumLength(20)
            .Matches(PhonePattern).WithMessage("Phone number may only contain digits, spaces, and the characters + - ( ).")
            .Must(value => HasMinimumDigitCount(value, MinPhoneDigits)).WithMessage($"Phone number must contain at least {MinPhoneDigits} digits.")
            .When(x => !string.IsNullOrWhiteSpace(x.AlternatePhone));
        RuleFor(x => x.AlternatePhoneRelation).MaximumLength(50).Matches(NamePattern).WithMessage("Relation must contain letters only.").When(x => !string.IsNullOrWhiteSpace(x.AlternatePhoneRelation));
        RuleFor(x => x.AlternatePhone2)
            .MaximumLength(20)
            .Matches(PhonePattern).WithMessage("Phone number may only contain digits, spaces, and the characters + - ( ).")
            .Must(value => HasMinimumDigitCount(value, MinPhoneDigits)).WithMessage($"Phone number must contain at least {MinPhoneDigits} digits.")
            .When(x => !string.IsNullOrWhiteSpace(x.AlternatePhone2));
        RuleFor(x => x.AlternatePhone2Relation).MaximumLength(50).Matches(NamePattern).WithMessage("Relation must contain letters only.").When(x => !string.IsNullOrWhiteSpace(x.AlternatePhone2Relation));
        RuleFor(x => x.Email).EmailAddress().MaximumLength(256).When(x => !string.IsNullOrWhiteSpace(x.Email));
        RuleFor(x => x.Profession).MaximumLength(100);

        RuleFor(x => x.EmergencyContactRelationship).NotEmpty().MaximumLength(50).Matches(NamePattern).WithMessage("Relation must contain letters only.");
        RuleFor(x => x.EmergencyContactName).NotEmpty().MaximumLength(150).Matches(NamePattern).WithMessage("Name must contain letters only.");
        RuleFor(x => x.EmergencyContactPhone)
            .NotEmpty().MaximumLength(20)
            .Matches(PhonePattern).WithMessage("Phone number may only contain digits, spaces, and the characters + - ( ).")
            .Must(value => HasMinimumDigitCount(value, MinPhoneDigits)).WithMessage($"Phone number must contain at least {MinPhoneDigits} digits.");

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
        RuleFor(x => x.DepartmentId).NotEmpty();
        RuleFor(x => x.ConsultantId).NotEmpty();
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
