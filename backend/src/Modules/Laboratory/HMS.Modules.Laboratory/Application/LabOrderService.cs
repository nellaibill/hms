using HMS.Modules.Laboratory.Application.Abstractions;
using HMS.Modules.Laboratory.Application.Mapping;
using HMS.Modules.Laboratory.Contracts;
using HMS.Modules.Laboratory.Domain;
using HMS.Modules.Masters.Application;
using HMS.Modules.Masters.Contracts;
using HMS.Shared.Kernel;
using Microsoft.Extensions.Logging;

namespace HMS.Modules.Laboratory.Application;

/// <summary>
/// Public (not internal): LabOrdersController — which ASP.NET Core requires to be a public
/// class with a public constructor for controller discovery/DI activation — takes this as a
/// constructor dependency; a public constructor cannot have an internal parameter type
/// (CS0051). It's also the cross-module seam Billing's InvoiceService calls into (in-process,
/// best-effort) via CreateFromInvoiceAsync — see that method's own doc comment for why there's
/// deliberately no public HTTP endpoint for it.
/// </summary>
public interface ILabOrderService
{
    /// <summary>The one entry point Billing calls, right after an invoice containing at least
    /// one BillingType.Laboratory line item is persisted. Idempotent: a retried call for the
    /// same InvoiceId returns the existing order (Result.Success) rather than duplicating —
    /// see the unique index on LabOrder.InvoiceId, which backstops this at the database level
    /// too. Not exposed as a controller action: lab staff never manually create a patient
    /// billing request, only Billing does, in-process (see LabOrdersController's own doc
    /// comment).</summary>
    Task<Result<LabOrderResponse>> CreateFromInvoiceAsync(CreateLabOrderFromInvoiceRequest request, Guid? actorId, CancellationToken cancellationToken);

    Task<Result<LabOrderResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<PagedResult<LabOrderResponse>> GetPagedAsync(LabOrderListQuery query, CancellationToken cancellationToken);

    Task<Result<IReadOnlyList<LabOrderResponse>>> GetByPatientIdAsync(Guid patientId, CancellationToken cancellationToken);

    Task<Result<LabDashboardSummaryResponse>> GetDashboardSummaryAsync(CancellationToken cancellationToken);

    Task<Result<LabOrderResponse>> CollectSampleAsync(Guid itemId, CollectSampleRequest request, Guid? actorId, CancellationToken cancellationToken);

    Task<Result<LabOrderResponse>> RejectSampleAsync(Guid itemId, RejectSampleRequest request, Guid? actorId, CancellationToken cancellationToken);

    Task<Result<LabOrderResponse>> RequestRecollectionAsync(Guid itemId, Guid? actorId, CancellationToken cancellationToken);

    Task<Result<LabOrderResponse>> ReceiveSampleAsync(Guid itemId, Guid? actorId, CancellationToken cancellationToken);

    Task<Result<LabOrderResponse>> StartProcessingAsync(Guid itemId, Guid? actorId, CancellationToken cancellationToken);

    Task<Result<LabOrderResponse>> SaveResultDraftAsync(Guid itemId, SaveResultDraftRequest request, Guid? actorId, CancellationToken cancellationToken);

    Task<Result<LabOrderResponse>> SubmitForVerificationAsync(Guid itemId, Guid? actorId, CancellationToken cancellationToken);

    Task<Result<LabOrderResponse>> VerifyAsync(Guid itemId, Guid? actorId, CancellationToken cancellationToken);

    Task<Result<LabOrderResponse>> RejectForCorrectionAsync(Guid itemId, RejectForCorrectionRequest request, Guid? actorId, CancellationToken cancellationToken);

    Task<Result<LabOrderResponse>> GenerateReportAsync(Guid orderId, Guid? actorId, CancellationToken cancellationToken);

    Task<Result<LabOrderResponse>> ReleaseReportAsync(Guid orderId, Guid? actorId, CancellationToken cancellationToken);
}

