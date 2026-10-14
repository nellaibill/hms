using HMS.Modules.Pharmacy.Application;
using HMS.Modules.Pharmacy.Application.Abstractions;
using HMS.Shared.Kernel;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace HMS.Modules.Pharmacy.Infrastructure;

internal class PharmacyUnitOfWork : IPharmacyUnitOfWork
{
    private const int MaxAttempts = 3;

    private readonly PharmacyDbContext _dbContext;
    private readonly IStockPoster _stockPoster;

    public PharmacyUnitOfWork(PharmacyDbContext dbContext, IStockPoster stockPoster)
    {
        _dbContext = dbContext;
        _stockPoster = stockPoster;
    }

    public async Task<string> NextNumberAsync(DocumentSeries series, CancellationToken cancellationToken)
    {
        var fullyQualifiedName = $"{PharmacyDbContext.SchemaName}.{series.SequenceName}";
        var results = await _dbContext.Database
            .SqlQuery<long>($"SELECT nextval({fullyQualifiedName}::regclass)")
            .ToListAsync(cancellationToken);

        return $"{series.Prefix}-{PharmacyClock.Today():yyyy}-{results[0]:D6}";
    }

    public async Task<Result<T>> ExecuteAsync<T>(Func<Task<Result<T>>> work, CancellationToken cancellationToken)
    {
        for (var attempt = 1; ; attempt++)
        {
            var result = await work();
            if (!result.IsSuccess)
            {
                Reset();
                return result;
            }

            try
            {
                await _dbContext.SaveChangesAsync(cancellationToken);
                return result;
            }
            catch (Exception ex) when (IsStockConflict(ex))
            {
                Reset();
                if (attempt >= MaxAttempts)
                {
                    return Result<T>.Failure(PharmacyErrorCodes.StockConflict, "Stock changed while this was being saved. Please try again.");
                }
            }
        }
    }

    private void Reset()
    {
        _dbContext.ChangeTracker.Clear();
        _stockPoster.Reset();
    }

    // Two requests moving the same balance (stale xmin), or both creating a batch's first
    // balance row in a store (unique index) — either way the fix is to look again and redo it.
    private static bool IsStockConflict(Exception ex) =>
        ex is DbUpdateConcurrencyException
        || (ex is DbUpdateException { InnerException: PostgresException { SqlState: PostgresErrorCodes.UniqueViolation, ConstraintName: "ux_pharmacy_stock_balances_store_product_batch" } });
}
