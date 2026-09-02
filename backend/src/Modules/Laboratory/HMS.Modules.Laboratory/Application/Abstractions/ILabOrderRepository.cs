using HMS.Modules.Laboratory.Contracts;
using HMS.Modules.Laboratory.Domain;

namespace HMS.Modules.Laboratory.Application.Abstractions;

/// <summary>
/// Defined here (Application) and implemented in Infrastructure, per the dependency
/// inversion rule in docs/DeveloperHandbook.md — Application never references EF Core types.
/// </summary>
internal interface ILabOrderRepository
{
    Task AddAsync(LabOrder order, CancellationToken cancellationToken);

    /// <summary>Always includes Items -> Parameters and Items -> Events — a LabOrder is never
    /// meaningfully read without its full graph, mirroring Billing's IInvoiceRepository.GetByIdAsync
    /// "always Include Items" precedent.</summary>
    Task<LabOrder?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>For the idempotency check in LabOrderService.CreateFromInvoiceAsync — a retried
    /// call for the same InvoiceId must return the existing order, never duplicate.</summary>
    Task<LabOrder?> GetByInvoiceIdAsync(Guid invoiceId, CancellationToken cancellationToken);

    /// <summary>Loads the full order (same graph as GetByIdAsync) that owns the given item —
    /// every one of LabOrderService's per-item mutator methods needs the whole aggregate to
    /// mutate the one item and return a consistent, fully up-to-date LabOrderResponse.</summary>
    Task<LabOrder?> GetByItemIdAsync(Guid itemId, CancellationToken cancellationToken);

    Task<(IReadOnlyList<LabOrder> Items, int TotalCount)> GetPagedAsync(LabOrderListQuery query, CancellationToken cancellationToken);

    Task<IReadOnlyList<LabOrder>> GetByPatientIdAsync(Guid patientId, CancellationToken cancellationToken);

    /// <summary>Aggregated worklist dashboard counts for the current tenant — see
    /// Contracts/LabOrderContracts.cs's LabDashboardSummaryResponse for what each tile means.</summary>
    Task<LabDashboardSummaryResponse> GetDashboardCountsAsync(CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
