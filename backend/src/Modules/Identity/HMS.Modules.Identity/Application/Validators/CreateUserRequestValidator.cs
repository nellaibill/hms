using FluentValidation;
using HMS.Modules.Identity.Contracts;

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
            .MaximumLength(30)
            .Matches(@"^[0-9+\-() ]*$")
            .When(x => !string.IsNullOrWhiteSpace(x.PhoneNumber));
    }
}
