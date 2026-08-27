using HMS.Shared.Kernel;

namespace HMS.Modules.Masters.Domain;

/// <summary>
/// A staff job title/designation (e.g. "Nurse", "Doctor", "Lab Technician") — reference data
/// consumed by HR's Employee entity (DepartmentId/DesignationId, both cross-module Guid
/// references, no DB-level FK — see docs/DecisionLog.md). Deliberately a near-exact clone of
/// <see cref="Department"/>'s shape: same Code/Name/IsActive fields, same soft-delete-aware
/// unique code index, so the frontend's config-driven Masters CRUD engine can pick it up with
/// zero new page code.
/// </summary>
internal class Designation : Entity
{
    public string Code { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public bool IsActive { get; private set; } = true;

    // Required by EF Core materialization.
    private Designation()
    {
    }

    private Designation(
        Guid id,
        string code,
        string name,
        bool isActive,
        Guid? createdBy)
        : base(id, createdBy)
    {
        Code = code;
        Name = name;
        IsActive = isActive;
    }

    public static Designation Create(
        string code,
        string name,
        bool isActive,
        Guid? createdBy)
    {
        Guard.AgainstNullOrWhiteSpace(code, nameof(code));
        Guard.AgainstNullOrWhiteSpace(name, nameof(name));

        return new Designation(
            Guid.CreateVersion7(),
            code.Trim().ToUpperInvariant(),
            name.Trim(),
            isActive,
            createdBy);
    }

    public void Update(
        string name,
        bool isActive,
        Guid? updatedBy)
    {
        Guard.AgainstNullOrWhiteSpace(name, nameof(name));

        Name = name.Trim();
        IsActive = isActive;
        MarkUpdated(updatedBy);
    }
}
