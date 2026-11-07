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
        Expression<Func<T, IReadOnlyCollection<string>>> permissionKeys,
        Expression<Func<T, IReadOnlyCollection<string>>> hiddenMenus)
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

        // ADR-128: menu paths from the app's sidebar, e.g. "/clinical/ipd".
        validator.RuleFor(hiddenMenus)
            .NotNull()
            .Must(menus => menus is null || menus.Count <= MaxHiddenMenus)
            .WithMessage($"No more than {MaxHiddenMenus} menus can be hidden.");

        validator.RuleFor(hiddenMenus)
            .Must(menus => menus is null || menus.All(IsMenuPath))
            .WithMessage($"Each hidden menu must be a menu path starting with '/', up to {MaxMenuPathLength} characters.");
    }

    private const int MaxHiddenMenus = 100;

    private const int MaxMenuPathLength = 100;

    private static bool IsMenuPath(string? menu) =>
        !string.IsNullOrWhiteSpace(menu) && menu.Trim().StartsWith('/') && menu.Trim().Length <= MaxMenuPathLength;

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