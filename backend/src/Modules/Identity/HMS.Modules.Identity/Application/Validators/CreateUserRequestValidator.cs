using FluentValidation;
using HMS.Modules.Identity.Contracts;
using HMS.Shared.Kernel;

namespace HMS.Modules.Identity.Application.Validators;

/// <summary>
/// Server-side validation per docs/modules/Identity/Users.md's validation rules table.
/// The authoritative check — client-side validation (frontend/shared/validation) is a
/// UX convenience only, per docs/ApiStandards.md §7.
/// </summary>
internal class CreateUserRequestValidator : AbstractValidator<CreateUserRequest>
{
    public CreateUserRequestValidator()
    {
        RuleFor(x => x.Username)
            .NotEmpty()
            .MaximumLength(100)
            .Matches(@"^[a-zA-Z0-9._-]+$")
            .WithMessage("Username may contain only letters, numbers, dots, underscores and hyphens.");
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.PhoneNumber)
            .NotEmpty()
            .WithMessage("Phone number is required.")
            .Length(10)
            .WithMessage("Phone number must be exactly 10 digits.")
            .Matches(@"^\d+$")
            .WithMessage("Phone number can contain digits only.");
        RuleFor(x => x.RoleId)
            .NotEmpty()
            .WithMessage("Role is required.");

        // Optional (see CreateUserRequest.Password) — same policy as SetPasswordRequestValidator
        // whenever one is actually supplied.
        When(x => !string.IsNullOrEmpty(x.Password), () =>
        {
            RuleFor(x => x.Password!)
                .MinimumLength(PasswordPolicy.MinimumLength)
                .WithMessage($"Password must be at least {PasswordPolicy.MinimumLength} characters.")
                .Matches(PasswordPolicy.ComplexityRegex)
                .WithMessage(PasswordPolicy.ComplexityMessage);
        });
    }
}
