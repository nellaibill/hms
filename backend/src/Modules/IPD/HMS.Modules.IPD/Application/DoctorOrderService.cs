using HMS.Modules.IPD.Application.Abstractions;
using HMS.Modules.IPD.Application.Mapping;
using HMS.Modules.IPD.Contracts;
using HMS.Modules.IPD.Domain;
using HMS.Modules.Masters.Application;
using HMS.Shared.Kernel;
using Microsoft.Extensions.Logging;

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
    private readonly IDiagnosticServiceService _diagnosticServiceService;
    private readonly IDiagnosticTestService _diagnosticTestService;
    private readonly IConsultationTypeService _consultationTypeService;
    private readonly IAdmissionChargeService _admissionChargeService;
    private readonly ILogger<DoctorOrderService> _logger;

    public DoctorOrderService(
        IDoctorOrderRepository repository,
        IAdmissionRepository admissionRepository,
        IDiagnosticServiceService diagnosticServiceService,
        IDiagnosticTestService diagnosticTestService,
        IConsultationTypeService consultationTypeService,
        IAdmissionChargeService admissionChargeService,
        ILogger<DoctorOrderService> logger)
    {
        _repository = repository;
        _admissionRepository = admissionRepository;
        _diagnosticServiceService = diagnosticServiceService;
        _diagnosticTestService = diagnosticTestService;
        _consultationTypeService = consultationTypeService;
        _admissionChargeService = admissionChargeService;
        _logger = logger;
    }

    public async Task<Result<DoctorOrderResponse>> CreateAsync(Guid admissionId, CreateDoctorOrderRequest request, Guid? actorId, CancellationToken cancellationToken)
    {
        if (await _admissionRepository.GetByIdAsync(admissionId, cancellationToken) is null)
        {
            return Result<DoctorOrderResponse>.Failure(IPDErrorCodes.NotFound, $"Admission '{admissionId}' was not found.");
        }

        var order = DoctorOrder.Create(
            admissionId, request.OrderType, request.Description, request.Instructions, request.OrderedAt, actorId, actorId, request.CatalogItemId);

        await _repository.AddAsync(order, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        // Best-effort, not transactional with the order itself — a charge-posting failure must
        // never undo/fail an already-placed order. Mirrors IPDLabOrderService.PostChargeAsync's
        // exact shape (see ADR-064/065).
        if (order.CatalogItemId is { } catalogItemId)
        {
            await PostChargeAsync(admissionId, order.OrderType, catalogItemId, actorId, cancellationToken);
        }

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

    /// <summary>Resolves a price by OrderType from the matching Masters catalog and posts one
    /// AdmissionCharge (ChargeType.DoctorOrderCharge). Only Radiology/Procedure/Consultation
    /// have a priced catalog to resolve against — this is only ever called when
    /// order.CatalogItemId is set, which itself is only possible for those three types (the
    /// frontend only shows a catalog picker for them). Diet/Nursing/Blood/Referral orders never
    /// reach here. See ADR-065.</summary>
    private async Task PostChargeAsync(Guid admissionId, DoctorOrderType orderType, Guid catalogItemId, Guid? actorId, CancellationToken cancellationToken)
    {
        try
        {
            decimal amount;
            string remarks;

            switch (orderType)
            {
                case DoctorOrderType.Radiology:
                    var serviceResult = await _diagnosticServiceService.GetByIdAsync(catalogItemId, cancellationToken);
                    if (!serviceResult.IsSuccess)
                    {
                        _logger.LogWarning(
                            "IPD: could not resolve diagnostic service '{CatalogItemId}' to post a DoctorOrderCharge for admission '{AdmissionId}' — skipping charge.",
                            catalogItemId, admissionId);
                        return;
                    }

                    amount = serviceResult.Value!.Price;
                    remarks = $"Radiology: {serviceResult.Value.Name}";
                    break;

                case DoctorOrderType.Procedure:
                    var testResult = await _diagnosticTestService.GetByIdAsync(catalogItemId, cancellationToken);
                    if (!testResult.IsSuccess)
                    {
                        _logger.LogWarning(
                            "IPD: could not resolve diagnostic test '{CatalogItemId}' to post a DoctorOrderCharge for admission '{AdmissionId}' — skipping charge.",
                            catalogItemId, admissionId);
                        return;
                    }

                    amount = testResult.Value!.Price;
                    remarks = $"Procedure: {testResult.Value.Name}";
                    break;

                case DoctorOrderType.Consultation:
                    var consultationResult = await _consultationTypeService.GetByIdAsync(catalogItemId, cancellationToken);
                    if (!consultationResult.IsSuccess)
                    {
                        _logger.LogWarning(
                            "IPD: could not resolve consultation type '{CatalogItemId}' to post a DoctorOrderCharge for admission '{AdmissionId}' — skipping charge.",
                            catalogItemId, admissionId);
                        return;
                    }

                    if (consultationResult.Value!.Amount is not { } consultationAmount)
                    {
                        // A legitimate "no fixed fee" state (e.g. "Others / On-call"), not an
                        // error — mirrors ConsultationBillingCard.tsx's own null-handling.
                        return;
                    }

                    amount = consultationAmount;
                    remarks = $"Consultation: {consultationResult.Value.Name}";
                    break;

                default:
                    // Diet/Nursing/Blood/Referral have no priced catalog — CatalogItemId should
                    // never be set for these, but degrade gracefully rather than throw.
                    return;
            }

            var chargeResult = await _admissionChargeService.CreateAsync(
                admissionId,
                new CreateAdmissionChargeRequest { ChargeType = ChargeType.DoctorOrderCharge, Amount = amount, Remarks = remarks },
                actorId,
                cancellationToken);

            if (!chargeResult.IsSuccess)
            {
                _logger.LogWarning("IPD: failed to post a DoctorOrderCharge for admission '{AdmissionId}': {Error}", admissionId, chargeResult.Error);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "IPD: unexpected error posting a DoctorOrderCharge for admission '{AdmissionId}'.", admissionId);
        }
    }
}
