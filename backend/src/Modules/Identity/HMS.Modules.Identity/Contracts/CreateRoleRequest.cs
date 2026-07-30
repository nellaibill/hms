namespace HMS.Modules.Identity.Contracts;

public record CreateRoleRequest
{
    public string Name { get; init; } = string.Empty;

    public string? Description { get; init; }

    public bool IsActive { get; init; } = true;

    public int DisplayOrder { get; init; }

    /// <summary>
    /// Selected permission keys.
    /// Example:
    /// patient-management.view
    /// patient-management.create
    /// </summary>
    public IReadOnlyCollection<string> PermissionKeys { get; init; }
        = [];
}