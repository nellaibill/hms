using System.Linq.Expressions;
using FluentValidation;

namespace HMS.Modules.Identity.Application.Validators;

/// <summary>
/// Shared validation rules for role create/update requests.
/// Keeps validation logic in one place to avoid duplication.
/// </summary>
internal static class RoleValidationRules
{
    public static void ApplyRoleRules<T>(
        AbstractValidator<T> validator,
        Expression<Func<T, string>> name,
        Expression<Func<T, string?>> description,
        Expression<Func<T, int>> displayOrder,
        Expression<Func<T, IReadOnlyCollection<string>>> permissionKeys)
    {
        validator.RuleFor(name)
            .NotEmpty()
            .MaximumLength(100);

        validator.RuleFor(description)
            .MaximumLength(500);

        validator.RuleFor(displayOrder)
            .GreaterThanOrEqualTo(0);

        validator.RuleFor(permissionKeys)
            .NotNull()
            .Must(keys => keys.Any())
            .WithMessage("At least one permission must be selected.");

        validator.RuleFor(permissionKeys)
            .Must(HaveUniquePermissions)
            .WithMessage("Duplicate permissions are not allowed.");
    }

    private static bool HaveUniquePermissions(
        IEnumerable<string>? permissionKeys)
    {
        if (permissionKeys is null)
        {
            return false;
        }

        var keys = permissionKeys
            .Where(k => !string.IsNullOrWhiteSpace(k))
            .Select(k => k.Trim())
            .ToList();

        return keys.Count ==
               keys.Distinct(StringComparer.OrdinalIgnoreCase).Count();
    }
}