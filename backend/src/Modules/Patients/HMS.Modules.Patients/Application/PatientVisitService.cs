using HMS.Modules.Masters.Application;
using HMS.Modules.Patients.Application.Abstractions;
using HMS.Modules.Patients.Application.Mapping;
using HMS.Modules.Patients.Contracts;
using HMS.Modules.Patients.Domain;
using HMS.Shared.Kernel;
using Microsoft.Extensions.Logging;

namespace HMS.Modules.Patients.Application;

/// <summary>
/// Orchestrates recording a patient visit (Registration Details). Department/Consultant/
/// AppointmentType/ConsultationType are cross-module references into HMS.Modules.Masters'
/// reference data — each is checked to actually exist before the visit is created, the same
/// pattern PatientService.ValidateAddressReferencesAsync already uses for State/District.
/// </summary>
internal class PatientVisitService : IPatientVisitService
{
    private readonly IPatientVisitRepository _repository;
    private readonly IPatientRepository _patientRepository;
    private readonly IDepartmentService _departmentService;
    private readonly IConsultantService _consultantService;
    private readonly IAppointmentTypeService _appointmentTypeService;
    private readonly IConsultationTypeService _consultationTypeService;
    private readonly ILogger<PatientVisitService> _logger;

    public PatientVisitService(
        IPatientVisitRepository repository,
        IPatientRepository patientRepository,
        IDepartmentService departmentService,
        IConsultantService consultantService,
        IAppointmentTypeService appointmentTypeService,
        IConsultationTypeService consultationTypeService,
        ILogger<PatientVisitService> logger)
    {
        _repository = repository;
        _patientRepository = patientRepository;
        _departmentService = departmentService;
        _consultantService = consultantService;
        _appointmentTypeService = appointmentTypeService;
        _consultationTypeService = consultationTypeService;
        _logger = logger;
    }

    public async Task<Result<PatientVisitResponse>> CreateAsync(Guid patientId, CreatePatientVisitRequest request, Guid? actorId, CancellationToken cancellationToken)
    {
        if (!await _patientRepository.ExistsAsync(patientId, cancellationToken))
        {
            return Result<PatientVisitResponse>.Failure(PatientErrorCodes.NotFound, $"Patient '{patientId}' was not found.");
        }

        if (request.AppointmentTypeId.HasValue)
        {
            var appointmentType = await _appointmentTypeService.GetByIdAsync(request.AppointmentTypeId.Value, cancellationToken);
            if (!appointmentType.IsSuccess)
            {
                return Result<PatientVisitResponse>.Failure(PatientErrorCodes.InvalidAppointmentType, $"Appointment type '{request.AppointmentTypeId}' was not found.");
            }
        }

        foreach (var line in request.Consultations)
        {
            var department = await _departmentService.GetByIdAsync(line.DepartmentId, cancellationToken);
            if (!department.IsSuccess)
            {
                return Result<PatientVisitResponse>.Failure(PatientErrorCodes.InvalidDepartment, $"Department '{line.DepartmentId}' was not found.");
            }

            var consultant = await _consultantService.GetByIdAsync(line.ConsultantId, cancellationToken);
            if (!consultant.IsSuccess)
            {
                return Result<PatientVisitResponse>.Failure(PatientErrorCodes.InvalidConsultant, $"Consultant '{line.ConsultantId}' was not found.");
            }

            if (line.ConsultationTypeId.HasValue)
            {
                var consultationType = await _consultationTypeService.GetByIdAsync(line.ConsultationTypeId.Value, cancellationToken);
                if (!consultationType.IsSuccess)
                {
                    return Result<PatientVisitResponse>.Failure(PatientErrorCodes.InvalidConsultationType, $"Consultation type '{line.ConsultationTypeId}' was not found.");
                }

                // The consultant's own Consultation Types (Masters → Consultant Edit) are the one
                // source of truth for which types can be booked with them — the same rule the
                // Registration, Add Visit and Billing pickers apply, enforced here so no page (or
                // direct API call) can record a type the doctor doesn't offer.
                if (!consultant.Value!.ConsultationTypeCharges.Any(c => c.ConsultationTypeId == line.ConsultationTypeId.Value))
                {
                    return Result<PatientVisitResponse>.Failure(
                        PatientErrorCodes.ConsultationTypeNotOffered,
                        $"{consultant.Value.Name} does not offer '{consultationType.Value!.Name}'. Pick one of the consultant's consultation types (Masters → Consultants).");
                }
            }
        }

        var visit = PatientVisit.Create(patientId, request.VisitType, request.AppointmentTypeId, actorId);
        foreach (var line in request.Consultations)
        {
            visit.AddConsultation(PatientVisitConsultation.Create(visit.Id, line.DepartmentId, line.ConsultantId, line.ConsultationTypeId, line.AppointmentTime), actorId);
        }

        await _repository.AddAsync(visit, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Recorded visit {VisitId} for patient {PatientId} with {ConsultationCount} consultation(s)", visit.Id, patientId, visit.Consultations.Count);

        return Result<PatientVisitResponse>.Success(visit.ToResponse());
    }

    public async Task<Result<PatientVisitResponse>> GetByIdAsync(Guid patientId, Guid visitId, CancellationToken cancellationToken)
    {
        var visit = await _repository.GetByIdAsync(visitId, cancellationToken);
        if (visit is null || visit.PatientId != patientId)
        {
            return Result<PatientVisitResponse>.Failure(PatientErrorCodes.VisitNotFound, $"Visit '{visitId}' was not found for this patient.");
        }

        return Result<PatientVisitResponse>.Success(visit.ToResponse());
    }

    public async Task<Result<IReadOnlyList<PatientVisitResponse>>> GetByPatientIdAsync(Guid patientId, CancellationToken cancellationToken)
    {
        var visits = await _repository.GetByPatientIdAsync(patientId, cancellationToken);
        return Result<IReadOnlyList<PatientVisitResponse>>.Success(visits.Select(v => v.ToResponse()).ToList());
    }

    public async Task<IReadOnlyList<MonthlyTotal>> GetMonthlyCountsAsync(VisitType visitType, int months, CancellationToken cancellationToken)
    {
        var utcNow = DateTime.UtcNow;
        var timestamps = await _repository.GetCreatedAtSinceAsync(visitType, MonthlySeries.StartUtc(months, utcNow), cancellationToken);
        return MonthlySeries.Build(timestamps.Select(t => (t, 1m)), months, utcNow);
    }

    public async Task<PagedResult<PatientVisitResponse>> GetAllAsync(PatientVisitListQuery query, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _repository.GetPagedAsync(query, cancellationToken);
        var mapped = items.Select(v => v.ToResponse()).ToList();
        return new PagedResult<PatientVisitResponse>(mapped, query.Page, query.PageSize, totalCount);
    }
}
