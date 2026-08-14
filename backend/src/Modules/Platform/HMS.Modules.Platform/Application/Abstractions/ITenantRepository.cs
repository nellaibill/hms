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

    Task<Tenant?> GetByHospitalCodeAsync(string hospitalCode, CancellationToken cancellationToken);

    Task<Tenant?> GetByEmailAsync(string email, CancellationToken cancellationToken);

    Task<(IReadOnlyList<Tenant> Items, int TotalCount)> GetPagedAsync(TenantListQuery query, CancellationToken cancellationToken);

    Task<(int Total, int Active, int Inactive)> GetCountsAsync(CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
