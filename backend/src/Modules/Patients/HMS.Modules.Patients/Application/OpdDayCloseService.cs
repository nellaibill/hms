using HMS.Modules.ActivityLog.Application;
using HMS.Modules.ActivityLog.Contracts;
using HMS.Modules.Patients.Application.Abstractions;
using HMS.Modules.Patients.Contracts;
using HMS.Modules.Patients.Domain;
using HMS.Shared.Kernel;

namespace HMS.Modules.Patients.Application;

/// <summary>The OPD day close (ADR-110) for the tenant already set on this scope's
/// ITenantContext — run once a night by OpdDayCloseSchedulerService.</summary>
internal interface IOpdDayCloseService
{
    /// <summary>Auto-completes every OP consultation line still Waiting/CheckedIn/InConsultation
    /// whose appointment fell on an earlier hospital-local day than <paramref name="nowUtc"/>'s.
    /// Today's and future lines, other statuses and non-OP visits are never touched, so running
    /// it twice changes nothing the second time. Returns how many lines it closed.</summary>
    Task<int> AutoCompleteStaleConsultationsAsync(DateTime nowUtc, CancellationToken cancellationToken);
}

internal class OpdDayCloseService : IOpdDayCloseService
{
    // Keeps one round-trip's tracked graph small — the first run on a tenant can find a
    // backlog of thousands of old Waiting lines (load-test data), a normal night only dozens.
    private const int BatchSize = 200;

    private readonly IPatientVisitRepository _repository;
    private readonly IActivityLogService _activityLog;

    public OpdDayCloseService(IPatientVisitRepository repository, IActivityLogService activityLog)
    {
        _repository = repository;
        _activityLog = activityLog;
    }

    public async Task<int> AutoCompleteStaleConsultationsAsync(DateTime nowUtc, CancellationToken cancellationToken)
    {
        var startOfTodayUtc = StartOfHospitalDayUtc(nowUtc);
        var closedCount = 0;

        while (true)
        {
            var visits = await _repository.GetOpVisitsWithOpenConsultationsBeforeAsync(startOfTodayUtc, BatchSize, cancellationToken);

            var closed = new List<(PatientVisit Visit, PatientVisitConsultation Consultation)>();
            foreach (var visit in visits)
            {
                // A visit can hold a past line and a still-current one — only the past one closes.
                foreach (var consultation in visit.Consultations.Where(c => IsOpen(c.Status) && c.AppointmentTime < startOfTodayUtc))
                {
                    consultation.AutoComplete(nowUtc);
                    closed.Add((visit, consultation));
                }
            }

            // Every visit the query returns has at least one such line, so this is empty only once
            // the query finds nothing — checking closed rather than visits means the loop can
            // never spin on a batch it didn't change.
            if (closed.Count == 0)
            {
                break;
            }

            await _repository.SaveChangesAsync(cancellationToken);
            closedCount += closed.Count;

            foreach (var (visit, consultation) in closed)
            {
                // Best-effort audit write (never throws — see IActivityLogService); the status is
                // already saved. No UserId: the system did this, not a person.
                await _activityLog.LogAsync(new ActivityLogRequest
                {
                    Action = ActivityLogActions.Update,
                    Module = ActivityLogModules.Patients,
                    EntityType = "PatientVisit",
                    EntityId = visit.Id.ToString(),
                    Description = $"OPD consultation auto-completed at day close (was {consultation.AutoCompletedFrom})",
                    OldValues = new { ConsultationId = consultation.Id, Status = consultation.AutoCompletedFrom },
                    NewValues = new { ConsultationId = consultation.Id, consultation.Status },
                }, cancellationToken);
            }
        }

        return closedCount;
    }

    /// <summary>The UTC instant the hospital-local day containing <paramref name="nowUtc"/>
    /// began — India Standard Time, not UTC, which trails it by 5½ hours (same rule as
    /// PharmacyClock).</summary>
    internal static DateTime StartOfHospitalDayUtc(DateTime nowUtc)
    {
        var today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(nowUtc, DateTimeKind.Utc), MonthlySeries.HospitalTimeZone));
        return TimeZoneInfo.ConvertTimeToUtc(today.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified), MonthlySeries.HospitalTimeZone);
    }

    private static bool IsOpen(OpdConsultationStatus status)
        => status is OpdConsultationStatus.Waiting or OpdConsultationStatus.CheckedIn or OpdConsultationStatus.InConsultation;
}
