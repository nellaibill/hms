using HMS.Modules.DischargeSummary.Application.Abstractions;
using HMS.Modules.DischargeSummary.Application.Mapping;
using HMS.Modules.DischargeSummary.Contracts;
using HMS.Modules.IPD.Application;
using HMS.Modules.IPD.Contracts;
using HMS.Modules.Patients.Application;
using HMS.Shared.Kernel;
using Microsoft.Extensions.Logging;

namespace HMS.Modules.DischargeSummary.Application;

/// <summary>
/// Orchestrates DischargeSummary use cases: expected failures (not found, admission not
/// discharged, duplicate, already finalized) are returned as <see cref="Result"/> failures,
/// never thrown — see docs/Architecture.md's exception handling strategy.
/// </summary>
internal class DischargeSummaryService : IDischargeSummaryService
{
    private readonly IDischargeSummaryRepository _repository;
    private readonly IAdmissionService _admissionService;
    private readonly IPatientService _patientService;
    private readonly ILogger<DischargeSummaryService> _logger;

    public DischargeSummaryService(
        IDischargeSummaryRepository repository,
        IAdmissionService admissionService,
        IPatientService patientService,
        ILogger<DischargeSummaryService> logger)
    {
        _repository = repository;
        _admissionService = admissionService;
        _patientService = patientService;
        _logger = logger;
    }

    public async Task<Result<DischargeSummaryResponse>> CreateDraftAsync(Guid admissionId, Guid? actorId, CancellationToken cancellationToken)
    {
        var admissionResult = await _admissionService.GetByIdAsync(admissionId, cancellationToken);
        if (!admissionResult.IsSuccess)
        {
            return Result<DischargeSummaryResponse>.Failure(
                DischargeSummaryErrorCodes.InvalidAdmission,
                $"Admission '{admissionId}' was not found.");
        }

        var admission = admissionResult.Value!;
        if (admission.Status != AdmissionStatus.Discharged)
        {
            return Result<DischargeSummaryResponse>.Failure(
                DischargeSummaryErrorCodes.AdmissionNotDischarged,
                "A discharge summary can only be created once the admission has been discharged.");
        }

        var existing = await _repository.GetByAdmissionIdAsync(admissionId, cancellationToken);
        if (existing is not null)
        {
            return Result<DischargeSummaryResponse>.Failure(
                DischargeSummaryErrorCodes.AlreadyExists,
                $"A discharge summary already exists for admission '{admissionId}'.");
        }

        var patientResult = await _patientService.GetByIdAsync(admission.PatientId, cancellationToken);
        if (!patientResult.IsSuccess)
        {
            return Result<DischargeSummaryResponse>.Failure(
                DischargeSummaryErrorCodes.InvalidPatient,
                $"Patient '{admission.PatientId}' was not found.");
        }

        var summary = Domain.DischargeSummary.Create(admissionId, admission.PatientId, admission.FinalDiagnosis, actorId);

        await _repository.AddAsync(summary, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Created discharge summary {DischargeSummaryId} for admission {AdmissionId}", summary.Id, admissionId);

        return Result<DischargeSummaryResponse>.Success(summary.ToResponse());
    }

    public async Task<Result<DischargeSummaryResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var summary = await _repository.GetByIdAsync(id, cancellationToken);
        return summary is null
            ? Result<DischargeSummaryResponse>.Failure(DischargeSummaryErrorCodes.NotFound, $"Discharge summary '{id}' was not found.")
            : Result<DischargeSummaryResponse>.Success(summary.ToResponse());
    }

    public async Task<Result<DischargeSummaryResponse>> GetByAdmissionIdAsync(Guid admissionId, CancellationToken cancellationToken)
    {
        var summary = await _repository.GetByAdmissionIdAsync(admissionId, cancellationToken);
        return summary is null
            ? Result<DischargeSummaryResponse>.Failure(DischargeSummaryErrorCodes.NotFound, $"No discharge summary exists for admission '{admissionId}'.")
            : Result<DischargeSummaryResponse>.Success(summary.ToResponse());
    }

    public async Task<Result<DischargeSummaryResponse>> FinalizeAsync(Guid id, FinalizeDischargeSummaryRequest request, Guid? actorId, CancellationToken cancellationToken)
    {
        var summary = await _repository.GetByIdAsync(id, cancellationToken);
        if (summary is null)
        {
            return Result<DischargeSummaryResponse>.Failure(DischargeSummaryErrorCodes.NotFound, $"Discharge summary '{id}' was not found.");
        }

        if (summary.Status == DischargeSummaryStatus.Finalized)
        {
            return Result<DischargeSummaryResponse>.Failure(
                DischargeSummaryErrorCodes.AlreadyFinalized,
                $"Discharge summary '{id}' has already been finalized.");
        }

        summary.Finalize(
            request.PreparedByUserId,
            request.CheckedByUserId,
            request.ConsultantApprovedByUserId,
            DateTime.UtcNow,
            actorId);

        await _repository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Finalized discharge summary {DischargeSummaryId}", summary.Id);

        return Result<DischargeSummaryResponse>.Success(summary.ToResponse());
    }
}
