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
            // Excludes voided invoices: a voided invoice has no real payment status of its
            // own (Domain/Invoice.cs's PaymentStatus getter only speaks to Pending/Paid) and
            // must not surface as "Pending" collections work — it's shown separately via its
            // own IsVoided flag/badge, not folded into either status filter.
            invoices = invoices.Where(i => !i.IsVoided && (!i.Items.Any() || i.Items.Any(li => li.PaymentStatus != Contracts.PaymentStatus.Paid)));
        }

        invoices = ApplySort(invoices, query.Sort);

        var totalCount = await invoices.CountAsync(cancellationToken);

        var items = await invoices
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<IReadOnlyList<(DateTime CreatedAt, BillingType BillingType, decimal Total)>> GetLineTotalsSinceAsync(DateTime fromUtc, CancellationToken cancellationToken)
    {
        var rows = await _dbContext.Invoices
            .AsNoTracking()
            .Where(i => !i.IsVoided && i.CreatedAt >= fromUtc)
            .SelectMany(i => i.Items.Select(li => new { i.CreatedAt, li.BillingType, li.Total }))
            .ToListAsync(cancellationToken);

        return rows.Select(r => (r.CreatedAt, r.BillingType, r.Total)).ToList();
    }

    public async Task<IReadOnlyList<Invoice>> GetByPatientIdAsync(Guid patientId, CancellationToken cancellationToken)
        => await _dbContext.Invoices
            .Include(i => i.Items)
            .Where(i => i.PatientId == patientId)
            .OrderByDescending(i => i.CreatedAt)
            .ToListAsync(cancellationToken);

    public async Task<(IReadOnlyList<ProcedureLineItemRow> Items, int TotalCount)> GetProcedureLineItemsPagedAsync(ProcedureListQuery query, CancellationToken cancellationToken)
    {
        // Deliberately projects into an anonymous type first, not directly into
        // ProcedureLineItemRow: EF Core can't translate a Where/OrderBy applied *after* a
        // Select that already projected into a user-defined record/class — it inlines the
        // constructor call at the point of member access instead of pushing the predicate down
        // to the underlying columns, producing a runtime "could not be translated" exception
        // (confirmed live; same fix as HMS.Modules.Patients.Infrastructure.Repositories
        // .PatientVisitRepository's OPD queries). Anonymous types don't have this problem, so
        // every filter below stays on the `select new { ... }` shape all the way through to the
        // single final Select into ProcedureLineItemRow, which — as the terminal projection,
        // never followed by more query composition — is safe.
        var rows =
            from item in _dbContext.InvoiceLineItems
            join invoice in _dbContext.Invoices on item.InvoiceId equals invoice.Id
            where item.BillingType == BillingType.Procedure
            select new { item, invoice };

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = $"%{query.Search.Trim()}%";
            rows = rows.Where(r => EF.Functions.ILike(r.invoice.PatientName, term) || EF.Functions.ILike(r.invoice.PatientUhid, term));
        }

        // Model binding produces DateTime.Kind = Unspecified for a plain query-string date —
        // Npgsql rejects that against a `timestamp with time zone` column ("only UTC is
        // supported"), so it must be normalized before use — same fix as
        // PatientVisitRepository.GetPagedAsync/PatientRepository.BuildFilteredQuery.
        if (query.From.HasValue)
        {
            var from = DateTime.SpecifyKind(query.From.Value, DateTimeKind.Utc);
            rows = rows.Where(r => r.invoice.CreatedAt >= from);
        }

        if (query.To.HasValue)
        {
            var to = DateTime.SpecifyKind(query.To.Value, DateTimeKind.Utc);
            rows = rows.Where(r => r.invoice.CreatedAt <= to);
        }

        if (!string.IsNullOrWhiteSpace(query.DepartmentId))
        {
            rows = rows.Where(r => r.item.DepartmentId == query.DepartmentId);
        }

        if (!string.IsNullOrWhiteSpace(query.ConsultantId))
        {
            rows = rows.Where(r => r.item.BilledConsultantId == query.ConsultantId);
        }

        if (query.PaymentStatus.HasValue)
        {
            rows = rows.Where(r => r.item.PaymentStatus == query.PaymentStatus.Value);
        }

        var totalCount = await rows.CountAsync(cancellationToken);

        var items = await rows
            .OrderByDescending(r => r.invoice.CreatedAt)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(r => new ProcedureLineItemRow(
                r.invoice.Id,
                r.item.Id,
                r.invoice.PatientId,
                r.invoice.PatientName,
                r.invoice.PatientUhid,
                r.item.ServiceId,
                r.item.BilledConsultantId,
                r.item.DepartmentId,
                r.invoice.CreatedAt,
                r.item.PaymentStatus,
                r.item.Total))
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

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
