using HMS.Modules.Billing.Application.Abstractions;
using HMS.Modules.Billing.Contracts;
using HMS.Modules.Billing.Domain;
using Microsoft.EntityFrameworkCore;

namespace HMS.Modules.Billing.Infrastructure.Repositories;

internal class InvoiceRepository : IInvoiceRepository
{
    private readonly BillingDbContext _dbContext;

    public InvoiceRepository(BillingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(Invoice invoice, CancellationToken cancellationToken)
        => await _dbContext.Invoices.AddAsync(invoice, cancellationToken);

    public Task<Invoice?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => _dbContext.Invoices.Include(i => i.Items).FirstOrDefaultAsync(i => i.Id == id, cancellationToken);

    public async Task<(IReadOnlyList<Invoice> Items, int TotalCount)> GetPagedAsync(InvoiceListQuery query, CancellationToken cancellationToken)
    {
        var invoices = _dbContext.Invoices.Include(i => i.Items).AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = $"%{query.Search.Trim()}%";
            invoices = invoices.Where(i =>
                EF.Functions.ILike(i.PatientName, term) ||
                EF.Functions.ILike(i.PatientUhid, term) ||
                EF.Functions.ILike(i.InvoiceNumber, term));
        }

        // PaymentStatus is derived (every item Paid), not a stored column — mirrors
        // Domain/Invoice.cs's own PaymentStatus getter, expressed as a query predicate
        // instead so it can run server-side.
        if (query.PaymentStatus == Contracts.PaymentStatus.Paid)
        {
            invoices = invoices.Where(i => i.Items.Any() && i.Items.All(li => li.PaymentStatus == Contracts.PaymentStatus.Paid));
        }
        else if (query.PaymentStatus == Contracts.PaymentStatus.Pending)
        {
            invoices = invoices.Where(i => !i.Items.Any() || i.Items.Any(li => li.PaymentStatus != Contracts.PaymentStatus.Paid));
        }

        invoices = ApplySort(invoices, query.Sort);

        var totalCount = await invoices.CountAsync(cancellationToken);

        var items = await invoices
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<IReadOnlyList<Invoice>> GetByPatientIdAsync(Guid patientId, CancellationToken cancellationToken)
        => await _dbContext.Invoices
            .Include(i => i.Items)
            .Where(i => i.PatientId == patientId)
            .OrderByDescending(i => i.CreatedAt)
            .ToListAsync(cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken)
        => _dbContext.SaveChangesAsync(cancellationToken);

    private static IQueryable<Invoice> ApplySort(IQueryable<Invoice> invoices, string? sort)
    {
        if (string.IsNullOrWhiteSpace(sort))
        {
            return invoices.OrderByDescending(i => i.CreatedAt);
        }

        var descending = sort.StartsWith('-');
        var field = descending ? sort[1..] : sort;

        return field.ToLowerInvariant() switch
        {
            "netamount" => descending ? invoices.OrderByDescending(i => i.NetAmount) : invoices.OrderBy(i => i.NetAmount),
            "patientname" => descending ? invoices.OrderByDescending(i => i.PatientName) : invoices.OrderBy(i => i.PatientName),
            "createdat" => descending ? invoices.OrderByDescending(i => i.CreatedAt) : invoices.OrderBy(i => i.CreatedAt),
            _ => invoices.OrderByDescending(i => i.CreatedAt),
        };
    }
}