internal class LabOrderService : ILabOrderService
{
    private readonly ILabOrderRepository _repository;
    private readonly ILabOrderNumberGenerator _numberGenerator;
    private readonly IDiagnosticServiceService _diagnosticServiceService;
    private readonly IDiagnosticPackageService _diagnosticPackageService;
    private readonly ILogger<LabOrderService> _logger;

    public LabOrderService(
        ILabOrderRepository repository,
        ILabOrderNumberGenerator numberGenerator,
        IDiagnosticServiceService diagnosticServiceService,
        IDiagnosticPackageService diagnosticPackageService,
        ILogger<LabOrderService> logger)
    {
        _repository = repository;
        _numberGenerator = numberGenerator;
        _diagnosticServiceService = diagnosticServiceService;
        _diagnosticPackageService = diagnosticPackageService;
        _logger = logger;
    }

    public async Task<Result<LabOrderResponse>> CreateFromInvoiceAsync(CreateLabOrderFromInvoiceRequest request, Guid? actorId, CancellationToken cancellationToken)
    {
        var existing = await _repository.GetByInvoiceIdAsync(request.InvoiceId, cancellationToken);
        if (existing is not null)
        {
            return Result<LabOrderResponse>.Success(existing.ToResponse());
        }

        var itemSpecs = new List<LabOrderItemSpec>();

        foreach (var line in request.Lines)
        {
            if (line.PackageId is { } packageId)
            {
                var packageResult = await _diagnosticPackageService.GetByIdAsync(packageId, cancellationToken);
                if (!packageResult.IsSuccess)
                {
                    _logger.LogWarning(
                        "Laboratory: package '{PackageId}' referenced by invoice line '{InvoiceLineItemId}' could not be resolved — skipping.",
                        packageId, line.InvoiceLineItemId);
                    continue;
                }

                // A package could in theory mix service types (e.g. a lab test bundled with a
                // procedure) — only the Laboratory-typed members are expanded into LabOrderItems.
                foreach (var packageItem in packageResult.Value!.Items)
                {
                    var serviceResult = await _diagnosticServiceService.GetByIdAsync(packageItem.ServiceId, cancellationToken);
                    if (!serviceResult.IsSuccess)
                    {
                        _logger.LogWarning(
                            "Laboratory: service '{ServiceId}' in package '{PackageId}' could not be resolved — skipping.",
                            packageItem.ServiceId, packageId);
                        continue;
                    }

                    if (serviceResult.Value!.ServiceType != DiagnosticTestServiceType.Laboratory)
                    {
                        continue;
                    }

                    itemSpecs.Add(new LabOrderItemSpec(
                        serviceResult.Value.Id,
                        packageId,
                        serviceResult.Value.Name,
                        line.InvoiceLineItemId,
                        line.DepartmentId,
                        line.ConsultantId,
                        null));
                }
            }
            else if (line.ServiceId is { } serviceId)
            {
                var serviceResult = await _diagnosticServiceService.GetByIdAsync(serviceId, cancellationToken);
                if (!serviceResult.IsSuccess || serviceResult.Value!.ServiceType != DiagnosticTestServiceType.Laboratory)
                {
                    return Result<LabOrderResponse>.Failure(
                        LaboratoryErrorCodes.InvalidServiceOrPackage,
                        $"Service '{serviceId}' could not be resolved as a laboratory service.");
                }

                itemSpecs.Add(new LabOrderItemSpec(
                    serviceResult.Value.Id,
                    null,
                    serviceResult.Value.Name,
                    line.InvoiceLineItemId,
                    line.DepartmentId,
                    line.ConsultantId,
                    null));
            }
        }

        if (itemSpecs.Count == 0)
        {
            return Result<LabOrderResponse>.Failure(
                LaboratoryErrorCodes.EmptyOrder,
                "No laboratory items could be resolved from the supplied invoice lines.");
        }

        var labOrderNumber = await _numberGenerator.NextLabOrderNumberAsync(cancellationToken);

        var order = LabOrder.Create(
            labOrderNumber,
            request.InvoiceId,
            request.PatientId,
            request.PatientName,
            request.PatientUhid,
            request.VisitId,
            request.Source,
            itemSpecs,
            actorId);

        await _repository.AddAsync(order, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        return Result<LabOrderResponse>.Success(order.ToResponse());
    }

    public async Task<Result<LabOrderResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var order = await _repository.GetByIdAsync(id, cancellationToken);
        return order is null
            ? Result<LabOrderResponse>.Failure(LaboratoryErrorCodes.OrderNotFound, $"Lab order '{id}' was not found.")
            : Result<LabOrderResponse>.Success(order.ToResponse());
    }

