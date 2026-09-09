using HMS.Modules.IPD.Application.Abstractions;
using HMS.Modules.IPD.Application.Mapping;
using HMS.Modules.IPD.Contracts;
using HMS.Modules.IPD.Domain;
using HMS.Shared.Kernel;

namespace HMS.Modules.IPD.Application;

/// <summary>
/// Public (not internal): AdmissionAdvancesController — which ASP.NET Core requires to be a
/// public class with a public constructor for controller discovery/DI activation — takes this
/// as a constructor dependency; a public constructor cannot have an internal parameter type
/// (CS0051).
/// </summary>
public interface IAdmissionAdvanceService
{
    Task<Result<AdmissionAdvanceResponse>> CreateAsync(Guid admissionId, CreateAdmissionAdvanceRequest request, Guid? actorId, CancellationToken cancellationToken);

    Task<Result<IReadOnlyList<AdmissionAdvanceResponse>>> GetByAdmissionIdAsync(Guid admissionId, CancellationToken cancellationToken);
}

internal class AdmissionAdvanceService : IAdmissionAdvanceService
{
    private readonly IAdmissionAdvanceRepository _repository;
    private readonly IAdmissionRepository _admissionRepository;

    public AdmissionAdvanceService(IAdmissionAdvanceRepository repository, IAdmissionRepository admissionRepository)
    {
        _repository = repository;
        _admissionRepository = admissionRepository;
    }

    public async Task<Result<AdmissionAdvanceResponse>> CreateAsync(Guid admissionId, CreateAdmissionAdvanceRequest request, Guid? actorId, CancellationToken cancellationToken)
    {
        if (await _admissionRepository.GetByIdAsync(admissionId, cancellationToken) is null)
        {
            return Result<AdmissionAdvanceResponse>.Failure(IPDErrorCodes.NotFound, $"Admission '{admissionId}' was not found.");
        }

        var advance = AdmissionAdvance.Create(admissionId, request.Amount, request.Method, request.ReferenceNumber, request.Remarks, actorId);

        await _repository.AddAsync(advance, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        return Result<AdmissionAdvanceResponse>.Success(advance.ToResponse());
    }

    public async Task<Result<IReadOnlyList<AdmissionAdvanceResponse>>> GetByAdmissionIdAsync(Guid admissionId, CancellationToken cancellationToken)
    {
        if (await _admissionRepository.GetByIdAsync(admissionId, cancellationToken) is null)
        {
            return Result<IReadOnlyList<AdmissionAdvanceResponse>>.Failure(IPDErrorCodes.NotFound, $"Admission '{admissionId}' was not found.");
        }

        var advances = await _repository.GetByAdmissionIdAsync(admissionId, cancellationToken);
        return Result<IReadOnlyList<AdmissionAdvanceResponse>>.Success(advances.Select(a => a.ToResponse()).ToList());
    }
}
