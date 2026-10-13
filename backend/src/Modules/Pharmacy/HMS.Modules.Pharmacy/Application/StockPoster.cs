using HMS.Modules.Pharmacy.Application.Abstractions;
using HMS.Modules.Pharmacy.Contracts;
using HMS.Modules.Pharmacy.Domain;
using HMS.Shared.Kernel;

namespace HMS.Modules.Pharmacy.Application;

/// <summary>One stock movement a document wants posted. <see cref="Quantity"/> is always
/// positive and in units; <see cref="Type"/> says which way it goes.</summary>
internal sealed record StockMovement(
    TransactionType Type,
    Guid StoreId,
    Guid ProductId,
    Guid ProductBatchId,
    decimal Quantity,
    decimal? UnitCost = null,
    decimal? UnitPrice = null,
    StockSourceType? SourceType = null,
    Guid? SourceId = null,
    string? SourceNo = null,
    Guid? PatientId = null,
    Guid? AdmissionId = null,
    string? Remarks = null,
    string? Label = null);

/// <summary>
/// The only code that changes a stock balance on behalf of a document. A goods receipt, a
/// pharmacy bill, a return, a transfer, an adjustment — each describes its movements and
/// posts them here, so "balance and ledger always move together, and never below zero" is
/// written once rather than once per document. Nothing is saved here: the caller commits
/// the document and its movements together through IPharmacyUnitOfWork.
/// </summary>
internal interface IStockPoster
{
    /// <summary>Fails with InsufficientStock when an outward movement asks for more than the
    /// store holds of that batch; inward movements always succeed.</summary>
    Task<Result<PharmacyStockTransaction>> PostAsync(StockMovement movement, Guid? actorId, CancellationToken cancellationToken);

    /// <summary>What the store holds of a batch right now, counting movements already posted
    /// in this request.</summary>
    Task<decimal> GetOnHandAsync(Guid storeId, Guid productId, Guid productBatchId, CancellationToken cancellationToken);

    /// <summary>Forgets the balances read so far — called by the unit of work before a retry.</summary>
    void Reset();
}

internal class StockPoster : IStockPoster
{
    private readonly IPharmacyStockBalanceRepository _balanceRepository;
    private readonly IPharmacyStockTransactionRepository _transactionRepository;

    // A document can touch the same batch on two lines. The second line must see the first
    // one's change, so balances are held here for the life of the request instead of being
    // re-read (a re-read reloads from the database and would drop the unsaved change).
    private readonly Dictionary<(Guid StoreId, Guid ProductId, Guid BatchId), PharmacyStockBalance?> _balances = [];

    public StockPoster(IPharmacyStockBalanceRepository balanceRepository, IPharmacyStockTransactionRepository transactionRepository)
    {
        _balanceRepository = balanceRepository;
        _transactionRepository = transactionRepository;
    }

    public async Task<Result<PharmacyStockTransaction>> PostAsync(StockMovement movement, Guid? actorId, CancellationToken cancellationToken)
    {
        var key = (movement.StoreId, movement.ProductId, movement.ProductBatchId);
        var balance = await FindAsync(key, cancellationToken);

        if (PharmacyStockTransaction.IsInward(movement.Type))
        {
            if (balance is null)
            {
                balance = PharmacyStockBalance.Create(movement.StoreId, movement.ProductId, movement.ProductBatchId, actorId);
                await _balanceRepository.AddAsync(balance, cancellationToken);
                _balances[key] = balance;
            }

            balance.Receive(movement.Quantity, actorId);
        }
        else
        {
            var onHand = balance?.QuantityOnHand ?? 0m;
            if (movement.Quantity > onHand)
            {
                var what = movement.Label ?? "this batch";
                return Result<PharmacyStockTransaction>.Failure(
                    PharmacyErrorCodes.InsufficientStock,
                    $"Only {onHand:0.####} of {what} in stock — {movement.Quantity:0.####} needed.");
            }

            balance!.Dispense(movement.Quantity, actorId);
        }

        var transaction = PharmacyStockTransaction.CreateFromDocument(
            movement.Type, movement.StoreId, movement.ProductId, movement.ProductBatchId, movement.Quantity, balance.QuantityOnHand,
            movement.UnitCost, movement.UnitPrice, movement.SourceType, movement.SourceId, movement.SourceNo,
            movement.PatientId, movement.AdmissionId, movement.Remarks, actorId);

        await _transactionRepository.AddAsync(transaction, cancellationToken);
        return Result<PharmacyStockTransaction>.Success(transaction);
    }

    public async Task<decimal> GetOnHandAsync(Guid storeId, Guid productId, Guid productBatchId, CancellationToken cancellationToken)
        => (await FindAsync((storeId, productId, productBatchId), cancellationToken))?.QuantityOnHand ?? 0m;

    public void Reset() => _balances.Clear();

    private async Task<PharmacyStockBalance?> FindAsync((Guid StoreId, Guid ProductId, Guid BatchId) key, CancellationToken cancellationToken)
    {
        if (!_balances.TryGetValue(key, out var balance))
        {
            balance = await _balanceRepository.GetAsync(key.StoreId, key.ProductId, key.BatchId, cancellationToken);
            _balances[key] = balance;
        }

        return balance;
    }
}
