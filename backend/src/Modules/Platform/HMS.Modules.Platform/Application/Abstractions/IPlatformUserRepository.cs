using HMS.Modules.Platform.Domain;

namespace HMS.Modules.Platform.Application.Abstractions;

/// <summary>
/// Defined here (Application) and implemented in Infrastructure, per the dependency
/// inversion rule in docs/DeveloperHandbook.md — Application never references EF Core types.
/// </summary>
internal interface IPlatformUserRepository
{
    Task AddAsync(PlatformUser user, CancellationToken cancellationToken);

    Task<PlatformUser?> GetByEmailAsync(string email, CancellationToken cancellationToken);

    Task<PlatformUser?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
