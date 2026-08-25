using FluentValidation;
using HMS.Modules.Patients.Contracts;

namespace HMS.Modules.Patients.Application.Validators;

internal class UpdatePatientRequestValidator : AbstractValidator<UpdatePatientRequest>
{
    public UpdatePatientRequestValidator()
    {
        RuleFor(x => x.Title).IsInEnum();
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100).Matches(CreatePatientRequestValidator.NamePattern).WithMessage(CreatePatientRequestValidator.NamePatternMessage);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100).Matches(CreatePatientRequestValidator.NamePattern).WithMessage(CreatePatientRequestValidator.NamePatternMessage);
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
        RuleFor(x => x.BloodGroup).IsInEnum();
        RuleFor(x => x.MaritalStatus).IsInEnum();

        RuleFor(x => x.PrimaryPhone).NotEmpty().Matches(CreatePatientRequestValidator.PhonePattern).WithMessage(CreatePatientRequestValidator.PhonePatternMessage);
        RuleFor(x => x.SecondaryPhone)
            .Matches(CreatePatientRequestValidator.PhonePattern).WithMessage(CreatePatientRequestValidator.PhonePatternMessage)
            .When(x => !string.IsNullOrWhiteSpace(x.SecondaryPhone));
        RuleFor(x => x.Email).EmailAddress().MaximumLength(256).When(x => !string.IsNullOrWhiteSpace(x.Email));
        RuleFor(x => x.Profession).MaximumLength(100);

        RuleFor(x => x.IdProofNumber).NotEmpty().WithMessage("ID proof number is required when an ID proof type is selected.").When(x => x.IdProofType.HasValue);
        RuleFor(x => x.IdProofNumber)
            .Matches(CreatePatientRequestValidator.AadhaarPattern).WithMessage("Aadhaar number must be exactly 12 digits.")
            .When(x => x.IdProofType == IdProofType.Aadhaar && !string.IsNullOrWhiteSpace(x.IdProofNumber));

        RuleFor(x => x.ModeOfArrivalSource).IsInEnum();
        RuleFor(x => x.ModeOfArrivalChannel)
            .NotEmpty().WithMessage("Channel is required for this arrival source.")
            .When(x => x.ModeOfArrivalSource is ModeOfArrivalSource.OnlineAdvertisement or ModeOfArrivalSource.OfflineAdvertisement);
        RuleFor(x => x.ModeOfArrivalSpecify)
            .NotEmpty().WithMessage("Please specify.")
            .When(x => !string.IsNullOrWhiteSpace(x.ModeOfArrivalChannel) && x.ModeOfArrivalChannel == "Other");

        RuleFor(x => x.Address).NotNull().SetValidator(new AddressRequestValidator());
        RuleFor(x => x.RowVersion).NotEmpty();
    }
}