    public async Task<PagedResult<LabOrderResponse>> GetPagedAsync(LabOrderListQuery query, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _repository.GetPagedAsync(query, cancellationToken);
        return new PagedResult<LabOrderResponse>(items.Select(o => o.ToResponse()).ToList(), query.Page, query.PageSize, totalCount);
    }

    public async Task<Result<IReadOnlyList<LabOrderResponse>>> GetByPatientIdAsync(Guid patientId, CancellationToken cancellationToken)
    {
        var orders = await _repository.GetByPatientIdAsync(patientId, cancellationToken);
        return Result<IReadOnlyList<LabOrderResponse>>.Success(orders.Select(o => o.ToResponse()).ToList());
    }

    public async Task<Result<LabDashboardSummaryResponse>> GetDashboardSummaryAsync(CancellationToken cancellationToken)
    {
        var summary = await _repository.GetDashboardCountsAsync(cancellationToken);
        return Result<LabDashboardSummaryResponse>.Success(summary);
    }

    public Task<Result<LabOrderResponse>> CollectSampleAsync(Guid itemId, CollectSampleRequest request, Guid? actorId, CancellationToken cancellationToken)
        => MutateItemAsync(itemId, item => item.CollectSample(request.SampleType, request.Location, request.Quantity, request.Remarks, actorId), cancellationToken);

    public Task<Result<LabOrderResponse>> RejectSampleAsync(Guid itemId, RejectSampleRequest request, Guid? actorId, CancellationToken cancellationToken)
        => MutateItemAsync(itemId, item => item.RejectSample(request.Reason, request.Remarks, actorId), cancellationToken);

    public Task<Result<LabOrderResponse>> RequestRecollectionAsync(Guid itemId, Guid? actorId, CancellationToken cancellationToken)
        => MutateItemAsync(itemId, item => item.RequestRecollection(actorId), cancellationToken);

    public Task<Result<LabOrderResponse>> ReceiveSampleAsync(Guid itemId, Guid? actorId, CancellationToken cancellationToken)
        => MutateItemAsync(itemId, item => item.ReceiveSample(actorId), cancellationToken);

    public Task<Result<LabOrderResponse>> StartProcessingAsync(Guid itemId, Guid? actorId, CancellationToken cancellationToken)
        => MutateItemAsync(itemId, item => item.StartProcessing(actorId), cancellationToken);

    public Task<Result<LabOrderResponse>> SaveResultDraftAsync(Guid itemId, SaveResultDraftRequest request, Guid? actorId, CancellationToken cancellationToken)
        => MutateItemAsync(itemId, item => item.SaveResultDraft(
            request.Parameters.Select(p => new LabResultParameterSpec(p.ParameterName, p.ResultValue, p.Unit, p.ReferenceRange, p.Flag, p.Remarks)).ToList(),
            actorId), cancellationToken);

    public Task<Result<LabOrderResponse>> SubmitForVerificationAsync(Guid itemId, Guid? actorId, CancellationToken cancellationToken)
        => MutateItemAsync(itemId, item => item.SubmitForVerification(actorId), cancellationToken);

