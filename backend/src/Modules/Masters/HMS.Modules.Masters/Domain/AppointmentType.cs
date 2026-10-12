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

    /// <summary>Manual sort weighting for the appointment type picker (Registration's Add
    /// Visit, Record Visit) — lower shows first, null/unset sorts after every prioritized
    /// type, ties break alphabetically (see AppointmentTypeRepository.ApplySort). Same
    /// mechanism as Consultant.Priority.</summary>
    public int? Priority { get; private set; }

    // Required by EF Core materialization.
    private AppointmentType()
    {
    }

    private AppointmentType(
        Guid id,
        string name,
        bool isActive,
        int? priority,
        Guid? createdBy)
        : base(id, createdBy)
    {
        Name = name;
        IsActive = isActive;
        Priority = priority;
    }

    public static AppointmentType Create(
        string name,
        bool isActive,
        int? priority,
        Guid? createdBy)
    {
        Guard.AgainstNullOrWhiteSpace(name, nameof(name));

        return new AppointmentType(
            Guid.CreateVersion7(),
            name.Trim(),
            isActive,
            priority,
            createdBy);
    }

    public void Update(
        string name,
        bool isActive,
        int? priority,
        Guid? updatedBy)
    {
        Guard.AgainstNullOrWhiteSpace(name, nameof(name));

        Name = name.Trim();
        IsActive = isActive;
        Priority = priority;
        MarkUpdated(updatedBy);
    }
}
