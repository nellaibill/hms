using HMS.Modules.Billing.Application.Abstractions;
using HMS.Modules.Billing.Contracts;
using HMS.Modules.Billing.Domain;
using Microsoft.EntityFrameworkCore;

namespace HMS.Modules.Billing.Infrastructure.Repositories;

internal class InvoiceRevertRequestRepository : IInvoiceRevertRequestRepository
{
    private readonly BillingDbContext _dbContext;

    public InvoiceRevertRequestRepository(BillingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(InvoiceRevertRequest request, CancellationToken cancellationToken)
        => await _dbContext.InvoiceRevertRequests.AddAsync(request, cancellationToken);

    public Task<InvoiceRevertRequest?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => _dbContext.InvoiceRevertRequests.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

    public async Task<IReadOnlyList<InvoiceRevertRequest>> GetByInvoiceIdAsync(Guid invoiceId, CancellationToken cancellationToken)
        => await _dbContext.InvoiceRevertRequests
            .Where(r => r.InvoiceId == invoiceId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(cancellationToken);

    public async Task<(IReadOnlyList<InvoiceRevertRequest> Items, int TotalCount)> GetPagedAsync(InvoiceRevertRequestListQuery query, CancellationToken cancellationToken)
    {
        var requests = _dbContext.InvoiceRevertRequests.AsNoTracking();

        if (query.Status is { } status)
        {
            requests = requests.Where(r => r.Status == status);
        }

        requests = query.Status == InvoiceRevertRequestStatus.Pending
            ? requests.OrderBy(r => r.CreatedAt)
            : requests.OrderByDescending(r => r.CreatedAt);

        var totalCount = await requests.CountAsync(cancellationToken);
        var items = await requests
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public Task<int> CountPendingAsync(CancellationToken cancellationToken)
        => _dbContext.InvoiceRevertRequests.CountAsync(r => r.Status == InvoiceRevertRequestStatus.Pending, cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken)
        => _dbContext.SaveChangesAsync(cancellationToken);
}
