using HMS.Modules.Masters.Application.Abstractions;
using HMS.Modules.Masters.Contracts;
using HMS.Modules.Masters.Domain;
using Microsoft.EntityFrameworkCore;

namespace HMS.Modules.Masters.Infrastructure.Repositories;

internal class PaymentMethodRepository : IPaymentMethodRepository
{
    private readonly MastersDbContext _dbContext;

    public PaymentMethodRepository(MastersDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(PaymentMethod paymentMethod, CancellationToken cancellationToken)
        => await _dbContext.PaymentMethods.AddAsync(paymentMethod, cancellationToken);

    public Task<PaymentMethod?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => _dbContext.PaymentMethods.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public Task<bool> ExistsByCodeAsync(string methodCode, Guid? excludingId, CancellationToken cancellationToken)
        => _dbContext.PaymentMethods.AnyAsync(p => p.MethodCode == methodCode && p.Id != excludingId, cancellationToken);

    public async Task<(IReadOnlyList<PaymentMethod> Items, int TotalCount)> GetPagedAsync(PaymentMethodListQuery query, CancellationToken cancellationToken)
    {
        var methods = _dbContext.PaymentMethods.AsQueryable();

        if (query.IsActive.HasValue)
        {
            methods = methods.Where(p => p.IsActive == query.IsActive.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = $"%{query.Search.Trim()}%";
            methods = methods.Where(p => EF.Functions.ILike(p.MethodCode, term) || EF.Functions.ILike(p.MethodName, term));
        }

        methods = ApplySort(methods, query.Sort);

        var totalCount = await methods.CountAsync(cancellationToken);
        var items = await methods.Skip((query.Page - 1) * query.PageSize).Take(query.PageSize).ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
        => _dbContext.SaveChangesAsync(cancellationToken);

    private static IQueryable<PaymentMethod> ApplySort(IQueryable<PaymentMethod> methods, string? sort)
    {
        if (string.IsNullOrWhiteSpace(sort))
        {
            return methods.OrderBy(p => p.MethodName);
        }

        var descending = sort.StartsWith('-');
        var field = descending ? sort[1..] : sort;

        return field.ToLowerInvariant() switch
        {
            "methodcode" => descending ? methods.OrderByDescending(p => p.MethodCode) : methods.OrderBy(p => p.MethodCode),
            "updatedat" => descending ? methods.OrderByDescending(p => p.UpdatedAt) : methods.OrderBy(p => p.UpdatedAt),
            _ => descending ? methods.OrderByDescending(p => p.MethodName) : methods.OrderBy(p => p.MethodName),
        };
    }
}
