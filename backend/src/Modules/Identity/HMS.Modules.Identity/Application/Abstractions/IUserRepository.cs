using HMS.Modules.Identity.Contracts;
using HMS.Modules.Identity.Domain;

namespace HMS.Modules.Identity.Application.Abstractions;

/// <summary>
/// Defined here (Application) and implemented in Infrastructure, per the dependency
/// inversion rule in docs/Architecture.md — Application never references EF Core types.
/// </summary>
internal interface IUserRepository
{
    Task AddAsync(User user, CancellationToken cancellationToken);

    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken);

    Task<(IReadOnlyList<User> Items, int TotalCount)> GetPagedAsync(UserListQuery query, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
