using HMS.Modules.IPD.Application.Abstractions;
using HMS.Modules.IPD.Contracts;
using HMS.Modules.IPD.Domain;
using HMS.Modules.Masters.Application;
using HMS.Modules.Patients.Application;
using HMS.Shared.Kernel;

namespace HMS.Modules.IPD.Application;

/// <summary>
/// Public (not internal): AdmissionsController — which ASP.NET Core requires to be a public
/// class with a public constructor for controller discovery/DI activation — takes this as
/// a constructor dependency; a public constructor cannot have an internal parameter type
/// (CS0051).
/// </summary>
public interface IAdmissionService
{
    Task<Result<AdmissionResponse>> CreateAsync(CreateAdmissionRequest request, Guid? actorId, CancellationToken cancellationToken);

    Task<Result<AdmissionResponse>> UpdateAsync(Guid id, UpdateAdmissionRequest request, Guid? actorId, CancellationToken cancellationToken);

    Task<Result<AdmissionResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<PagedResult<AdmissionResponse>> GetPagedAsync(AdmissionListQuery query, CancellationToken cancellationToken);

    Task<Result<AdmissionResponse>> TransferBedAsync(Guid id, TransferBedRequest request, Guid? actorId, CancellationToken cancellationToken);

    Task<Result<AdmissionResponse>> DischargeAsync(Guid id, DischargeAdmissionRequest request, Guid? actorId, CancellationToken cancellationToken);
}

internal class AdmissionService : IAdmissionService
{
    private readonly IAdmissionRepository _repository;
    private readonly IWardRepository _wardRepository;
    private readonly IBedRepository _bedRepository;
    private readonly IAdmissionIdentifierGenerator _identifierGenerator;
    private readonly IPatientService _patientService;
    private readonly IDepartmentService _departmentService;
    private readonly IConsultantService _consultantService;

    public AdmissionService(
        IAdmissionRepository repository,
        IWardRepository wardRepository,
        IBedRepository bedRepository,
        IAdmissionIdentifierGenerator identifierGenerator,
        IPatientService patientService,
        IDepartmentService departmentService,
        IConsultantService consultantService)
    {
        _repository = repository;
        _wardRepository = wardRepository;
        _bedRepository = bedRepository;
        _identifierGenerator = identifierGenerator;
        _patientService = patientService;
        _departmentService = departmentService;
        _consultantService = consultantService;
    }

