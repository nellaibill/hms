using HMS.Modules.IPD.Application.Abstractions;
using HMS.Modules.IPD.Application.Mapping;
using HMS.Modules.IPD.Contracts;
using HMS.Modules.IPD.Domain;
using HMS.Shared.Kernel;

namespace HMS.Modules.IPD.Application;

/// <summary>
/// Public (not internal): VitalsController — which ASP.NET Core requires to be a public class
/// with a public constructor for controller discovery/DI activation — takes this as a
/// constructor dependency; a public constructor cannot have an internal parameter type
/// (CS0051).
/// </summary>
public interface IVitalsReadingService
{
    Task<Result<VitalsReadingResponse>> CreateAsync(Guid admissionId, CreateVitalsReadingRequest request, Guid? actorId, CancellationToken cancellationToken);

    Task<Result<IReadOnlyList<VitalsReadingResponse>>> GetByAdmissionIdAsync(Guid admissionId, CancellationToken cancellationToken);
}

internal class VitalsReadingService : IVitalsReadingService
{
    private readonly IVitalsReadingRepository _repository;
    private readonly IAdmissionRepository _admissionRepository;

    public VitalsReadingService(IVitalsReadingRepository repository, IAdmissionRepository admissionRepository)
    {
        _repository = repository;
        _admissionRepository = admissionRepository;
    }

    public async Task<Result<VitalsReadingResponse>> CreateAsync(Guid admissionId, CreateVitalsReadingRequest request, Guid? actorId, CancellationToken cancellationToken)
    {
        if (await _admissionRepository.GetByIdAsync(admissionId, cancellationToken) is null)
        {
            return Result<VitalsReadingResponse>.Failure(IPDErrorCodes.NotFound, $"Admission '{admissionId}' was not found.");
        }

        var reading = VitalsReading.Create(
            admissionId,
            request.RecordedAt,
            request.TemperatureF,
            request.PulseRate,
            request.RespiratoryRate,
            request.BloodPressureSystolic,
            request.BloodPressureDiastolic,
            request.SpO2Percent,
            request.WeightKg,
            request.HeightCm,
            request.PainScore,
            request.BloodGlucoseMgDl,
            request.RecordedByUserId,
            request.Notes,
            actorId);

        await _repository.AddAsync(reading, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        return Result<VitalsReadingResponse>.Success(reading.ToResponse());
    }

    public async Task<Result<IReadOnlyList<VitalsReadingResponse>>> GetByAdmissionIdAsync(Guid admissionId, CancellationToken cancellationToken)
    {
        if (await _admissionRepository.GetByIdAsync(admissionId, cancellationToken) is null)
        {
            return Result<IReadOnlyList<VitalsReadingResponse>>.Failure(IPDErrorCodes.NotFound, $"Admission '{admissionId}' was not found.");
        }

        var readings = await _repository.GetByAdmissionIdAsync(admissionId, cancellationToken);
        return Result<IReadOnlyList<VitalsReadingResponse>>.Success(readings.Select(r => r.ToResponse()).ToList());
    }
}
