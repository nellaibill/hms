using HMS.Shared.Kernel;

namespace HMS.Modules.Identity.Domain;

/// <summary>
/// Represents a system permission.
/// Permissions are seeded by the system and assigned to roles.
/// </summary>
public sealed class Permission : Entity
{
    private Permission()
    {
    }

    private Permission(
        Guid id,
        string module,
        string action,
        string key,
        string label,
        int displayOrder,
        bool isActive)
        : base(id, null)
    {
        Module = module;
        Action = action;
        Key = key;
        Label = label;
        DisplayOrder = displayOrder;
        IsActive = isActive;
    }

    public string Module { get; private set; } = string.Empty;

    public string Action { get; private set; } = string.Empty;

    public string Key { get; private set; } = string.Empty;

    public string Label { get; private set; } = string.Empty;

    public int DisplayOrder { get; private set; }

    public bool IsActive { get; private set; }

    /// <summary>
    /// Creates a new permission at runtime.
    /// </summary>
    public static Permission Create(
        string module,
        string action,
        string key,
        string label,
        int displayOrder)
    {
        return new Permission(
            Guid.CreateVersion7(),
            module,
            action,
            key,
            label,
            displayOrder,
            true);
    }

    /// <summary>
    /// Creates a permission with a fixed identifier for EF Core seed data.
    /// </summary>
    internal static Permission CreateSeed(
        Guid id,
        string module,
        string action,
        string key,
        string label,
        int displayOrder)
    {
        return new Permission(
            id,
            module,
            action,
            key,
            label,
            displayOrder,
            true);
    }

    public void Update(
        string label,
        int displayOrder,
        bool isActive)
    {
        Label = label;
        DisplayOrder = displayOrder;
        IsActive = isActive;
    }
}