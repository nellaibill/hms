using HMS.Modules.IPD.Application.Abstractions;
using HMS.Modules.IPD.Application.Mapping;
using HMS.Modules.IPD.Contracts;
using HMS.Modules.IPD.Domain;
using HMS.Shared.Kernel;

namespace HMS.Modules.IPD.Application;

/// <summary>
/// Public (not internal): AdmissionChargesController — which ASP.NET Core requires to be a
/// public class with a public constructor for controller discovery/DI activation — takes
/// this as a constructor dependency; a public constructor cannot have an internal parameter
/// type (CS0051).
/// </summary>
public interface IAdmissionChargeService
{
    Task<Result<AdmissionChargeResponse>> CreateAsync(Guid admissionId, CreateAdmissionChargeRequest request, Guid? actorId, CancellationToken cancellationToken);

    Task<Result<IReadOnlyList<AdmissionChargeResponse>>> GetByAdmissionIdAsync(Guid admissionId, CancellationToken cancellationToken);
}

internal class AdmissionChargeService : IAdmissionChargeService
{
    private readonly IAdmissionChargeRepository _repository;
    private readonly IAdmissionRepository _admissionRepository;

    public AdmissionChargeService(IAdmissionChargeRepository repository, IAdmissionRepository admissionRepository)
    {
        _repository = repository;
        _admissionRepository = admissionRepository;
    }

    public async Task<Result<AdmissionChargeResponse>> CreateAsync(Guid admissionId, CreateAdmissionChargeRequest request, Guid? actorId, CancellationToken cancellationToken)
    {
        if (await _admissionRepository.GetByIdAsync(admissionId, cancellationToken) is null)
        {
            return Result<AdmissionChargeResponse>.Failure(IPDErrorCodes.NotFound, $"Admission '{admissionId}' was not found.");
        }

        var charge = AdmissionCharge.Create(admissionId, request.ChargeType, request.Amount, request.Remarks, actorId);

        await _repository.AddAsync(charge, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        return Result<AdmissionChargeResponse>.Success(charge.ToResponse());
    }

    public async Task<Result<IReadOnlyList<AdmissionChargeResponse>>> GetByAdmissionIdAsync(Guid admissionId, CancellationToken cancellationToken)
    {
        if (await _admissionRepository.GetByIdAsync(admissionId, cancellationToken) is null)
        {
            return Result<IReadOnlyList<AdmissionChargeResponse>>.Failure(IPDErrorCodes.NotFound, $"Admission '{admissionId}' was not found.");
        }

        var charges = await _repository.GetByAdmissionIdAsync(admissionId, cancellationToken);
        return Result<IReadOnlyList<AdmissionChargeResponse>>.Success(charges.Select(c => c.ToResponse()).ToList());
    }
}
