using FluentValidation;
using HMS.Modules.Identity.Contracts;

namespace HMS.Modules.Identity.Application.Validators;

/// <summary>
/// Malformed-request checks only (empty fields, unrecognized LoginType) — these return 400,
/// distinct from a credentials-didn't-match failure, which AuthenticationService reports as
/// a 401 Result failure. Matches CreateUserRequestValidator's split.
/// </summary>
internal class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.LoginType)
            .NotEmpty()
            .WithMessage("Login type is required.")
            .Must(LoginTypes.IsKnown)
            .WithMessage("Login type is not recognized.");
        RuleFor(x => x.Username).NotEmpty().WithMessage("Username is required.");
        RuleFor(x => x.Password).NotEmpty().WithMessage("Password is required.");
    }
}
