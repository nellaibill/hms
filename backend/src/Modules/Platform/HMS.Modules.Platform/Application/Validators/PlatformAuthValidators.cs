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
