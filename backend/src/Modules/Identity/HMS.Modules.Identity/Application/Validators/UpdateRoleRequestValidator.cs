using FluentValidation;
using HMS.Modules.Identity.Contracts;

namespace HMS.Modules.Identity.Application.Validators;

internal sealed class UpdateRoleRequestValidator
    : AbstractValidator<UpdateRoleRequest>
{
    public UpdateRoleRequestValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty()
            .MaximumLength(50)
            .Matches("^[A-Z0-9_]+$")
            .WithMessage("Code may contain only uppercase letters, numbers and underscores.");

        RoleValidationRules.ApplyRoleRules(
            this,
            x => x.Name,
            x => x.Description,
            x => x.DisplayOrder,
            x => x.PermissionKeys);
    }
}