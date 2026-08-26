using HMS.Shared.Kernel;

namespace HMS.Modules.Masters.Domain;

/// <summary>
/// A patient registration's OP appointment category (e.g. "New", "Follow-up", "Referral") —
/// shared reference data for Patients (a visit's optional AppointmentTypeId), matching the
/// Department/Consultant consolidation pattern (see docs/DecisionLog.md).
/// </summary>
internal class AppointmentType : Entity
{
    public string Name { get; private set; } = null!;
    public bool IsActive { get; private set; } = true;

    // Required by EF Core materialization.
    private AppointmentType()
    {
    }

    private AppointmentType(
        Guid id,
        string name,
        bool isActive,
        Guid? createdBy)
        : base(id, createdBy)
    {
        Name = name;
        IsActive = isActive;
    }

    public static AppointmentType Create(
        string name,
        bool isActive,
        Guid? createdBy)
    {
        Guard.AgainstNullOrWhiteSpace(name, nameof(name));

        return new AppointmentType(
            Guid.CreateVersion7(),
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
