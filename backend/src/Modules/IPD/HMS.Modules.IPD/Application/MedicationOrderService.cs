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
public interface IMedicationOrderService
{
    Task<Result<MedicationOrderResponse>> CreateAsync(Guid admissionId, CreateMedicationOrderRequest request, Guid? actorId, CancellationToken cancellationToken);

    Task<Result<IReadOnlyList<MedicationOrderResponse>>> GetByAdmissionIdAsync(Guid admissionId, CancellationToken cancellationToken);

    Task<Result<MedicationOrderResponse>> DiscontinueAsync(Guid admissionId, Guid orderId, DiscontinueMedicationOrderRequest request, Guid? actorId, CancellationToken cancellationToken);
}

internal class MedicationOrderService : IMedicationOrderService
{
    private readonly IMedicationOrderRepository _repository;
    private readonly IAdmissionRepository _admissionRepository;

    public MedicationOrderService(IMedicationOrderRepository repository, IAdmissionRepository admissionRepository)
    {
        _repository = repository;
        _admissionRepository = admissionRepository;
    }

    public async Task<Result<MedicationOrderResponse>> CreateAsync(Guid admissionId, CreateMedicationOrderRequest request, Guid? actorId, CancellationToken cancellationToken)
    {
        if (await _admissionRepository.GetByIdAsync(admissionId, cancellationToken) is null)
        {
            return Result<MedicationOrderResponse>.Failure(IPDErrorCodes.NotFound, $"Admission '{admissionId}' was not found.");
        }

        var order = MedicationOrder.Create(
            admissionId,
            request.DrugName,
            request.Dose,
            request.Route,
            request.Frequency,
            request.StartDate,
            request.EndDate,
            request.Instructions,
            request.OrderedAt,
            actorId,
            actorId);

        await _repository.AddAsync(order, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        return Result<MedicationOrderResponse>.Success(order.ToResponse());
    }

    public async Task<Result<IReadOnlyList<MedicationOrderResponse>>> GetByAdmissionIdAsync(Guid admissionId, CancellationToken cancellationToken)
    {
        if (await _admissionRepository.GetByIdAsync(admissionId, cancellationToken) is null)
        {
            return Result<IReadOnlyList<MedicationOrderResponse>>.Failure(IPDErrorCodes.NotFound, $"Admission '{admissionId}' was not found.");
        }

        var orders = await _repository.GetByAdmissionIdAsync(admissionId, cancellationToken);
        return Result<IReadOnlyList<MedicationOrderResponse>>.Success(orders.Select(o => o.ToResponse()).ToList());
    }

    public async Task<Result<MedicationOrderResponse>> DiscontinueAsync(Guid admissionId, Guid orderId, DiscontinueMedicationOrderRequest request, Guid? actorId, CancellationToken cancellationToken)
    {
        var order = await _repository.GetByIdAsync(orderId, cancellationToken);
        if (order is null || order.AdmissionId != admissionId)
        {
            return Result<MedicationOrderResponse>.Failure(IPDErrorCodes.MedicationOrderNotFound, $"Medication order '{orderId}' was not found for admission '{admissionId}'.");
        }

        if (order.Status == MedicationOrderStatus.Discontinued)
        {
            return Result<MedicationOrderResponse>.Failure(IPDErrorCodes.MedicationOrderAlreadyDiscontinued, $"Medication order '{orderId}' has already been discontinued.");
        }

        order.Discontinue(request.Reason, actorId);
        await _repository.SaveChangesAsync(cancellationToken);

        return Result<MedicationOrderResponse>.Success(order.ToResponse());
    }
}
