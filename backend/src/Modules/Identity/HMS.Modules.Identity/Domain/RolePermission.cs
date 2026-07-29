namespace HMS.Modules.Identity.Domain;

/// <summary>
/// Represents the assignment of a Permission to a Role.
/// This is the junction entity that implements the many-to-many
/// relationship between Roles and Permissions.
/// </summary>
public sealed class RolePermission
{
    private RolePermission()
    {
    }

    private RolePermission(
        Guid roleId,
        Guid permissionId)
    {
        RoleId = roleId;
        PermissionId = permissionId;
    }

    /// <summary>
    /// Gets the Role identifier.
    /// </summary>
    public Guid RoleId { get; private set; }

    /// <summary>
    /// Gets the Permission identifier.
    /// </summary>
    public Guid PermissionId { get; private set; }

    /// <summary>
    /// Navigation property to the Role.
    /// </summary>
    public Role Role { get; private set; } = null!;

    /// <summary>
    /// Navigation property to the Permission.
    /// </summary>
    public Permission Permission { get; private set; } = null!;

    /// <summary>
    /// Creates a new RolePermission assignment.
    /// </summary>
    public static RolePermission Create(
        Guid roleId,
        Guid permissionId)
    {
        return new RolePermission(roleId, permissionId);
    }
}