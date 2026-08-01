using FluentValidation;
using HMS.Modules.Identity.Contracts;

namespace HMS.Modules.Identity.Application.Validators;

internal sealed class CreateRoleRequestValidator
    : AbstractValidator<CreateRoleRequest>
{
    public CreateRoleRequestValidator()
    {
        RoleValidationRules.ApplyRoleRules(
            this,
            x => x.Name,
            x => x.Description,
            x => x.DisplayOrder,
            x => x.PermissionKeys);
    }
}