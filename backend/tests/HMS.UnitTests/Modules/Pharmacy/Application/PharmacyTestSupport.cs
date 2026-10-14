using HMS.Modules.Pharmacy.Application;
using HMS.Modules.Pharmacy.Application.Abstractions;
using HMS.Modules.Pharmacy.Contracts;
using HMS.Modules.Pharmacy.Domain;
using HMS.Shared.Kernel;
using NSubstitute;

namespace HMS.UnitTests.Modules.Pharmacy.Application;

/// <summary>
/// A unit of work with no database behind it: numbers count up from 1, and "saving" is just
/// running the work once and counting it. Enough for a service test to see what a document
/// did without standing up Postgres.
/// </summary>
internal sealed class FakePharmacyUnitOfWork : IPharmacyUnitOfWork
{
    private int _next = 1;

    public int Saves { get; private set; }

    public Task<string> NextNumberAsync(DocumentSeries series, CancellationToken cancellationToken)
        => Task.FromResult($"{series.Prefix}-2026-{_next++:D6}");

    public async Task<Result<T>> ExecuteAsync<T>(Func<Task<Result<T>>> work, CancellationToken cancellationToken)
    {
        var result = await work();
        if (result.IsSuccess)
        {
            Saves++;
        }

        return result;
    }
}

/// <summary>
/// The real StockPoster over in-memory balances — so a document test exercises the actual
/// "never below zero" rule and sees real running balances, rather than a mock's opinion.
/// </summary>
internal sealed class StockFixture
{
    private readonly Dictionary<(Guid, Guid, Guid), PharmacyStockBalance> _balances = [];

    public IPharmacyStockBalanceRepository Balances { get; } = Substitute.For<IPharmacyStockBalanceRepository>();
    public IPharmacyStockTransactionRepository Transactions { get; } = Substitute.For<IPharmacyStockTransactionRepository>();
    public List<PharmacyStockTransaction> Posted { get; } = [];
    public StockPoster Poster { get; }

    public StockFixture()
    {
        Balances.GetAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(call => _balances.GetValueOrDefault((call.ArgAt<Guid>(0), call.ArgAt<Guid>(1), call.ArgAt<Guid>(2))));
        Balances.When(b => b.AddAsync(Arg.Any<PharmacyStockBalance>(), Arg.Any<CancellationToken>()))
            .Do(call =>
            {
                var balance = call.Arg<PharmacyStockBalance>();
                _balances[(balance.StoreId, balance.ProductId, balance.ProductBatchId)] = balance;
            });
        Transactions.When(t => t.AddAsync(Arg.Any<PharmacyStockTransaction>(), Arg.Any<CancellationToken>()))
            .Do(call => Posted.Add(call.Arg<PharmacyStockTransaction>()));

        Poster = new StockPoster(Balances, Transactions);
    }

    public void Seed(Guid storeId, Guid productId, Guid batchId, decimal quantity)
    {
        var balance = PharmacyStockBalance.Create(storeId, productId, batchId, null);
        balance.Receive(quantity, null);
        _balances[(storeId, productId, batchId)] = balance;
    }

    public decimal OnHand(Guid storeId, Guid productId, Guid batchId)
        => _balances.GetValueOrDefault((storeId, productId, batchId))?.QuantityOnHand ?? 0m;

    public static PharmacyStore DefaultStore(IPharmacyStoreRepository repository)
    {
        var store = PharmacyStore.Create("MAIN", "Main Pharmacy", StoreType.Pharmacy, null);
        store.MakeDefault(null);
        repository.GetDefaultAsync(Arg.Any<CancellationToken>()).Returns(store);
        repository.GetByIdAsync(store.Id, Arg.Any<CancellationToken>()).Returns(store);
        repository.GetAllAsync(Arg.Any<CancellationToken>()).Returns([store]);
        return store;
    }
}