    public async Task<Result<AdmissionResponse>> CreateAsync(CreateAdmissionRequest request, Guid? actorId, CancellationToken cancellationToken)
    {
        var patientResult = await _patientService.GetByIdAsync(request.PatientId, cancellationToken);
        if (!patientResult.IsSuccess)
        {
            return Result<AdmissionResponse>.Failure(IPDErrorCodes.InvalidPatient, $"Patient '{request.PatientId}' was not found.");
        }

        if (!(await _departmentService.GetByIdAsync(request.DepartmentId, cancellationToken)).IsSuccess)
        {
            return Result<AdmissionResponse>.Failure(IPDErrorCodes.InvalidDepartment, $"Department '{request.DepartmentId}' was not found.");
        }

        if (!(await _consultantService.GetByIdAsync(request.ConsultantId, cancellationToken)).IsSuccess)
        {
            return Result<AdmissionResponse>.Failure(IPDErrorCodes.InvalidConsultant, $"Consultant '{request.ConsultantId}' was not found.");
        }

        if (await _wardRepository.GetByIdAsync(request.WardId, cancellationToken) is null)
        {
            return Result<AdmissionResponse>.Failure(IPDErrorCodes.InvalidWard, $"Ward '{request.WardId}' was not found.");
        }

        var bed = await _bedRepository.GetByIdAsync(request.BedId, cancellationToken);
        if (bed is null || bed.WardId != request.WardId)
        {
            return Result<AdmissionResponse>.Failure(IPDErrorCodes.InvalidBed, $"Bed '{request.BedId}' was not found in ward '{request.WardId}'.");
        }

        if (bed.Status != BedStatus.Available)
        {
            return Result<AdmissionResponse>.Failure(IPDErrorCodes.BedNotAvailable, $"Bed '{bed.BedNumber}' is not available.");
        }

        if (await _repository.GetActiveByPatientIdAsync(request.PatientId, cancellationToken) is not null)
        {
            return Result<AdmissionResponse>.Failure(IPDErrorCodes.PatientAlreadyAdmitted, "This patient already has an active admission.");
        }

        var admissionNumber = await _identifierGenerator.NextAdmissionNumberAsync(cancellationToken);

        var admission = Admission.Create(
            admissionNumber,
            request.PatientId,
            request.DepartmentId,
            request.ConsultantId,
            request.WardId,
            request.BedId,
            request.AdmissionDateTime ?? DateTime.UtcNow,
            request.AdmissionType,
            request.ReasonForAdmission,
            actorId);

        bed.SetStatus(BedStatus.Occupied, actorId);

        await _repository.AddAsync(admission, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        return Result<AdmissionResponse>.Success(await BuildResponseAsync(admission, cancellationToken));
    }

    public async Task<Result<AdmissionResponse>> UpdateAsync(Guid id, UpdateAdmissionRequest request, Guid? actorId, CancellationToken cancellationToken)
    {
        var admission = await _repository.GetByIdAsync(id, cancellationToken);
        if (admission is null)
        {
            return Result<AdmissionResponse>.Failure(IPDErrorCodes.NotFound, $"Admission '{id}' was not found.");
        }

        if (!(await _departmentService.GetByIdAsync(request.DepartmentId, cancellationToken)).IsSuccess)
        {
            return Result<AdmissionResponse>.Failure(IPDErrorCodes.InvalidDepartment, $"Department '{request.DepartmentId}' was not found.");
        }

        if (!(await _consultantService.GetByIdAsync(request.ConsultantId, cancellationToken)).IsSuccess)
        {
            return Result<AdmissionResponse>.Failure(IPDErrorCodes.InvalidConsultant, $"Consultant '{request.ConsultantId}' was not found.");
        }

        admission.Update(request.DepartmentId, request.ConsultantId, request.AdmissionType, request.ReasonForAdmission, actorId);
        await _repository.SaveChangesAsync(cancellationToken);

        return Result<AdmissionResponse>.Success(await BuildResponseAsync(admission, cancellationToken));
    }

    public async Task<Result<AdmissionResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var admission = await _repository.GetByIdAsync(id, cancellationToken);
        return admission is null
            ? Result<AdmissionResponse>.Failure(IPDErrorCodes.NotFound, $"Admission '{id}' was not found.")
            : Result<AdmissionResponse>.Success(await BuildResponseAsync(admission, cancellationToken));
    }

    public async Task<PagedResult<AdmissionResponse>> GetPagedAsync(AdmissionListQuery query, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _repository.GetPagedAsync(query, cancellationToken);

        // MVP-scale N+1 lookups (one Patient/Consultant/Ward/Bed round-trip per row) to
        // denormalize display fields — acceptable at IPD's current admitted-patient volumes;
        // revisit with a bulk lookup if this list grows large enough to matter.
        var responses = new List<AdmissionResponse>(items.Count);
        foreach (var admission in items)
        {
            responses.Add(await BuildResponseAsync(admission, cancellationToken));
        }

        return new PagedResult<AdmissionResponse>(responses, query.Page, query.PageSize, totalCount);
    }

    public async Task<Result<AdmissionResponse>> TransferBedAsync(Guid id, TransferBedRequest request, Guid? actorId, CancellationToken cancellationToken)
    {
        var admission = await _repository.GetByIdAsync(id, cancellationToken);
        if (admission is null)
        {
            return Result<AdmissionResponse>.Failure(IPDErrorCodes.NotFound, $"Admission '{id}' was not found.");
        }

        if (admission.Status != AdmissionStatus.Admitted)
        {
            return Result<AdmissionResponse>.Failure(IPDErrorCodes.AdmissionAlreadyDischarged, "Only an active admission can be transferred.");
        }

        if (await _wardRepository.GetByIdAsync(request.NewWardId, cancellationToken) is null)
        {
            return Result<AdmissionResponse>.Failure(IPDErrorCodes.InvalidWard, $"Ward '{request.NewWardId}' was not found.");
        }

        var newBed = await _bedRepository.GetByIdAsync(request.NewBedId, cancellationToken);
        if (newBed is null || newBed.WardId != request.NewWardId)
        {
            return Result<AdmissionResponse>.Failure(IPDErrorCodes.InvalidBed, $"Bed '{request.NewBedId}' was not found in ward '{request.NewWardId}'.");
        }

        if (newBed.Status != BedStatus.Available)
        {
            return Result<AdmissionResponse>.Failure(IPDErrorCodes.BedNotAvailable, $"Bed '{newBed.BedNumber}' is not available.");
        }

        var oldBed = await _bedRepository.GetByIdAsync(admission.BedId, cancellationToken);
        oldBed?.SetStatus(BedStatus.Available, actorId);
        newBed.SetStatus(BedStatus.Occupied, actorId);

        admission.TransferBed(request.NewWardId, request.NewBedId, actorId);
        await _repository.SaveChangesAsync(cancellationToken);

        return Result<AdmissionResponse>.Success(await BuildResponseAsync(admission, cancellationToken));
    }

