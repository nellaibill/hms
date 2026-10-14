using HMS.Modules.Pharmacy.Domain;

namespace HMS.Modules.Pharmacy.Application.Abstractions;

internal interface IPharmacyStoreRepository
{
    Task AddAsync(PharmacyStore store, CancellationToken cancellationToken);

    Task<PharmacyStore?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>The store stock goes to and comes from when a request names none. Should
    /// more than one ever be flagged, the oldest wins, so the answer is at least stable.</summary>
    Task<PharmacyStore?> GetDefaultAsync(CancellationToken cancellationToken);

    /// <summary>Every store, default first then by name — a hospital has a handful, so this
    /// is never paged.</summary>
    Task<IReadOnlyList<PharmacyStore>> GetAllAsync(CancellationToken cancellationToken);

    Task<bool> ExistsByCodeAsync(string code, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}

internal static class PharmacyStoreRepositoryExtensions
{
    /// <summary>
    /// The store a stock operation applies to: the one named, or the default when none is.
    /// Null when the named store doesn't exist or is inactive — callers turn that into
    /// PharmacyErrorCodes.InvalidStore.
    /// </summary>
    public static async Task<PharmacyStore?> ResolveAsync(this IPharmacyStoreRepository repository, Guid? storeId, CancellationToken cancellationToken)
    {
        var store = storeId is { } id
            ? await repository.GetByIdAsync(id, cancellationToken)
            : await repository.GetDefaultAsync(cancellationToken);

        return store is { IsActive: true } ? store : null;
    }
}
