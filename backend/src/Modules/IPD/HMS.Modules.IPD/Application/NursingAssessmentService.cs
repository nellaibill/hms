using HMS.Modules.IPD.Application.Abstractions;
using HMS.Modules.IPD.Application.Mapping;
using HMS.Modules.IPD.Contracts;
using HMS.Modules.IPD.Domain;
using HMS.Shared.Kernel;

namespace HMS.Modules.IPD.Application;

/// <summary>
/// Public (not internal): NursingAssessmentsController — which ASP.NET Core requires to be a
/// public class with a public constructor for controller discovery/DI activation — takes
/// this as a constructor dependency; a public constructor cannot have an internal parameter
/// type (CS0051).
/// </summary>
public interface INursingAssessmentService
{
    Task<Result<NursingAssessmentResponse>> CreateAsync(Guid admissionId, CreateNursingAssessmentRequest request, Guid? actorId, CancellationToken cancellationToken);

    Task<Result<IReadOnlyList<NursingAssessmentResponse>>> GetByAdmissionIdAsync(Guid admissionId, CancellationToken cancellationToken);
}

internal class NursingAssessmentService : INursingAssessmentService
{
    private readonly INursingAssessmentRepository _repository;
    private readonly IAdmissionRepository _admissionRepository;

    public NursingAssessmentService(INursingAssessmentRepository repository, IAdmissionRepository admissionRepository)
    {
        _repository = repository;
        _admissionRepository = admissionRepository;
    }

    public async Task<Result<NursingAssessmentResponse>> CreateAsync(Guid admissionId, CreateNursingAssessmentRequest request, Guid? actorId, CancellationToken cancellationToken)
    {
        if (await _admissionRepository.GetByIdAsync(admissionId, cancellationToken) is null)
        {
            return Result<NursingAssessmentResponse>.Failure(IPDErrorCodes.NotFound, $"Admission '{admissionId}' was not found.");
        }

        var assessment = NursingAssessment.Create(
            admissionId,
            request.AssessedAt,
            request.GeneralCondition,
            request.ConsciousnessLevel,
            request.Mobility,
            request.NutritionStatus,
            request.FallRisk,
            request.PressureSoreRisk,
            request.SkinCondition,
            request.PainScore,
            request.Notes,
            request.AssessedByUserId,
            actorId);

        await _repository.AddAsync(assessment, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        return Result<NursingAssessmentResponse>.Success(assessment.ToResponse());
    }

    public async Task<Result<IReadOnlyList<NursingAssessmentResponse>>> GetByAdmissionIdAsync(Guid admissionId, CancellationToken cancellationToken)
    {
        if (await _admissionRepository.GetByIdAsync(admissionId, cancellationToken) is null)
        {
            return Result<IReadOnlyList<NursingAssessmentResponse>>.Failure(IPDErrorCodes.NotFound, $"Admission '{admissionId}' was not found.");
        }

        var assessments = await _repository.GetByAdmissionIdAsync(admissionId, cancellationToken);
        return Result<IReadOnlyList<NursingAssessmentResponse>>.Success(assessments.Select(a => a.ToResponse()).ToList());
    }
}
