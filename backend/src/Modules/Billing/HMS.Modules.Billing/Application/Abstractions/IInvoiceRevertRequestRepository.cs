using HMS.Modules.Billing.Contracts;
using HMS.Modules.Billing.Domain;

namespace HMS.Modules.Billing.Application.Abstractions;

/// <summary>Shares BillingDbContext with <see cref="IInvoiceRepository"/> (both scoped), so
/// approving a request and reverting its invoice commit in one SaveChangesAsync.</summary>
internal interface IInvoiceRevertRequestRepository
{
    Task AddAsync(InvoiceRevertRequest request, CancellationToken cancellationToken);

    Task<InvoiceRevertRequest?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Newest first.</summary>
    Task<IReadOnlyList<InvoiceRevertRequest>> GetByInvoiceIdAsync(Guid invoiceId, CancellationToken cancellationToken);

    /// <summary>Newest first; Pending requests sort by oldest first instead, so the queue reads in
    /// the order it was raised.</summary>
    Task<(IReadOnlyList<InvoiceRevertRequest> Items, int TotalCount)> GetPagedAsync(InvoiceRevertRequestListQuery query, CancellationToken cancellationToken);

    Task<int> CountPendingAsync(CancellationToken cancellationToken);

    /// <summary>Whether this invoice has a revert request still waiting for a decision — while
    /// it does, the bill takes no payment (ADR-118).</summary>
    Task<bool> HasPendingAsync(Guid invoiceId, CancellationToken cancellationToken);

    /// <summary>The reasons given on the most recent <paramref name="take"/> requests, newest first.</summary>
    Task<IReadOnlyList<string>> GetRecentReasonsAsync(int take, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
