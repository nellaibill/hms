using FluentValidation;
using HMS.Modules.Patients.Contracts;

namespace HMS.Modules.Patients.Application.Validators;

internal class UpdatePatientRequestValidator : AbstractValidator<UpdatePatientRequest>
{
    public UpdatePatientRequestValidator()
    {
        RuleFor(x => x.Title).IsInEnum();
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100).Matches(CreatePatientRequestValidator.NamePattern).WithMessage("First name must contain letters only.");
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100).Matches(CreatePatientRequestValidator.NamePattern).WithMessage("Last name must contain letters only.");
        RuleFor(x => x.DateOfBirth)
            .NotEmpty()
            .LessThanOrEqualTo(DateOnly.FromDateTime(DateTime.UtcNow)).WithMessage("Date of birth cannot be in the future.")
            .GreaterThanOrEqualTo(CreatePatientRequestValidator.MinDateOfBirth).WithMessage("Date of birth is too far in the past — please check the year.");
        RuleFor(x => x)
            .Must(x => CreatePatientRequestValidator.IsTitleConsistentWithAge(x.Title, x.DateOfBirth))
            .WithName("Title")
            .WithMessage("Title does not match the patient's age (Baby: under 2, Master/Miss: under 18, Mr/Mrs/Ms/Dr: 18 or older).")
            .When(x => Enum.IsDefined(x.Title));
        RuleFor(x => x)
            .Must(x => CreatePatientRequestValidator.IsTitleConsistentWithGender(x.Title, x.Gender))
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
            .Matches(CreatePatientRequestValidator.PhonePattern).WithMessage("Phone number may only contain digits, spaces, and the characters + - ( ).")
            .Must(value => CreatePatientRequestValidator.HasMinimumDigitCount(value, CreatePatientRequestValidator.MinPhoneDigits)).WithMessage($"Phone number must contain at least {CreatePatientRequestValidator.MinPhoneDigits} digits.");
        RuleFor(x => x.PrimaryPhoneRelation).MaximumLength(50).Matches(CreatePatientRequestValidator.NamePattern).WithMessage("Relation must contain letters only.").When(x => !string.IsNullOrWhiteSpace(x.PrimaryPhoneRelation));
        RuleFor(x => x.AlternatePhone)
            .MaximumLength(20)
            .Matches(CreatePatientRequestValidator.PhonePattern).WithMessage("Phone number may only contain digits, spaces, and the characters + - ( ).")
            .Must(value => CreatePatientRequestValidator.HasMinimumDigitCount(value, CreatePatientRequestValidator.MinPhoneDigits)).WithMessage($"Phone number must contain at least {CreatePatientRequestValidator.MinPhoneDigits} digits.")
            .When(x => !string.IsNullOrWhiteSpace(x.AlternatePhone));
        RuleFor(x => x.Email).EmailAddress().MaximumLength(256).When(x => !string.IsNullOrWhiteSpace(x.Email));
        RuleFor(x => x.Profession).MaximumLength(100);

        RuleFor(x => x.EmergencyContactRelationship).NotEmpty().MaximumLength(50).Matches(CreatePatientRequestValidator.NamePattern).WithMessage("Relation must contain letters only.");
        RuleFor(x => x.EmergencyContactName).NotEmpty().MaximumLength(150).Matches(CreatePatientRequestValidator.NamePattern).WithMessage("Name must contain letters only.");
        RuleFor(x => x.EmergencyContactPhone)
            .NotEmpty().MaximumLength(20)
            .Matches(CreatePatientRequestValidator.PhonePattern).WithMessage("Phone number may only contain digits, spaces, and the characters + - ( ).")
            .Must(value => CreatePatientRequestValidator.HasMinimumDigitCount(value, CreatePatientRequestValidator.MinPhoneDigits)).WithMessage($"Phone number must contain at least {CreatePatientRequestValidator.MinPhoneDigits} digits.");

        RuleFor(x => x.AllergyType).NotEmpty().MaximumLength(200).When(x => x.HasKnownAllergy);
        RuleFor(x => x.AllergySeverity).NotNull().IsInEnum().When(x => x.HasKnownAllergy);
    }
}
