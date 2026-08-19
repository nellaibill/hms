using HMS.Modules.Platform.Contracts;
using HMS.Modules.Platform.Domain;

namespace HMS.Modules.Platform.Application.Abstractions;

/// <summary>
/// Defined here (Application) and implemented in Infrastructure, per the dependency
/// inversion rule — same split as <see cref="IPlatformUserRepository"/>.
/// </summary>
internal interface ITenantRepository
{
    Task AddAsync(Tenant tenant, CancellationToken cancellationToken);

    Task<Tenant?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Same as <see cref="GetByIdAsync"/> but bypasses the IsDeleted query filter
    /// — the only way to look up a soft-deleted tenant, e.g. to restore it.</summary>
    Task<Tenant?> GetByIdIncludingDeletedAsync(Guid id, CancellationToken cancellationToken);

    Task<Tenant?> GetByHospitalCodeAsync(string hospitalCode, CancellationToken cancellationToken);

    Task<Tenant?> GetByEmailAsync(string email, CancellationToken cancellationToken);

    Task<(IReadOnlyList<Tenant> Items, int TotalCount)> GetPagedAsync(TenantListQuery query, CancellationToken cancellationToken);

    /// <summary>Mirrors <see cref="GetPagedAsync"/> but returns only soft-deleted tenants
    /// (bypassing the IsDeleted query filter) — backs the deleted-hospitals list an admin
    /// restores from.</summary>
    Task<(IReadOnlyList<Tenant> Items, int TotalCount)> GetDeletedPagedAsync(TenantListQuery query, CancellationToken cancellationToken);

    Task<(int Total, int Active, int Inactive)> GetCountsAsync(CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
