using HMS.Modules.Pharmacy.Application.Abstractions;
using HMS.Modules.Pharmacy.Contracts;
using HMS.Modules.Pharmacy.Domain;
using Microsoft.EntityFrameworkCore;

namespace HMS.Modules.Pharmacy.Infrastructure.Repositories;

internal class PharmacyStockTransactionRepository : IPharmacyStockTransactionRepository
{
    private readonly PharmacyDbContext _dbContext;

    public PharmacyStockTransactionRepository(PharmacyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(PharmacyStockTransaction transaction, CancellationToken cancellationToken)
        => await _dbContext.StockTransactions.AddAsync(transaction, cancellationToken);

    public Task<PharmacyStockTransaction?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => _dbContext.StockTransactions.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

    public async Task<(IReadOnlyList<PharmacyStockTransaction> Items, int TotalCount)> GetPagedAsync(StockLedgerListQuery query, CancellationToken cancellationToken)
    {
        var transactions = _dbContext.StockTransactions.AsQueryable();

        if (query.ProductId.HasValue)
        {
            transactions = transactions.Where(t => t.ProductId == query.ProductId.Value);
        }

        if (query.ProductBatchId.HasValue)
        {
            transactions = transactions.Where(t => t.ProductBatchId == query.ProductBatchId.Value);
        }

        if (query.TransactionType.HasValue)
        {
            transactions = transactions.Where(t => t.TransactionType == query.TransactionType.Value);
        }

        if (query.PatientId.HasValue)
        {
            transactions = transactions.Where(t => t.PatientId == query.PatientId.Value);
        }

        if (query.FromDate.HasValue)
        {
            transactions = transactions.Where(t => t.TransactionDate >= query.FromDate.Value);
        }

        if (query.ToDate.HasValue)
        {
            transactions = transactions.Where(t => t.TransactionDate <= query.ToDate.Value);
        }

        transactions = ApplySort(transactions, query.Sort);

        var totalCount = await transactions.CountAsync(cancellationToken);
        var items = await transactions.Skip((query.Page - 1) * query.PageSize).Take(query.PageSize).ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    private static IQueryable<PharmacyStockTransaction> ApplySort(IQueryable<PharmacyStockTransaction> transactions, string? sort)
    {
        if (string.IsNullOrWhiteSpace(sort))
        {
            return transactions.OrderByDescending(t => t.TransactionDate);
        }

        var descending = sort.StartsWith('-');
        var field = descending ? sort[1..] : sort;

        return field.ToLowerInvariant() switch
        {
            "quantity" => descending ? transactions.OrderByDescending(t => t.Quantity) : transactions.OrderBy(t => t.Quantity),
            "createdat" => descending ? transactions.OrderByDescending(t => t.CreatedAt) : transactions.OrderBy(t => t.CreatedAt),
            _ => descending ? transactions.OrderByDescending(t => t.TransactionDate) : transactions.OrderBy(t => t.TransactionDate),
        };
    }
}
