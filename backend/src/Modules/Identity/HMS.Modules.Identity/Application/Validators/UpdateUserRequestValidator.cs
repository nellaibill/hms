using FluentValidation;
using HMS.Modules.Identity.Contracts;

namespace HMS.Modules.Identity.Application.Validators;

internal class UpdateUserRequestValidator : AbstractValidator<UpdateUserRequest>
{
    public UpdateUserRequestValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.PhoneNumber)
            .MaximumLength(30)
            .Matches(@"^[0-9+\-() ]*$")
            .When(x => !string.IsNullOrWhiteSpace(x.PhoneNumber));
    }
}
