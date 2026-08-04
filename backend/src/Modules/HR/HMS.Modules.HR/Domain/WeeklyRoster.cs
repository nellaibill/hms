using HMS.Shared.Kernel;

namespace HMS.Modules.HR.Domain;

/// <summary>
/// The header record for one department's roster for one week — the aggregate root for
/// Phase 3. Deliberately just a header: it does not generate, store, or reference
/// ShiftAssignments — assigning staff to shifts on dates remains ShiftAssignment's sole
/// responsibility (see docs/DecisionLog.md). Publish (Phase 6) is the one minimal action
/// beyond plain CRUD — it only flips Published/PublishedDate, idempotently; nothing else
/// reacts to it, no state-machine rules, no side effects on ShiftAssignments.
/// </summary>
internal class WeeklyRoster : Entity
{
    public DateOnly WeekStartDate { get; private set; }
    public Guid DepartmentId { get; private set; }
    public bool Published { get; private set; }
    public DateTime? PublishedDate { get; private set; }

    // Required by EF Core materialization.
    private WeeklyRoster()
    {
    }

    private WeeklyRoster(
        Guid id,
        DateOnly weekStartDate,
        Guid departmentId,
        bool published,
        DateTime? publishedDate,
        Guid? createdBy)
        : base(id, createdBy)
    {
        WeekStartDate = weekStartDate;
        DepartmentId = departmentId;
        Published = published;
        PublishedDate = publishedDate;
    }

    // Deliberately no guard clause against Guid.Empty for departmentId — matches
    // ShiftAssignment.Create, which likewise never guards its Guid id parameters;
    // required-ness for Guid fields is enforced at the validator layer only.
    public static WeeklyRoster Create(
        DateOnly weekStartDate,
        Guid departmentId,
        bool published,
        DateTime? publishedDate,
        Guid? createdBy)
    {
        // Time-ordered UUID per docs/DatabaseArchitecture.md §4 — same convention every
        // other aggregate root in this codebase uses.
        return new WeeklyRoster(Guid.CreateVersion7(), weekStartDate, departmentId, published, publishedDate, createdBy);
    }

    public void Update(
        DateOnly weekStartDate,
        Guid departmentId,
        bool published,
        DateTime? publishedDate,
        Guid? updatedBy)
    {
        WeekStartDate = weekStartDate;
        DepartmentId = departmentId;
        Published = published;
        PublishedDate = publishedDate;
        MarkUpdated(updatedBy);
    }

    // Idempotent — mirrors Shift/User's Activate(): a no-op if already published, rather
    // than an error or a re-stamped PublishedDate. Per Phase 6, this is the only publish
    // "workflow": no other state changes, nothing else is triggered.
    public void Publish(Guid? updatedBy)
    {
        if (Published)
        {
            return;
        }

        Published = true;
        PublishedDate = DateTime.UtcNow;
        MarkUpdated(updatedBy);
    }
}
