using FluentValidation;
using HMS.Modules.Platform.Contracts;

namespace HMS.Modules.Platform.Application.Validators;

/// <summary>
/// Malformed-request checks only — a credentials-didn't-match failure is reported as a 401
/// Result failure by PlatformAuthenticationService, not here. Matches
/// HMS.Modules.Identity.Application.Validators.LoginRequestValidator's split.
/// </summary>
internal class PlatformLoginRequestValidator : AbstractValidator<PlatformLoginRequest>
{
    public PlatformLoginRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().WithMessage("Email is required.").EmailAddress().WithMessage("Email is not valid.");
        RuleFor(x => x.Password).NotEmpty().WithMessage("Password is required.");
    }
}
