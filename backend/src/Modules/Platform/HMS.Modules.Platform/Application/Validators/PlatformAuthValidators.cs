using FluentValidation;
using HMS.Modules.Platform.Contracts;
using HMS.Shared.Kernel;

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

/// <summary>Shared malformed-request check for every "prove you have an authenticator app"
/// request (MFA login verify, enable, disable) — a TOTP code is always 6 digits.</summary>
internal static class MfaCodeRule
{
    public static IRuleBuilderOptions<T, string> MustBeAValidTotpCode<T>(this IRuleBuilder<T, string> rule) =>
        rule.NotEmpty().WithMessage("Code is required.")
            .Length(6).WithMessage("Code must be exactly 6 digits.")
            .Matches(@"^\d+$").WithMessage("Code can contain digits only.");
}

internal class PlatformMfaVerifyRequestValidator : AbstractValidator<PlatformMfaVerifyRequest>
{
    public PlatformMfaVerifyRequestValidator()
    {
        RuleFor(x => x.ChallengeToken).NotEmpty().WithMessage("Challenge token is required.");
        RuleFor(x => x.Code).MustBeAValidTotpCode();
    }
}

internal class PlatformMfaEnableRequestValidator : AbstractValidator<PlatformMfaEnableRequest>
{
    public PlatformMfaEnableRequestValidator()
    {
        RuleFor(x => x.Code).MustBeAValidTotpCode();
    }
}

internal class PlatformMfaDisableRequestValidator : AbstractValidator<PlatformMfaDisableRequest>
{
    public PlatformMfaDisableRequestValidator()
    {
        RuleFor(x => x.Code).MustBeAValidTotpCode();
    }
}

/// <summary>Malformed-request checks only — "current password doesn't match" is a
/// business-rule failure reported as a Result failure by PlatformAuthenticationService, not
/// here. Matches HMS.Modules.Identity's ChangePasswordRequestValidator exactly, including
/// reusing the same shared PasswordPolicy.</summary>
internal class PlatformChangePasswordRequestValidator : AbstractValidator<PlatformChangePasswordRequest>
{
    public PlatformChangePasswordRequestValidator()
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
