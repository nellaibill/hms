using HMS.Modules.Roles.Contracts;
using HMS.Shared.Kernel;

namespace HMS.Modules.Roles.Application;

public interface IRoleService
{
    Task<Result<RoleResponse>> CreateAsync(
        CreateRoleRequest request,
        Guid? actorId,
        CancellationToken cancellationToken);

    Task<Result<RoleResponse>> UpdateAsync(
        Guid id,
        UpdateRoleRequest request,
        Guid? actorId,
        CancellationToken cancellationToken);

    Task<Result> DeleteAsync(
        Guid id,
        Guid? actorId,
        CancellationToken cancellationToken);

    Task<Result<RoleResponse>> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken);

    Task<PagedResult<RoleResponse>> GetPagedAsync(
        RoleListQuery query,
        CancellationToken cancellationToken);

    Task<Result<RoleResponse>> ActivateAsync(
        Guid id,
        Guid? actorId,
        CancellationToken cancellationToken);

    Task<Result<RoleResponse>> DeactivateAsync(
        Guid id,
        Guid? actorId,
        CancellationToken cancellationToken);
}