using HMS.Modules.IPD.Application.Abstractions;
using HMS.Modules.IPD.Application.Mapping;
using HMS.Modules.IPD.Contracts;
using HMS.Modules.IPD.Domain;
using HMS.Shared.Kernel;

namespace HMS.Modules.IPD.Application;

/// <summary>
/// Public (not internal): DoctorOrdersController — which ASP.NET Core requires to be a public
/// class with a public constructor for controller discovery/DI activation — takes this as a
/// constructor dependency; a public constructor cannot have an internal parameter type
/// (CS0051).
/// </summary>
public interface IDoctorOrderService
{
    Task<Result<DoctorOrderResponse>> CreateAsync(Guid admissionId, CreateDoctorOrderRequest request, Guid? actorId, CancellationToken cancellationToken);

    Task<Result<IReadOnlyList<DoctorOrderResponse>>> GetByAdmissionIdAsync(Guid admissionId, CancellationToken cancellationToken);

    Task<Result<DoctorOrderResponse>> AdvanceAsync(Guid admissionId, Guid orderId, Guid? actorId, CancellationToken cancellationToken);

    Task<Result<DoctorOrderResponse>> CancelAsync(Guid admissionId, Guid orderId, CancelDoctorOrderRequest request, Guid? actorId, CancellationToken cancellationToken);
}

internal class DoctorOrderService : IDoctorOrderService
{
    private readonly IDoctorOrderRepository _repository;
    private readonly IAdmissionRepository _admissionRepository;

    public DoctorOrderService(IDoctorOrderRepository repository, IAdmissionRepository admissionRepository)
    {
        _repository = repository;
        _admissionRepository = admissionRepository;
    }

    public async Task<Result<DoctorOrderResponse>> CreateAsync(Guid admissionId, CreateDoctorOrderRequest request, Guid? actorId, CancellationToken cancellationToken)
    {
        if (await _admissionRepository.GetByIdAsync(admissionId, cancellationToken) is null)
        {
            return Result<DoctorOrderResponse>.Failure(IPDErrorCodes.NotFound, $"Admission '{admissionId}' was not found.");
        }

        var order = DoctorOrder.Create(admissionId, request.OrderType, request.Description, request.Instructions, request.OrderedAt, actorId, actorId);

        await _repository.AddAsync(order, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        return Result<DoctorOrderResponse>.Success(order.ToResponse());
    }

    public async Task<Result<IReadOnlyList<DoctorOrderResponse>>> GetByAdmissionIdAsync(Guid admissionId, CancellationToken cancellationToken)
    {
        if (await _admissionRepository.GetByIdAsync(admissionId, cancellationToken) is null)
        {
            return Result<IReadOnlyList<DoctorOrderResponse>>.Failure(IPDErrorCodes.NotFound, $"Admission '{admissionId}' was not found.");
        }

        var orders = await _repository.GetByAdmissionIdAsync(admissionId, cancellationToken);
        return Result<IReadOnlyList<DoctorOrderResponse>>.Success(orders.Select(o => o.ToResponse()).ToList());
    }

    public async Task<Result<DoctorOrderResponse>> AdvanceAsync(Guid admissionId, Guid orderId, Guid? actorId, CancellationToken cancellationToken)
    {
        var order = await _repository.GetByIdAsync(orderId, cancellationToken);
        if (order is null || order.AdmissionId != admissionId)
        {
            return Result<DoctorOrderResponse>.Failure(IPDErrorCodes.DoctorOrderNotFound, $"Doctor order '{orderId}' was not found for admission '{admissionId}'.");
        }

        if (order.Status is DoctorOrderStatus.Completed or DoctorOrderStatus.Cancelled)
        {
            return Result<DoctorOrderResponse>.Failure(
                IPDErrorCodes.InvalidOrderStatusTransition,
                $"Cannot advance a doctor order that is already {order.Status}.");
        }

        order.Advance(actorId);
        await _repository.SaveChangesAsync(cancellationToken);

        return Result<DoctorOrderResponse>.Success(order.ToResponse());
    }

    public async Task<Result<DoctorOrderResponse>> CancelAsync(Guid admissionId, Guid orderId, CancelDoctorOrderRequest request, Guid? actorId, CancellationToken cancellationToken)
    {
        var order = await _repository.GetByIdAsync(orderId, cancellationToken);
        if (order is null || order.AdmissionId != admissionId)
        {
            return Result<DoctorOrderResponse>.Failure(IPDErrorCodes.DoctorOrderNotFound, $"Doctor order '{orderId}' was not found for admission '{admissionId}'.");
        }

        if (order.Status is DoctorOrderStatus.Completed or DoctorOrderStatus.Cancelled)
        {
            return Result<DoctorOrderResponse>.Failure(
                IPDErrorCodes.InvalidOrderStatusTransition,
                $"Cannot cancel a doctor order that is already {order.Status}.");
        }

        order.Cancel(request.Reason, actorId);
        await _repository.SaveChangesAsync(cancellationToken);

        return Result<DoctorOrderResponse>.Success(order.ToResponse());
    }
}
