namespace HMS.Modules.Roles.Contracts;

public record UpdateRoleRequest
{
    public string Name { get; init; } = string.Empty;

    public string Code { get; init; } = string.Empty;

    public string? Description { get; init; }

    public bool IsSystemRole { get; init; }

    public int DisplayOrder { get; init; }
}