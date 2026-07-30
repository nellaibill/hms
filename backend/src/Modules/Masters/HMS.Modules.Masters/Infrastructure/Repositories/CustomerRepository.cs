using HMS.Modules.Masters.Application.Abstractions;
using HMS.Modules.Masters.Contracts;
using HMS.Modules.Masters.Domain;
using Microsoft.EntityFrameworkCore;

namespace HMS.Modules.Masters.Infrastructure.Repositories;

internal class CustomerRepository : ICustomerRepository
{
    private readonly MastersDbContext _dbContext;

    public CustomerRepository(MastersDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(Customer customer, CancellationToken cancellationToken)
        => await _dbContext.Customers.AddAsync(customer, cancellationToken);

    public Task<Customer?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => _dbContext.Customers.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public Task<bool> ExistsByCodeAsync(string customerCode, Guid? excludingId, CancellationToken cancellationToken)
        => _dbContext.Customers.AnyAsync(c => c.CustomerCode == customerCode && c.Id != excludingId, cancellationToken);

    public async Task<(IReadOnlyList<Customer> Items, int TotalCount)> GetPagedAsync(CustomerListQuery query, CancellationToken cancellationToken)
    {
        var customers = _dbContext.Customers.AsQueryable();

        if (query.IsActive.HasValue)
        {
            customers = customers.Where(c => c.IsActive == query.IsActive.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = $"%{query.Search.Trim()}%";
            customers = customers.Where(c => EF.Functions.ILike(c.CustomerCode, term) || EF.Functions.ILike(c.CustomerName, term));
        }

        customers = ApplySort(customers, query.Sort);

        var totalCount = await customers.CountAsync(cancellationToken);
        var items = await customers.Skip((query.Page - 1) * query.PageSize).Take(query.PageSize).ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
        => _dbContext.SaveChangesAsync(cancellationToken);

    private static IQueryable<Customer> ApplySort(IQueryable<Customer> customers, string? sort)
    {
        if (string.IsNullOrWhiteSpace(sort))
        {
            return customers.OrderBy(c => c.CustomerName);
        }

        var descending = sort.StartsWith('-');
        var field = descending ? sort[1..] : sort;

        return field.ToLowerInvariant() switch
        {
            "customercode" => descending ? customers.OrderByDescending(c => c.CustomerCode) : customers.OrderBy(c => c.CustomerCode),
            "updatedat" => descending ? customers.OrderByDescending(c => c.UpdatedAt) : customers.OrderBy(c => c.UpdatedAt),
            _ => descending ? customers.OrderByDescending(c => c.CustomerName) : customers.OrderBy(c => c.CustomerName),
        };
    }
}
