namespace HMS.Modules.Identity.Contracts;

public record RoleResponse
{
    public Guid Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public string? Description { get; init; }

    public bool IsSystemRole { get; init; }

    public bool IsActive { get; init; }

    public int DisplayOrder { get; init; }

    public DateTime CreatedAt { get; init; }

    public DateTime? UpdatedAt { get; init; }

    /// <summary>
    /// Permission keys assigned to this role.
    /// Example:
    /// patient-management.view
    /// patient-management.create
    /// </summary>
    public IReadOnlyCollection<string> PermissionKeys { get; init; }
        = [];
}