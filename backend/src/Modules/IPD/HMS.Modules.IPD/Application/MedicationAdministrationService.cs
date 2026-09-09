using HMS.Modules.IPD.Application.Abstractions;
using HMS.Modules.IPD.Application.Mapping;
using HMS.Modules.IPD.Contracts;
using HMS.Modules.IPD.Domain;
using HMS.Shared.Kernel;

namespace HMS.Modules.IPD.Application;

/// <summary>
/// Public (not internal): MedicationOrdersController — which ASP.NET Core requires to be a
/// public class with a public constructor for controller discovery/DI activation — takes
/// this as a constructor dependency; a public constructor cannot have an internal parameter
/// type (CS0051).
/// </summary>
public interface IMedicationAdministrationService
{
    Task<Result<MedicationAdministrationResponse>> CreateAsync(Guid admissionId, Guid medicationOrderId, CreateMedicationAdministrationRequest request, Guid? actorId, CancellationToken cancellationToken);

    Task<Result<IReadOnlyList<MedicationAdministrationResponse>>> GetByMedicationOrderIdAsync(Guid admissionId, Guid medicationOrderId, CancellationToken cancellationToken);
}

internal class MedicationAdministrationService : IMedicationAdministrationService
{
    private readonly IMedicationAdministrationRepository _repository;
    private readonly IMedicationOrderRepository _orderRepository;

    public MedicationAdministrationService(IMedicationAdministrationRepository repository, IMedicationOrderRepository orderRepository)
    {
        _repository = repository;
        _orderRepository = orderRepository;
    }

    public async Task<Result<MedicationAdministrationResponse>> CreateAsync(Guid admissionId, Guid medicationOrderId, CreateMedicationAdministrationRequest request, Guid? actorId, CancellationToken cancellationToken)
    {
        var orderResult = await ValidateOrderAsync(admissionId, medicationOrderId, cancellationToken);
        if (!orderResult.IsSuccess)
        {
            return Result<MedicationAdministrationResponse>.Failure(orderResult.ErrorCode!, orderResult.Error!);
        }

        var administration = MedicationAdministration.Create(
            medicationOrderId,
            request.ScheduledTime,
            request.WasGiven,
            request.AdministeredAt,
            request.Reason,
            request.Remarks,
            actorId,
            actorId);

        await _repository.AddAsync(administration, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        return Result<MedicationAdministrationResponse>.Success(administration.ToResponse());
    }

    public async Task<Result<IReadOnlyList<MedicationAdministrationResponse>>> GetByMedicationOrderIdAsync(Guid admissionId, Guid medicationOrderId, CancellationToken cancellationToken)
    {
        var orderResult = await ValidateOrderAsync(admissionId, medicationOrderId, cancellationToken);
        if (!orderResult.IsSuccess)
        {
            return Result<IReadOnlyList<MedicationAdministrationResponse>>.Failure(orderResult.ErrorCode!, orderResult.Error!);
        }

        var administrations = await _repository.GetByMedicationOrderIdAsync(medicationOrderId, cancellationToken);
        return Result<IReadOnlyList<MedicationAdministrationResponse>>.Success(administrations.Select(a => a.ToResponse()).ToList());
    }

    private async Task<Result<MedicationOrder>> ValidateOrderAsync(Guid admissionId, Guid medicationOrderId, CancellationToken cancellationToken)
    {
        var order = await _orderRepository.GetByIdAsync(medicationOrderId, cancellationToken);
        if (order is null || order.AdmissionId != admissionId)
        {
            return Result<MedicationOrder>.Failure(IPDErrorCodes.MedicationOrderNotFound, $"Medication order '{medicationOrderId}' was not found for admission '{admissionId}'.");
        }

        return Result<MedicationOrder>.Success(order);
    }
}
