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

    public string? Description { get; private set; }

    public bool IsSystemRole { get; private set; }

    public int DisplayOrder { get; private set; }

    public bool IsActive { get; private set; }

    /// <summary>
    /// Sidebar menus turned off for this role (ADR-128) — each one the menu's path, e.g.
    /// "/clinical/ipd". The menu and its pages are hidden in the app even though the role keeps
    /// the permission behind them, which other screens need (OPD's lists need Clinical Care, Lab
    /// and Radiology billing need Diagnostics). Empty means every menu its permissions allow.
    /// </summary>
    public IReadOnlyList<string> HiddenMenus { get; private set; } = [];

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
        string? description,
        bool isSystemRole,
        int displayOrder,
        Guid? createdBy)
        : base(id, createdBy)
    {
        Name = name;
        Description = description;
        IsSystemRole = isSystemRole;
        DisplayOrder = displayOrder;
        IsActive = true;
    }

    public static Role Create(
        string name,
        string? description,
        bool isSystemRole,
        int displayOrder,
        Guid? createdBy)
    {
        Guard.AgainstNullOrWhiteSpace(name, nameof(name));

        return new Role(
            Guid.CreateVersion7(),
            name.Trim(),
            description?.Trim(),
            isSystemRole,
            displayOrder,
            createdBy);
    }

    public void Update(
        string name,
        string? description,
        int displayOrder,
        Guid? updatedBy)
    {
        Guard.AgainstNullOrWhiteSpace(name, nameof(name));

        Name = name.Trim();
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

    /// <summary>Replaces the hidden menus — trimmed, blanks and repeats dropped, in a stable order.</summary>
    public void SetHiddenMenus(IEnumerable<string>? menus)
    {
        HiddenMenus = (menus ?? [])
            .Where(menu => !string.IsNullOrWhiteSpace(menu))
            .Select(menu => menu.Trim())
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToList();
    }

    public void ReplacePermissions(IEnumerable<Guid> permissionIds)
    {
        _rolePermissions.Clear();

        foreach (var permissionId in permissionIds.Distinct())
        {
            _rolePermissions.Add(RolePermission.Create(Id, permissionId));
        }
    }
}