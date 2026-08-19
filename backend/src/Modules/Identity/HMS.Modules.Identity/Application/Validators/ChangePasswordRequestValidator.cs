using FluentValidation;
using HMS.Modules.Identity.Contracts;
using HMS.Shared.Kernel;

namespace HMS.Modules.Identity.Application.Validators;

/// <summary>
/// Malformed-request checks only — "current password doesn't match" is a business-rule
/// failure reported as a Result failure by AuthenticationService, not here. Matches
/// CreateHospitalRequestValidator/SetPasswordRequestValidator's split.
/// </summary>
internal class ChangePasswordRequestValidator : AbstractValidator<ChangePasswordRequest>
{
    public ChangePasswordRequestValidator()
    {
        RuleFor(x => x.CurrentPassword).NotEmpty().WithMessage("Current password is required.");

        RuleFor(x => x.NewPassword)
            .NotEmpty()
            .WithMessage("New password is required.")
            .MinimumLength(PasswordPolicy.MinimumLength)
            .WithMessage($"New password must be at least {PasswordPolicy.MinimumLength} characters.")
            .Matches(PasswordPolicy.ComplexityRegex)
            .WithMessage(PasswordPolicy.ComplexityMessage);

        RuleFor(x => x.NewPassword)
            .NotEqual(x => x.CurrentPassword)
            .WithMessage("New password must be different from the current password.");
    }
}
