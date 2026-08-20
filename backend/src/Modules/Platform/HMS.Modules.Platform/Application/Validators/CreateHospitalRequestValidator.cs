using FluentValidation;
using HMS.Modules.Platform.Contracts;
using HMS.Shared.Kernel;

namespace HMS.Modules.Platform.Application.Validators;

/// <summary>
/// Malformed-request checks only — duplicate hospital code/admin email are business-rule
/// failures reported as a Result failure by HospitalRegistrationService, not here. Matches
/// HMS.Modules.Identity.Application.Validators.CreateUserRequestValidator's phone-number
/// convention (exactly 10 digits).
/// </summary>
internal class CreateHospitalRequestValidator : AbstractValidator<CreateHospitalRequest>
{
    public CreateHospitalRequestValidator()
    {
        RuleFor(x => x.HospitalName).NotEmpty().WithMessage("Hospital name is required.").MaximumLength(200);
        RuleFor(x => x.HospitalCode)
            .NotEmpty()
            .WithMessage("Hospital code is required.")
            .MaximumLength(100)
            .Matches(@"^[a-zA-Z0-9._-]+$")
            .WithMessage("Hospital code may contain only letters, numbers, dots, underscores and hyphens.");
        RuleFor(x => x.MobileNumber)
            .NotEmpty()
            .WithMessage("Mobile number is required.")
            .Length(10)
            .WithMessage("Mobile number must be exactly 10 digits.")
            .Matches(@"^\d+$")
            .WithMessage("Mobile number can contain digits only.");
        RuleFor(x => x.Address).NotEmpty().WithMessage("Address is required.").MaximumLength(500);
        RuleFor(x => x.City).NotEmpty().WithMessage("City is required.").MaximumLength(100);
        RuleFor(x => x.State).NotEmpty().WithMessage("State is required.").MaximumLength(100);
        RuleFor(x => x.Pincode)
            .NotEmpty()
            .WithMessage("Pincode is required.")
            .Length(6)
            .WithMessage("Pincode must be exactly 6 digits.")
            .Matches(@"^\d+$")
            .WithMessage("Pincode can contain digits only.");

        RuleFor(x => x.SuperAdminUsername)
            .NotEmpty()
            .WithMessage("Super Admin username is required.")
            .MaximumLength(100)
            .Matches(@"^[a-zA-Z0-9._-]+$")
            .WithMessage("Super Admin username may contain only letters, numbers, dots, underscores and hyphens.");
        RuleFor(x => x.SuperAdminFirstName).NotEmpty().WithMessage("Super Admin first name is required.").MaximumLength(100);
        RuleFor(x => x.SuperAdminLastName).NotEmpty().WithMessage("Super Admin last name is required.").MaximumLength(100);
        RuleFor(x => x.SuperAdminEmail).NotEmpty().WithMessage("Super Admin email is required.").EmailAddress().WithMessage("Super Admin email is not valid.").MaximumLength(256);
        RuleFor(x => x.SuperAdminPhoneNumber)
            .NotEmpty()
            .WithMessage("Super Admin phone number is required.")
            .Length(10)
            .WithMessage("Super Admin phone number must be exactly 10 digits.")
            .Matches(@"^\d+$")
            .WithMessage("Super Admin phone number can contain digits only.");
        RuleFor(x => x.SuperAdminPassword)
            .NotEmpty()
            .WithMessage("Super Admin password is required.")
            .MinimumLength(PasswordPolicy.MinimumLength)
            .WithMessage($"Super Admin password must be at least {PasswordPolicy.MinimumLength} characters.")
            .Matches(PasswordPolicy.ComplexityRegex)
            .WithMessage(PasswordPolicy.ComplexityMessage);

        RuleForEach(x => x.EnabledFeatureKeys)
            .Must(key => FeatureCatalog.All.Contains(key))
            .WithMessage("One or more feature keys are not recognized.");
    }
}