    public Task<Result<LabOrderResponse>> VerifyAsync(Guid itemId, Guid? actorId, CancellationToken cancellationToken)
        => MutateItemAsync(itemId, item => item.Verify(actorId), cancellationToken);

    public Task<Result<LabOrderResponse>> RejectForCorrectionAsync(Guid itemId, RejectForCorrectionRequest request, Guid? actorId, CancellationToken cancellationToken)
        => MutateItemAsync(itemId, item => item.RejectForCorrection(request.Reason, actorId), cancellationToken);

    public async Task<Result<LabOrderResponse>> GenerateReportAsync(Guid orderId, Guid? actorId, CancellationToken cancellationToken)
    {
        var order = await _repository.GetByIdAsync(orderId, cancellationToken);
        if (order is null)
        {
            return Result<LabOrderResponse>.Failure(LaboratoryErrorCodes.OrderNotFound, $"Lab order '{orderId}' was not found.");
        }

        // Pre-checked here (not just via the domain guard) so the specific error code can be
        // returned — same split as Invoice.Void/InvoiceService.VoidAsync.
        if (order.Items.Count == 0 || !order.Items.All(i => i.Status == LabOrderItemStatus.Verified))
        {
            return Result<LabOrderResponse>.Failure(
                LaboratoryErrorCodes.NotAllItemsVerified,
                "Every item on this order must be Verified before a report can be generated.");
        }

        order.GenerateReport(actorId);
        await _repository.SaveChangesAsync(cancellationToken);

        return Result<LabOrderResponse>.Success(order.ToResponse());
    }

    public async Task<Result<LabOrderResponse>> ReleaseReportAsync(Guid orderId, Guid? actorId, CancellationToken cancellationToken)
    {
        var order = await _repository.GetByIdAsync(orderId, cancellationToken);
        if (order is null)
        {
            return Result<LabOrderResponse>.Failure(LaboratoryErrorCodes.OrderNotFound, $"Lab order '{orderId}' was not found.");
        }

        if (order.ReportGeneratedAt is null)
        {
            return Result<LabOrderResponse>.Failure(LaboratoryErrorCodes.ReportNotGenerated, "The report must be generated before it can be released.");
        }

        if (order.ReportReleasedAt is not null)
        {
            return Result<LabOrderResponse>.Failure(LaboratoryErrorCodes.AlreadyReleased, "This order's report has already been released.");
        }

        order.ReleaseReport(actorId);
        await _repository.SaveChangesAsync(cancellationToken);

        return Result<LabOrderResponse>.Success(order.ToResponse());
    }

    /// <summary>Shared shape for every per-item mutator: load the owning order (full graph),
    /// find the item, apply the mutation, translate an InvalidOperationException (an illegal
    /// status transition) into a proper Result.Failure, save, and return the whole parent
    /// LabOrderResponse — never just the item — so the frontend always gets a consistent,
    /// fully up-to-date order view after any single-item action. Mirrors InvoiceService.
    /// RecordPaymentAsync/VoidAsync's own try/catch shape around Invoice's guard exceptions.</summary>
    private async Task<Result<LabOrderResponse>> MutateItemAsync(Guid itemId, Action<LabOrderItem> mutate, CancellationToken cancellationToken)
    {
        var order = await _repository.GetByItemIdAsync(itemId, cancellationToken);
        var item = order?.Items.FirstOrDefault(i => i.Id == itemId);
        if (order is null || item is null)
        {
            return Result<LabOrderResponse>.Failure(LaboratoryErrorCodes.ItemNotFound, $"Lab order item '{itemId}' was not found.");
        }

        try
        {
            mutate(item);
        }
        catch (InvalidOperationException ex)
        {
            return Result<LabOrderResponse>.Failure(LaboratoryErrorCodes.InvalidStatusTransition, ex.Message);
        }

        await _repository.SaveChangesAsync(cancellationToken);

        return Result<LabOrderResponse>.Success(order.ToResponse());
    }
}
