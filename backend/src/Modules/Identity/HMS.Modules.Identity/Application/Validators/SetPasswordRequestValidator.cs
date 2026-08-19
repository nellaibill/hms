using FluentValidation;
using HMS.Modules.Identity.Contracts;
using HMS.Shared.Kernel;

namespace HMS.Modules.Identity.Application.Validators;

internal class SetPasswordRequestValidator : AbstractValidator<SetPasswordRequest>
{
    public SetPasswordRequestValidator()
    {
        RuleFor(x => x.Password)
            .NotEmpty()
            .WithMessage("Password is required.")
            .MinimumLength(PasswordPolicy.MinimumLength)
            .WithMessage($"Password must be at least {PasswordPolicy.MinimumLength} characters.")
            .Matches(PasswordPolicy.ComplexityRegex)
            .WithMessage(PasswordPolicy.ComplexityMessage);
    }
}
