using HMS.Shared.Kernel;

namespace HMS.Modules.HR.Domain;

/// <summary>
/// A recurring daily work-shift definition (e.g. "Morning, 08:00-16:00") used to build
/// staff duty rosters — the aggregate root for Phase 1 of Shift Management. Other roster
/// aggregates (ShiftAssignment, WeeklyRoster, StaffAvailability, ShiftSwapRequest) are
/// out of scope for this phase.
///
/// StartTime/EndTime are time-of-day (TimeOnly), not tied to a specific calendar date —
/// a Shift is a reusable definition, not a scheduled occurrence. EndTime may be earlier
/// than StartTime for a shift that crosses midnight; IsNightShift is caller-supplied
/// (matches the spec's "no extra business rules" — it is not derived/validated here).
/// </summary>
internal class Shift : Entity
{
    public string Code { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public TimeOnly StartTime { get; private set; }
    public TimeOnly EndTime { get; private set; }
    public int BreakMinutes { get; private set; }
    public int GraceMinutes { get; private set; }
    public bool IsNightShift { get; private set; }
    public bool IsActive { get; private set; } = true;

    // Required by EF Core materialization.
    private Shift()
    {
    }

    private Shift(
        Guid id,
        string code,
        string name,
        TimeOnly startTime,
        TimeOnly endTime,
        int breakMinutes,
        int graceMinutes,
        bool isNightShift,
        bool isActive,
        Guid? createdBy)
        : base(id, createdBy)
    {
        Code = code;
        Name = name;
        StartTime = startTime;
        EndTime = endTime;
        BreakMinutes = breakMinutes;
        GraceMinutes = graceMinutes;
        IsNightShift = isNightShift;
        IsActive = isActive;
    }

    public static Shift Create(
        string code,
        string name,
        TimeOnly startTime,
        TimeOnly endTime,
        int breakMinutes,
        int graceMinutes,
        bool isNightShift,
        bool isActive,
        Guid? createdBy)
    {
        Guard.AgainstNullOrWhiteSpace(code, nameof(code));
        Guard.AgainstNullOrWhiteSpace(name, nameof(name));

        // Time-ordered UUID per docs/DatabaseArchitecture.md §4 — same convention every
        // other aggregate root in this codebase uses.
        return new Shift(
            Guid.CreateVersion7(),
            code.Trim().ToUpperInvariant(),
            name.Trim(),
            startTime,
            endTime,
            breakMinutes,
            graceMinutes,
            isNightShift,
            isActive,
            createdBy);
    }

    public void Update(
        string name,
        TimeOnly startTime,
        TimeOnly endTime,
        int breakMinutes,
        int graceMinutes,
        bool isNightShift,
        bool isActive,
        Guid? updatedBy)
    {
        Guard.AgainstNullOrWhiteSpace(name, nameof(name));

        Name = name.Trim();
        StartTime = startTime;
        EndTime = endTime;
        BreakMinutes = breakMinutes;
        GraceMinutes = graceMinutes;
        IsNightShift = isNightShift;
        IsActive = isActive;
        MarkUpdated(updatedBy);
    }
}
