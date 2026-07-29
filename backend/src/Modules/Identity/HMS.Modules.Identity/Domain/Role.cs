using HMS.Shared.Kernel;

namespace HMS.Modules.Identity.Domain;

/// <summary>
/// Represents a hospital role that can be assigned to users.
/// Examples: Super Admin, Doctor, Receptionist.
/// </summary>
internal sealed class Role : Entity
{
    private readonly List<RolePermission> _rolePermissions = [];

    public string Name { get; private set; } = null!;

    public string Code { get; private set; } = null!;

    public string? Description { get; private set; }

    public bool IsSystemRole { get; private set; }

    public int DisplayOrder { get; private set; }

    public bool IsActive { get; private set; }

    /// <summary>
    /// Permissions assigned to this role.
    /// </summary>
    public IReadOnlyCollection<RolePermission> RolePermissions =>
        _rolePermissions.AsReadOnly();

    // Required by EF Core materialization.
    private Role()
    {
    }

    private Role(
        Guid id,
        string name,
        string code,
        string? description,
        bool isSystemRole,
        int displayOrder,
        Guid? createdBy)
        : base(id, createdBy)
    {
        Name = name;
        Code = code;
        Description = description;
        IsSystemRole = isSystemRole;
        DisplayOrder = displayOrder;
        IsActive = true;
    }

    public static Role Create(
        string name,
        string code,
        string? description,
        bool isSystemRole,
        int displayOrder,
        Guid? createdBy)
    {
        Guard.AgainstNullOrWhiteSpace(name, nameof(name));
        Guard.AgainstNullOrWhiteSpace(code, nameof(code));

        return new Role(
            Guid.CreateVersion7(),
            name.Trim(),
            NormalizeCode(code),
            description?.Trim(),
            isSystemRole,
            displayOrder,
            createdBy);
    }

    public void Update(
        string name,
        string code,
        string? description,
        int displayOrder,
        Guid? updatedBy)
    {
        Guard.AgainstNullOrWhiteSpace(name, nameof(name));
        Guard.AgainstNullOrWhiteSpace(code, nameof(code));

        Name = name.Trim();
        Code = NormalizeCode(code);
        Description = description?.Trim();
        DisplayOrder = displayOrder;

        MarkUpdated(updatedBy);
    }

    public void Activate(Guid? updatedBy)
    {
        if (IsActive)
        {
            return;
        }

        IsActive = true;
        MarkUpdated(updatedBy);
    }

    public void Deactivate(Guid? updatedBy)
    {
        if (!IsActive)
        {
            return;
        }

        IsActive = false;
        MarkUpdated(updatedBy);
    }

    public void ReplacePermissions(IEnumerable<Guid> permissionIds)
    {
        _rolePermissions.Clear();

        foreach (var permissionId in permissionIds.Distinct())
        {
            _rolePermissions.Add(RolePermission.Create(Id, permissionId));
        }
    }

    private static string NormalizeCode(string code)
        => code.Trim().ToUpperInvariant();
}