using FluentValidation;
using HMS.Modules.Identity.Contracts;

namespace HMS.Modules.Identity.Application.Validators;

internal class UpdateUserRequestValidator : AbstractValidator<UpdateUserRequest>
{
    public UpdateUserRequestValidator()
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
    }
}
