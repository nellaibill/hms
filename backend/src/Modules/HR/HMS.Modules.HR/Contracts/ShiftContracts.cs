using HMS.Shared.Kernel;

namespace HMS.Modules.HR.Contracts;

public record CreateShiftRequest
{
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;

    // Nullable here (unlike the non-nullable TimeOnly on Shift itself): TimeOnly has no
    // "unset" sentinel — default(TimeOnly) is midnight, a legitimate real shift start —
    // so "StartTime/EndTime are required" can only be validated if a missing value is
    // representable at all, which means nullable in the wire contract.
    public TimeOnly? StartTime { get; init; }
    public TimeOnly? EndTime { get; init; }

    public int BreakMinutes { get; init; }
    public int GraceMinutes { get; init; }
    public bool IsNightShift { get; init; }
    public bool IsActive { get; init; } = true;
}

public record UpdateShiftRequest
{
    public string Name { get; init; } = string.Empty;
    public TimeOnly? StartTime { get; init; }
    public TimeOnly? EndTime { get; init; }
    public int BreakMinutes { get; init; }
    public int GraceMinutes { get; init; }
    public bool IsNightShift { get; init; }
    public bool IsActive { get; init; } = true;
}

public record ShiftResponse
{
    public Guid Id { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public TimeOnly StartTime { get; init; }
    public TimeOnly EndTime { get; init; }
    public int BreakMinutes { get; init; }
    public int GraceMinutes { get; init; }
    public bool IsNightShift { get; init; }
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

public class ShiftListQuery : PagedRequest
{
    public bool? IsActive { get; set; }
}
