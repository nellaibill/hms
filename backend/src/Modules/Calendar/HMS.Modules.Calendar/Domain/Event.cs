using HMS.Modules.Calendar.Contracts;
using HMS.Shared.Kernel;

namespace HMS.Modules.Calendar.Domain;

/// <summary>
/// A single calendar entry — a holiday, a hospital-wide event, a doctor's leave, a
/// meeting, training, or maintenance window — the aggregate root for Calendar Phase 1.
/// DepartmentId is a plain Guid reference with no navigation property, validated for
/// existence (when supplied) at the Application layer against HR's Department directory
/// — the same ID-reference convention every other cross-aggregate reference in this
/// codebase already uses (mirrors ShiftAssignment.DepartmentId).
///
/// Deliberately has no field identifying *which* doctor a "Doctor Leave" event belongs
/// to. The originating spec's "Doctor Leave cannot overlap another approved leave for
/// the same doctor" rule cannot be implemented without one — per explicit instruction,
/// this is documented as a known limitation (see EventService) rather than resolved by
/// adding an unrequested field to this table.
/// </summary>
internal class Event : Entity
{
    public string Title { get; private set; } = null!;

    public string? Description { get; private set; }

    public EventType EventType { get; private set; }

    public DateTime StartDate { get; private set; }

    public DateTime EndDate { get; private set; }

    public bool IsAllDay { get; private set; }

    public Guid? DepartmentId { get; private set; }

    // Required by EF Core materialization.
    private Event()
    {
    }

    private Event(
        Guid id,
        string title,
        string? description,
        EventType eventType,
        DateTime startDate,
        DateTime endDate,
        bool isAllDay,
        Guid? departmentId,
        Guid? createdBy)
        : base(id, createdBy)
    {
        Title = title;
        Description = description;
        EventType = eventType;
        StartDate = startDate;
        EndDate = endDate;
        IsAllDay = isAllDay;
        DepartmentId = departmentId;
    }

    public static Event Create(
        string title,
        string? description,
        EventType eventType,
        DateTime startDate,
        DateTime endDate,
        bool isAllDay,
        Guid? departmentId,
        Guid? createdBy)
    {
        Guard.AgainstNullOrWhiteSpace(title, nameof(title));

        // Time-ordered UUID per docs/DatabaseArchitecture.md §4 — same convention every
        // other aggregate root in this codebase uses.
        return new Event(
            Guid.CreateVersion7(),
            title.Trim(),
            description?.Trim(),
            eventType,
            startDate,
            endDate,
            isAllDay,
            departmentId,
            createdBy);
    }

    public void Update(
        string title,
        string? description,
        EventType eventType,
        DateTime startDate,
        DateTime endDate,
        bool isAllDay,
        Guid? departmentId,
        Guid? updatedBy)
    {
        Guard.AgainstNullOrWhiteSpace(title, nameof(title));

        Title = title.Trim();
        Description = description?.Trim();
        EventType = eventType;
        StartDate = startDate;
        EndDate = endDate;
        IsAllDay = isAllDay;
        DepartmentId = departmentId;
        MarkUpdated(updatedBy);
    }
}
