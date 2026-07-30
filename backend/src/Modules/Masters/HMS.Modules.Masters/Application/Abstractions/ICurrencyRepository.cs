using HMS.Modules.Masters.Contracts;
using HMS.Modules.Masters.Domain;

namespace HMS.Modules.Masters.Application.Abstractions;

/// <summary>Defined here (Application) and implemented in Infrastructure, per the dependency inversion rule in docs/DeveloperHandbook.md §4.</summary>
internal interface ICurrencyRepository
{
    Task AddAsync(Currency currency, CancellationToken cancellationToken);

    Task<Currency?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<bool> ExistsByCodeAsync(string currencyCode, Guid? excludingId, CancellationToken cancellationToken);

    Task<(IReadOnlyList<Currency> Items, int TotalCount)> GetPagedAsync(CurrencyListQuery query, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
