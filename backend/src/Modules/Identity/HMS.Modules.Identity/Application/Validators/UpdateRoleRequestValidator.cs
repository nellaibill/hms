using FluentValidation;
using HMS.Modules.Identity.Contracts;

namespace HMS.Modules.Identity.Application.Validators;

internal sealed class UpdateRoleRequestValidator
    : AbstractValidator<UpdateRoleRequest>
{
    public UpdateRoleRequestValidator()
    {
        RoleValidationRules.ApplyRoleRules(
            this,
            x => x.Name,
            x => x.Description,
            x => x.DisplayOrder,
            x => x.PermissionKeys);
    }
}