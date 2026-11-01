using HMS.Modules.Billing.Contracts;
using HMS.Modules.Billing.Domain;

namespace HMS.Modules.Billing.Application.Abstractions;

/// <summary>Shares BillingDbContext with <see cref="IInvoiceRepository"/> (both scoped), so a
/// bill and its discount request — on creation, approval or void — commit in one
/// SaveChangesAsync. Every read includes the request's lines.</summary>
internal interface IInvoiceDiscountRequestRepository
{
    Task AddAsync(InvoiceDiscountRequest request, CancellationToken cancellationToken);

    Task<InvoiceDiscountRequest?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Newest first.</summary>
    Task<IReadOnlyList<InvoiceDiscountRequest>> GetByInvoiceIdAsync(Guid invoiceId, CancellationToken cancellationToken);

    /// <summary>Newest first; Pending requests sort by oldest first instead, so the queue reads in
    /// the order it was raised.</summary>
    Task<(IReadOnlyList<InvoiceDiscountRequest> Items, int TotalCount)> GetPagedAsync(InvoiceDiscountRequestListQuery query, CancellationToken cancellationToken);

    Task<int> CountPendingAsync(CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
