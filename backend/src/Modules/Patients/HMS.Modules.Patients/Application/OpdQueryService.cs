using HMS.Modules.Masters.Application;
using HMS.Modules.Patients.Application.Abstractions;
using HMS.Modules.Patients.Application.Mapping;
using HMS.Modules.Patients.Contracts;
using HMS.Shared.Kernel;

namespace HMS.Modules.Patients.Application;

/// <summary>
/// Read/transition surface for the OPD (Out Patient Department) queue — a thin layer over
/// PatientVisitConsultation's AppointmentTime/Status (see that entity's own doc comment).
/// Kept separate from PatientVisitService: that service owns *creating* visits/consultations,
/// this one owns *querying the queue* and *advancing its status*, a different use-case shape
/// (paged/grouped reads, transition-by-action-name) that would otherwise crowd an already
/// sizeable service.
/// </summary>
internal class OpdQueryService : IOpdQueryService
{
    private readonly IPatientVisitRepository _repository;
    private readonly IDepartmentService _departmentService;
    private readonly IConsultantService _consultantService;
    private readonly IAppointmentTypeService _appointmentTypeService;

    public OpdQueryService(
        IPatientVisitRepository repository,
        IDepartmentService departmentService,
        IConsultantService consultantService,
        IAppointmentTypeService appointmentTypeService)
    {
        _repository = repository;
        _departmentService = departmentService;
        _consultantService = consultantService;
        _appointmentTypeService = appointmentTypeService;
    }

    public async Task<PagedResult<OpdPatientListItem>> GetPatientListAsync(OpdPatientListQuery query, CancellationToken cancellationToken)
    {
        var (rows, totalCount) = await _repository.GetOpdPatientListPagedAsync(query, cancellationToken);

        // MVP-scale N+1 lookups (one Department/Consultant/AppointmentType round-trip per row)
        // to denormalize display fields — same accepted pattern as
        // HMS.Modules.IPD.Application.AdmissionService.BuildResponseAsync, fine at OPD's
        // per-day row volumes.
        var items = new List<OpdPatientListItem>(rows.Count);
        foreach (var row in rows)
        {
            items.Add(await ToItemAsync(row, cancellationToken));
        }

        return new PagedResult<OpdPatientListItem>(items, query.Page, query.PageSize, totalCount);
    }

    public async Task<Result<IReadOnlyList<OpdConsultationSummaryItem>>> GetConsultationSummaryAsync(OpdConsultationSummaryQuery query, CancellationToken cancellationToken)
    {
        var rows = await _repository.GetOpdConsultationSummaryRowsAsync(query, cancellationToken);

        var groups = rows.GroupBy(r => r.ConsultantId);

        var items = new List<OpdConsultationSummaryItem>();
        foreach (var group in groups)
        {
            var rowsForConsultant = group.ToList();
            var first = rowsForConsultant[0];

            var consultant = await _consultantService.GetByIdAsync(group.Key, cancellationToken);
            var department = await _departmentService.GetByIdAsync(first.DepartmentId, cancellationToken);

            items.Add(new OpdConsultationSummaryItem
            {
                ConsultantId = group.Key,
                ConsultantName = consultant.Value?.Name ?? string.Empty,
                DepartmentId = first.DepartmentId,
                DepartmentName = department.Value?.Name ?? string.Empty,
                TotalPatients = rowsForConsultant.Count,
                Waiting = rowsForConsultant.Count(r => r.Status == OpdConsultationStatus.Waiting),
                InConsultation = rowsForConsultant.Count(r => r.Status == OpdConsultationStatus.InConsultation),
                Completed = rowsForConsultant.Count(r => r.Status == OpdConsultationStatus.Completed),
                AvailableDays = consultant.Value?.AvailableDays ?? [],
                VisitStartTime = consultant.Value?.VisitStartTime,
                VisitEndTime = consultant.Value?.VisitEndTime,
            });
        }

        return Result<IReadOnlyList<OpdConsultationSummaryItem>>.Success(items);
    }