    public async Task<Result<AdmissionResponse>> DischargeAsync(Guid id, DischargeAdmissionRequest request, Guid? actorId, CancellationToken cancellationToken)
    {
        var admission = await _repository.GetByIdAsync(id, cancellationToken);
        if (admission is null)
        {
            return Result<AdmissionResponse>.Failure(IPDErrorCodes.NotFound, $"Admission '{id}' was not found.");
        }

        if (admission.Status != AdmissionStatus.Admitted)
        {
            return Result<AdmissionResponse>.Failure(IPDErrorCodes.AdmissionAlreadyDischarged, "This admission has already been discharged.");
        }

        if (request.DischargeDateTime < admission.AdmissionDateTime)
        {
            return Result<AdmissionResponse>.Failure(IPDErrorCodes.InvalidDischargeDate, "Discharge date/time cannot be before the admission date/time.");
        }

        var bed = await _bedRepository.GetByIdAsync(admission.BedId, cancellationToken);
        bed?.SetStatus(BedStatus.Available, actorId);

        admission.Discharge(request.DischargeDateTime, request.DischargeType, request.FinalDiagnosis, request.DischargeNotes, request.FollowUpAdvice, actorId);
        await _repository.SaveChangesAsync(cancellationToken);

        return Result<AdmissionResponse>.Success(await BuildResponseAsync(admission, cancellationToken));
    }

    private async Task<AdmissionResponse> BuildResponseAsync(Admission admission, CancellationToken cancellationToken)
    {
        var patientResult = await _patientService.GetByIdAsync(admission.PatientId, cancellationToken);
        var consultantResult = await _consultantService.GetByIdAsync(admission.ConsultantId, cancellationToken);
        var ward = await _wardRepository.GetByIdAsync(admission.WardId, cancellationToken);
        var bed = await _bedRepository.GetByIdAsync(admission.BedId, cancellationToken);

        var patient = patientResult.Value;

        return new AdmissionResponse
        {
            Id = admission.Id,
            AdmissionNumber = admission.AdmissionNumber,
            PatientId = admission.PatientId,
            Uhid = patient?.Uhid ?? string.Empty,
            PatientName = patient is null ? string.Empty : $"{patient.FirstName} {patient.LastName}",
            Age = patient?.Age ?? 0,
            Gender = patient?.Gender.ToString() ?? string.Empty,
            DepartmentId = admission.DepartmentId,
            ConsultantId = admission.ConsultantId,
            ConsultantName = consultantResult.Value?.Name ?? string.Empty,
            WardId = admission.WardId,
            WardName = ward?.Name ?? string.Empty,
            BedId = admission.BedId,
            BedNumber = bed?.BedNumber ?? string.Empty,
            AdmissionDateTime = admission.AdmissionDateTime,
            AdmissionType = admission.AdmissionType,
            ReasonForAdmission = admission.ReasonForAdmission,
            Status = admission.Status,
            DischargeDateTime = admission.DischargeDateTime,
            DischargeType = admission.DischargeType,
            FinalDiagnosis = admission.FinalDiagnosis,
            DischargeNotes = admission.DischargeNotes,
            FollowUpAdvice = admission.FollowUpAdvice,
            CreatedAt = admission.CreatedAt,
            UpdatedAt = admission.UpdatedAt,
        };
    }
}
