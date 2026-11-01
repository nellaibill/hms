using HMS.Modules.Billing.Application.Abstractions;
using HMS.Modules.Billing.Contracts;
using HMS.Modules.Billing.Domain;
using Microsoft.EntityFrameworkCore;

namespace HMS.Modules.Billing.Infrastructure.Repositories;

internal class InvoiceDiscountRequestRepository : IInvoiceDiscountRequestRepository
{
    private readonly BillingDbContext _dbContext;

    public InvoiceDiscountRequestRepository(BillingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(InvoiceDiscountRequest request, CancellationToken cancellationToken)
        => await _dbContext.InvoiceDiscountRequests.AddAsync(request, cancellationToken);

    public Task<InvoiceDiscountRequest?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => WithDetails(_dbContext.InvoiceDiscountRequests)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

    public Task<InvoiceDiscountRequest?> GetByApprovalIdAsync(Guid approvalId, CancellationToken cancellationToken)
        => WithDetails(_dbContext.InvoiceDiscountRequests)
            .FirstOrDefaultAsync(r => r.Approvals.Any(a => a.Id == approvalId), cancellationToken);

    public async Task<IReadOnlyList<InvoiceDiscountRequest>> GetByInvoiceIdAsync(Guid invoiceId, CancellationToken cancellationToken)
        => await WithDetails(_dbContext.InvoiceDiscountRequests)
            .Where(r => r.InvoiceId == invoiceId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(cancellationToken);

    public async Task<(IReadOnlyList<InvoiceDiscountRequest> Items, int TotalCount)> GetPagedAsync(InvoiceDiscountRequestListQuery query, CancellationToken cancellationToken)
    {
        var requests = _dbContext.InvoiceDiscountRequests.AsNoTracking();

        if (query.Status is { } status)
        {
            requests = requests.Where(r => r.Status == status);
        }

        requests = query.Status == InvoiceDiscountRequestStatus.Pending
            ? requests.OrderBy(r => r.CreatedAt)
            : requests.OrderByDescending(r => r.CreatedAt);

        var totalCount = await requests.CountAsync(cancellationToken);
        var items = await WithDetails(requests)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<(IReadOnlyList<InvoiceDiscountRequest> Items, int TotalCount)> GetPagedForConsultantAsync(Guid consultantId, InvoiceDiscountRequestListQuery query, CancellationToken cancellationToken)
    {
        var requests = _dbContext.InvoiceDiscountRequests.AsNoTracking();

        requests = query.Status is { } status
            ? requests.Where(r => r.Approvals.Any(a => a.ConsultantId == consultantId && a.Status == status))
            : requests.Where(r => r.Approvals.Any(a => a.ConsultantId == consultantId));

        requests = query.Status == InvoiceDiscountRequestStatus.Pending
            ? requests.OrderBy(r => r.CreatedAt)
            : requests.OrderByDescending(r => r.CreatedAt);

        var totalCount = await requests.CountAsync(cancellationToken);
        var items = await WithDetails(requests)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public Task<int> CountPendingAsync(CancellationToken cancellationToken)
        => _dbContext.InvoiceDiscountRequests.CountAsync(r => r.Status == InvoiceDiscountRequestStatus.Pending, cancellationToken);

    public Task<int> CountPendingForConsultantAsync(Guid consultantId, CancellationToken cancellationToken)
        => _dbContext.InvoiceDiscountRequests.CountAsync(
            r => r.Approvals.Any(a => a.ConsultantId == consultantId && a.Status == InvoiceDiscountRequestStatus.Pending),
            cancellationToken);

    private static IQueryable<InvoiceDiscountRequest> WithDetails(IQueryable<InvoiceDiscountRequest> requests)
        => requests.Include(r => r.Lines).Include(r => r.Approvals);

    public Task SaveChangesAsync(CancellationToken cancellationToken)
        => _dbContext.SaveChangesAsync(cancellationToken);
}