    public async Task<Result<OpdPatientListItem>> GetConsultationDetailAsync(Guid consultationId, CancellationToken cancellationToken)
    {
        var row = await _repository.GetOpdConsultationDetailAsync(consultationId, cancellationToken);
        if (row is null)
        {
            return Result<OpdPatientListItem>.Failure(PatientErrorCodes.ConsultationNotFound, $"Consultation '{consultationId}' was not found.");
        }

        return Result<OpdPatientListItem>.Success(await ToItemAsync(row, cancellationToken));
    }

    public async Task<Result<VisitConsultationResponse>> TransitionAsync(Guid consultationId, string transitionAction, Guid? actorId, CancellationToken cancellationToken)
    {
        var visit = await _repository.GetByConsultationIdAsync(consultationId, cancellationToken);
        var consultation = visit?.Consultations.FirstOrDefault(c => c.Id == consultationId);
        if (visit is null || consultation is null)
        {
            return Result<VisitConsultationResponse>.Failure(PatientErrorCodes.ConsultationNotFound, $"Consultation '{consultationId}' was not found.");
        }

        try
        {
            switch (transitionAction)
            {
                case "check-in":
                    consultation.CheckIn();
                    break;
                case "start-consultation":
                    consultation.StartConsultation();
                    break;
                case "complete":
                    consultation.Complete();
                    break;
                case "cancel":
                    consultation.Cancel();
                    break;
                case "no-show":
                    consultation.MarkNoShow();
                    break;
                default:
                    return Result<VisitConsultationResponse>.Failure(PatientErrorCodes.InvalidStatusTransition, $"Unknown transition action '{transitionAction}'.");
            }
        }
        catch (InvalidOperationException ex)
        {
            return Result<VisitConsultationResponse>.Failure(PatientErrorCodes.InvalidStatusTransition, ex.Message);
        }

        await _repository.SaveChangesAsync(cancellationToken);

        return Result<VisitConsultationResponse>.Success(consultation.ToResponse());
    }

    private async Task<OpdPatientListItem> ToItemAsync(OpdPatientListRow row, CancellationToken cancellationToken)
    {
        var department = await _departmentService.GetByIdAsync(row.DepartmentId, cancellationToken);
        var consultant = await _consultantService.GetByIdAsync(row.ConsultantId, cancellationToken);

        string? appointmentTypeName = null;
        if (row.AppointmentTypeId.HasValue)
        {
            var appointmentType = await _appointmentTypeService.GetByIdAsync(row.AppointmentTypeId.Value, cancellationToken);
            appointmentTypeName = appointmentType.Value?.Name;
        }

        return new OpdPatientListItem
        {
            ConsultationId = row.ConsultationId,
            VisitId = row.VisitId,
            PatientId = row.PatientId,
            Uhid = row.Uhid,
            PatientName = $"{row.FirstName} {row.LastName}",
            PhoneNumber = row.PrimaryPhone,
            Age = CalculateAge(row.DateOfBirth, DateOnly.FromDateTime(DateTime.UtcNow)),
            Gender = row.Gender,
            AppointmentTime = row.AppointmentTime,
            AppointmentTypeId = row.AppointmentTypeId,
            AppointmentTypeName = appointmentTypeName,
            DepartmentId = row.DepartmentId,
            DepartmentName = department.Value?.Name ?? string.Empty,
            ConsultantId = row.ConsultantId,
            ConsultantName = consultant.Value?.Name ?? string.Empty,
            Status = row.Status,
        };
    }

    /// <summary>Mirrors Patient.CalculateAge exactly (that one is private to the Patient
    /// aggregate) — OpdPatientListRow carries DateOfBirth rather than a pre-computed Age since
    /// it's a plain repository projection, not the Patient entity itself.</summary>
    private static int CalculateAge(DateOnly dateOfBirth, DateOnly asOf)
    {
        var age = asOf.Year - dateOfBirth.Year;
        if (dateOfBirth > asOf.AddYears(-age))
        {
            age--;
        }

        return age;
    }
}
