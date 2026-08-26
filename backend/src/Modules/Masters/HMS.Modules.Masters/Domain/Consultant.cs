using HMS.Shared.Kernel;

namespace HMS.Modules.Masters.Domain;

/// <summary>
/// A consultant/doctor a patient visit can be attributed to — closes the gap Patients'
/// PatientRegistration.Consultant left as free text (per its own doc comment: "Free-text
/// placeholders until the Staff module exists to back these with a real consultant/
/// department master"). DepartmentId is optional and same-module (validated by direct
/// repository check, not a service call) since Department now lives here too.
/// </summary>
internal class Consultant : Entity
{
    public string Name { get; private set; } = null!;
    public Guid? DepartmentId { get; private set; }
    public string? Specialization { get; private set; }
    public bool IsActive { get; private set; } = true;

    // Required by EF Core materialization.
    private Consultant()
    {
    }

    private Consultant(
        Guid id,
        string name,
        Guid? departmentId,
        string? specialization,
        bool isActive,
        Guid? createdBy)
        : base(id, createdBy)
    {
        Name = name;
        DepartmentId = departmentId;
        Specialization = specialization;
        IsActive = isActive;
    }

    public static Consultant Create(
        string name,
        Guid? departmentId,
        string? specialization,
        bool isActive,
        Guid? createdBy)
    {
        Guard.AgainstNullOrWhiteSpace(name, nameof(name));

        return new Consultant(
            Guid.CreateVersion7(),
            name.Trim(),
            departmentId,
            specialization?.Trim(),
            isActive,
            createdBy);
    }

    public void Update(
        string name,
        Guid? departmentId,
        string? specialization,
        bool isActive,
        Guid? updatedBy)
    {
        Guard.AgainstNullOrWhiteSpace(name, nameof(name));

        Name = name.Trim();
        DepartmentId = departmentId;
        Specialization = specialization?.Trim();
        IsActive = isActive;
        MarkUpdated(updatedBy);
    }
}
