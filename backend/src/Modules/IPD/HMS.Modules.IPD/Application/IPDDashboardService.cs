using HMS.Modules.IPD.Application.Abstractions;
using HMS.Modules.IPD.Contracts;
using HMS.Shared.Kernel;

namespace HMS.Modules.IPD.Application;

/// <summary>
/// Public (not internal): IPDDashboardController — which ASP.NET Core requires to be a
/// public class with a public constructor for controller discovery/DI activation — takes
/// this as a constructor dependency; a public constructor cannot have an internal parameter
/// type (CS0051).
/// </summary>
public interface IIPDDashboardService
{
    Task<IPDDashboardResponse> GetDashboardAsync(CancellationToken cancellationToken);

    /// <summary>Admissions per calendar month (hospital local time) for the last
    /// <paramref name="months"/> months, oldest first, zero-filled — the Executive Dashboard's IP
    /// census (DASH-01).</summary>
    Task<IReadOnlyList<MonthlyTotal>> GetMonthlyAdmissionsAsync(int months, CancellationToken cancellationToken);
}

internal class IPDDashboardService : IIPDDashboardService
{
    private readonly IAdmissionRepository _admissionRepository;
    private readonly IBedRepository _bedRepository;

    public IPDDashboardService(IAdmissionRepository admissionRepository, IBedRepository bedRepository)
    {
        _admissionRepository = admissionRepository;
        _bedRepository = bedRepository;
    }

    public async Task<IReadOnlyList<MonthlyTotal>> GetMonthlyAdmissionsAsync(int months, CancellationToken cancellationToken)
    {
        var utcNow = DateTime.UtcNow;
        var times = await _admissionRepository.GetAdmissionTimesAsync(MonthlySeries.StartUtc(months, utcNow), utcNow, cancellationToken);
        return MonthlySeries.Build(times.Select(t => (t, 1m)), months, utcNow);
    }

    public async Task<IPDDashboardResponse> GetDashboardAsync(CancellationToken cancellationToken)
    {
        var totalAdmitted = await _admissionRepository.CountByStatusAsync(AdmissionStatus.Admitted, cancellationToken);
        var availableBeds = await _bedRepository.CountByStatusAsync(BedStatus.Available, cancellationToken);
        var occupiedBeds = await _bedRepository.CountByStatusAsync(BedStatus.Occupied, cancellationToken);
        var (icuTotal, icuOccupied) = await _bedRepository.GetIcuOccupancyAsync(cancellationToken);
        var todaysAdmissions = await _admissionRepository.CountAdmittedTodayAsync(cancellationToken);
        var todaysDischarges = await _admissionRepository.CountDischargedTodayAsync(cancellationToken);

        return new IPDDashboardResponse
        {
            TotalAdmitted = totalAdmitted,
            AvailableBeds = availableBeds,
            OccupiedBeds = occupiedBeds,
            IcuTotalBeds = icuTotal,
            IcuOccupiedBeds = icuOccupied,
            IcuOccupancyRate = icuTotal == 0 ? 0 : Math.Round(icuOccupied * 100.0 / icuTotal, 1),
            TodaysAdmissions = todaysAdmissions,
            TodaysDischarges = todaysDischarges,
        };
    }
}
