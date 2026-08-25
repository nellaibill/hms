using FluentValidation;
using HMS.Modules.Patients.Contracts;
using System.Linq;

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

    // Aadhaar/Passport/VoterId each have one fixed, nationally-standardized format — checkable
    // outright. DrivingLicense genuinely varies too much (state-specific RTO codes, several
    // formats still in circulation from before the current standard) to usefully pattern-match
    // beyond a basic sanity check; Other is a free-text catch-all by definition — neither gets
    // a format regex, only the shared NotEmpty rule below.
    internal const string AadhaarPattern = @"^[0-9]{12}$";
    internal const string AadhaarPatternMessage = "Aadhaar number must be exactly 12 digits.";

    // Indian passport: one letter followed by 7 digits (e.g. A1234567).
    internal const string PassportPattern = @"^[A-Za-z][0-9]{7}$";
    internal const string PassportPatternMessage = "Passport number must be 1 letter followed by 7 digits (e.g. A1234567).";

    // Voter ID / EPIC number: three letters followed by 7 digits (e.g. ABC1234567).
    internal const string VoterIdPattern = @"^[A-Za-z]{3}[0-9]{7}$";
    internal const string VoterIdPatternMessage = "Voter ID number must be 3 letters followed by 7 digits (e.g. ABC1234567).";

    // Not a real format check (see comment above) — just enough to reject an obviously wrong
    // value like "uiop" while accepting the genuine variety of real DL numbers.
    internal const string DrivingLicensePattern = @"^[A-Za-z0-9\s-]{10,20}$";
    internal const string DrivingLicensePatternMessage = "Driving License number must be 10–20 letters/digits.";

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
    // Brackets match the frontend Title dropdown's own displayed guidance exactly ("Baby — up
    // to 1 year", "Master/Miss — 1–18 years") — Master/Miss requires age >= 1 too, not just
    // < 18, so a newborn can't be registered as Master/Miss instead of Baby.
    internal static bool IsTitleConsistentWithAge(Title title, DateOnly dateOfBirth)
    {
        var age = CalculateAge(dateOfBirth, DateOnly.FromDateTime(DateTime.UtcNow));
        return title switch
        {
            Title.Baby => age <= 1,
            Title.Master or Title.Miss => age is >= 1 and < 18,
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

    // A minor's marital status isn't a real-world determination yet — under 18 must be 'NA';
    // 18-or-older must give a real answer (Married or Unmarried), not 'NA'. Same 18 threshold
    // Title already uses for Mr/Mrs/Ms/Dr.
    internal static bool IsMaritalStatusConsistentWithAge(MaritalStatus maritalStatus, DateOnly dateOfBirth)
    {
        var age = CalculateAge(dateOfBirth, DateOnly.FromDateTime(DateTime.UtcNow));
        return age < 18 ? maritalStatus == MaritalStatus.NA : maritalStatus is MaritalStatus.Married or MaritalStatus.Unmarried;
    }

    internal static string MaritalStatusAgeMessage(DateOnly dateOfBirth) =>
        CalculateAge(dateOfBirth, DateOnly.FromDateTime(DateTime.UtcNow)) < 18
            ? "Patients under 18 must have marital status 'N/A'."
            : "Marital status must be Married or Unmarried for patients 18 or older.";

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
            .WithMessage("Title does not match the patient's age (Baby: up to 1 year, Master/Miss: 1–18 years, Mr/Mrs/Ms/Dr: 18 or older).")
            .When(x => Enum.IsDefined(x.Title));
        RuleFor(x => x)
            .Must(x => IsTitleConsistentWithGender(x.Title, x.Gender))
            .WithName("Title")
            .WithMessage("Title does not match the patient's gender (Mr/Master: Male, Mrs/Ms/Miss: Female).")
            .When(x => Enum.IsDefined(x.Title) && Enum.IsDefined(x.Gender));
        RuleFor(x => x.Gender).IsInEnum();
        RuleFor(x => x.BloodGroup).IsInEnum();
        RuleFor(x => x.MaritalStatus).IsInEnum();
        RuleFor(x => x)
            .Must(x => IsMaritalStatusConsistentWithAge(x.MaritalStatus, x.DateOfBirth))
            .WithName("MaritalStatus")
            .WithMessage(x => MaritalStatusAgeMessage(x.DateOfBirth))
            .When(x => Enum.IsDefined(x.MaritalStatus));

        RuleFor(x => x.PrimaryPhone).NotEmpty().Matches(PhonePattern).WithMessage(PhonePatternMessage);
        RuleFor(x => x.SecondaryPhone).Matches(PhonePattern).WithMessage(PhonePatternMessage).When(x => !string.IsNullOrWhiteSpace(x.SecondaryPhone));
        // A "secondary" number identical to the primary isn't a second contact method — almost
        // certainly a copy-paste slip.
        RuleFor(x => x.SecondaryPhone)
            .NotEqual(x => x.PrimaryPhone).WithMessage("Secondary phone must be different from the primary phone.")
            .When(x => !string.IsNullOrWhiteSpace(x.SecondaryPhone));
        RuleFor(x => x.Email).EmailAddress().MaximumLength(256).When(x => !string.IsNullOrWhiteSpace(x.Email));
        RuleFor(x => x.Profession).MaximumLength(100);

        RuleFor(x => x.IdProofNumber)
            .NotEmpty().WithMessage("ID proof number is required when an ID proof type is selected.")
            .When(x => x.IdProofType.HasValue);
        RuleFor(x => x.IdProofNumber)
            .Matches(AadhaarPattern).WithMessage(AadhaarPatternMessage)
            .When(x => x.IdProofType == IdProofType.Aadhaar && !string.IsNullOrWhiteSpace(x.IdProofNumber));
        RuleFor(x => x.IdProofNumber)
            .Matches(PassportPattern).WithMessage(PassportPatternMessage)
            .When(x => x.IdProofType == IdProofType.Passport && !string.IsNullOrWhiteSpace(x.IdProofNumber));
        RuleFor(x => x.IdProofNumber)
            .Matches(VoterIdPattern).WithMessage(VoterIdPatternMessage)
            .When(x => x.IdProofType == IdProofType.VoterId && !string.IsNullOrWhiteSpace(x.IdProofNumber));
        RuleFor(x => x.IdProofNumber)
            .Matches(DrivingLicensePattern).WithMessage(DrivingLicensePatternMessage)
            .When(x => x.IdProofType == IdProofType.DrivingLicense && !string.IsNullOrWhiteSpace(x.IdProofNumber));

        RuleFor(x => x.ModeOfArrivalSource).IsInEnum();
        // Every source has a required detail that belongs here: Online/OfflineAdvertisement's
        // channel, PatientOrRelativeReferral's referral source, DoctorReferral's referring
        // department — so Channel is required for every defined source, not just the two Ad
        // sources (the frontend's arrivalSourceSchema requires the matching UI field for all
        // four, so this always has something to send once the request gets this far).
        RuleFor(x => x.ModeOfArrivalChannel)
            .NotEmpty().WithMessage("Channel is required for this arrival source.")
            .When(x => Enum.IsDefined(x.ModeOfArrivalSource));
        RuleFor(x => x.ModeOfArrivalSpecify)
            .NotEmpty().WithMessage("Please specify.")
            .When(x => !string.IsNullOrWhiteSpace(x.ModeOfArrivalChannel) && x.ModeOfArrivalChannel == "Other");

        RuleFor(x => x.Address).NotNull().SetValidator(new AddressRequestValidator());

        RuleForEach(x => x.Allergies).SetValidator(new AllergyRequestValidator());

        // A patient must have at least one emergency contact — same requirement the old
        // single-field design enforced, carried forward now that it's a repeatable list.
        RuleFor(x => x.EmergencyContacts).NotEmpty().WithMessage("At least one emergency contact is required.");
        RuleForEach(x => x.EmergencyContacts).SetValidator(new EmergencyContactRequestValidator());
        // An emergency contact is supposed to be someone else to call when the patient can't
        // be reached — reusing the patient's own primary phone defeats that purpose and is
        // almost certainly a data-entry mistake, not a deliberate choice.
        RuleFor(x => x.EmergencyContacts)
            .Must((request, contacts) => contacts.All(c => c.Phone != request.PrimaryPhone))
            .WithMessage("An emergency contact's phone number must be different from the patient's own primary phone.")
            .When(x => !string.IsNullOrWhiteSpace(x.PrimaryPhone));
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
