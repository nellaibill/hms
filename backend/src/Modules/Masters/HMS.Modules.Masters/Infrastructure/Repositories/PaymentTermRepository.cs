using HMS.Modules.Masters.Application.Abstractions;
using HMS.Modules.Masters.Contracts;
using HMS.Modules.Masters.Domain;
using Microsoft.EntityFrameworkCore;

namespace HMS.Modules.Masters.Infrastructure.Repositories;

internal class PaymentTermRepository : IPaymentTermRepository
{
    private readonly MastersDbContext _dbContext;

    public PaymentTermRepository(MastersDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(PaymentTerm paymentTerm, CancellationToken cancellationToken)
        => await _dbContext.PaymentTerms.AddAsync(paymentTerm, cancellationToken);

    public Task<PaymentTerm?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => _dbContext.PaymentTerms.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public Task<bool> ExistsByNameAsync(string termName, Guid? excludingId, CancellationToken cancellationToken)
        => _dbContext.PaymentTerms.AnyAsync(p => p.TermName == termName && p.Id != excludingId, cancellationToken);

    public async Task<(IReadOnlyList<PaymentTerm> Items, int TotalCount)> GetPagedAsync(PaymentTermListQuery query, CancellationToken cancellationToken)
    {
        var terms = _dbContext.PaymentTerms.AsQueryable();

        if (query.IsActive.HasValue)
        {
            terms = terms.Where(p => p.IsActive == query.IsActive.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = $"%{query.Search.Trim()}%";
            terms = terms.Where(p => EF.Functions.ILike(p.TermName, term));
        }

        terms = ApplySort(terms, query.Sort);

        var totalCount = await terms.CountAsync(cancellationToken);
        var items = await terms.Skip((query.Page - 1) * query.PageSize).Take(query.PageSize).ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
        => _dbContext.SaveChangesAsync(cancellationToken);

    private static IQueryable<PaymentTerm> ApplySort(IQueryable<PaymentTerm> terms, string? sort)
    {
        if (string.IsNullOrWhiteSpace(sort))
        {
            return terms.OrderBy(p => p.TermName);
        }

        var descending = sort.StartsWith('-');
        var field = descending ? sort[1..] : sort;

        return field.ToLowerInvariant() switch
        {
            "days" => descending ? terms.OrderByDescending(p => p.Days) : terms.OrderBy(p => p.Days),
            "updatedat" => descending ? terms.OrderByDescending(p => p.UpdatedAt) : terms.OrderBy(p => p.UpdatedAt),
            _ => descending ? terms.OrderByDescending(p => p.TermName) : terms.OrderBy(p => p.TermName),
        };
    }
}
