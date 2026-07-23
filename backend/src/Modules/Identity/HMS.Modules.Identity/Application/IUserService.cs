using HMS.Modules.Identity.Contracts;
using HMS.Shared.Kernel;

namespace HMS.Modules.Identity.Application;

/// <summary>
/// Public (not internal): this is the one Application-layer type UsersController — which
/// ASP.NET Core requires to be public, with a public constructor, for controller discovery
/// and DI activation (see docs/Architecture.md) — takes as a constructor dependency. A
/// public constructor cannot have an internal parameter type (CS0051), so this interface
/// is the module's deliberate, narrow seam between its public HTTP boundary and its
/// otherwise-internal Application/Domain/Infrastructure layers. <see cref="UserService"/>
/// (the implementation), <see cref="Domain.User"/>, and everything in Infrastructure
/// remain internal.
/// </summary>
public interface IUserService
{
    Task<Result<UserResponse>> CreateAsync(CreateUserRequest request, Guid? actorId, CancellationToken cancellationToken);

    Task<Result<UserResponse>> UpdateAsync(Guid id, UpdateUserRequest request, Guid? actorId, CancellationToken cancellationToken);

    Task<Result> DeleteAsync(Guid id, Guid? actorId, CancellationToken cancellationToken);

    Task<Result<UserResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<PagedResult<UserResponse>> GetPagedAsync(UserListQuery query, CancellationToken cancellationToken);

    Task<Result<UserResponse>> ActivateAsync(Guid id, Guid? actorId, CancellationToken cancellationToken);

    Task<Result<UserResponse>> DeactivateAsync(Guid id, Guid? actorId, CancellationToken cancellationToken);
}
