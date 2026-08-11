namespace HMS.Modules.IPD.Contracts;

public record IPDDashboardResponse
{
    public int TotalAdmitted { get; init; }
    public int AvailableBeds { get; init; }
    public int OccupiedBeds { get; init; }
    public int IcuTotalBeds { get; init; }
    public int IcuOccupiedBeds { get; init; }

    /// <summary>Percentage, 0-100. 0 when the hospital has no ICU beds configured.</summary>
    public double IcuOccupancyRate { get; init; }

    public int TodaysAdmissions { get; init; }
    public int TodaysDischarges { get; init; }
}
