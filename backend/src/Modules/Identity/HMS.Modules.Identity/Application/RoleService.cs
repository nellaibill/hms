using HMS.Modules.Identity.Application.Abstractions;
using HMS.Modules.Identity.Application.Mapping;
using HMS.Modules.Identity.Contracts;
using HMS.Modules.Identity.Domain;
using HMS.Shared.Kernel;
using Microsoft.Extensions.Logging;

namespace HMS.Modules.Identity.Application;

internal class RoleService : IRoleService
{
    private readonly IRoleRepository _roleRepository;
    private readonly IPermissionRepository _permissionRepository;
    private readonly ILogger<RoleService> _logger;

    public RoleService(
        IRoleRepository roleRepository,
        IPermissionRepository permissionRepository,
        ILogger<RoleService> logger)
    {
        _roleRepository = roleRepository;
        _permissionRepository = permissionRepository;
        _logger = logger;
    }

    public async Task<Result<RoleResponse>> CreateAsync(
        CreateRoleRequest request,
        Guid? actorId,
        CancellationToken cancellationToken)
    {
        var existingName =
            await _roleRepository.GetByNameAsync(
                request.Name,
                cancellationToken);

        if (existingName is not null)
        {
            return Result<RoleResponse>.Failure(
                RoleErrorCodes.DuplicateName,
                $"A role with name '{request.Name}' already exists.");
        }

        // Public API never creates system roles; those are seeded/provisioned internally.
        var role = Role.Create(
            request.Name,
            request.Description,
            isSystemRole: false,
            request.DisplayOrder,
            actorId);

        if (!request.IsActive)
            {
                role.Deactivate(actorId);
            }

        var requestedKeys = request.PermissionKeys
            .Where(k => !string.IsNullOrWhiteSpace(k))
            .Select(k => k.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var permissions =
            await _permissionRepository.GetActiveByKeysAsync(
                requestedKeys,
                cancellationToken);

        if (permissions.Count != requestedKeys.Count)
        {
            return Result<RoleResponse>.Failure(
                RoleErrorCodes.InvalidPermission,
                "One or more selected permissions are invalid.");
        }

        role.ReplacePermissions(
            permissions.Select(p => p.Id));

        await _roleRepository.AddAsync(role, cancellationToken);
        await _roleRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Created role {RoleId}",
            role.Id);

        return Result<RoleResponse>.Success(
            role.ToResponse());
    }

    public async Task<Result<RoleResponse>> UpdateAsync(
        Guid id,
        UpdateRoleRequest request,
        Guid? actorId,
        CancellationToken cancellationToken)
    {
        var role =
            await _roleRepository.GetByIdAsync(
                id,
                cancellationToken);

        if (role is null)
        {
            return Result<RoleResponse>.Failure(
                RoleErrorCodes.NotFound,
                $"Role '{id}' was not found.");
        }

        if (!string.Equals(
                role.Name,
                request.Name.Trim(),
                StringComparison.OrdinalIgnoreCase))
        {
            var existing =
                await _roleRepository.GetByNameAsync(
                    request.Name,
                    cancellationToken);

            if (existing is not null &&
                existing.Id != id)
            {
                return Result<RoleResponse>.Failure(
                    RoleErrorCodes.DuplicateName,
                    $"A role with name '{request.Name}' already exists.");
            }
        }

        role.Update(
            request.Name,
            request.Description,
            request.DisplayOrder,
            actorId);

        if (request.IsActive)
            {
                role.Activate(actorId);
            }
        else
            {
                role.Deactivate(actorId);
            }

        var requestedKeys = request.PermissionKeys
            .Where(k => !string.IsNullOrWhiteSpace(k))
            .Select(k => k.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
            
        var permissions =
            await _permissionRepository.GetActiveByKeysAsync(
                requestedKeys,
                cancellationToken);

        if (permissions.Count != requestedKeys.Count)
        {
            return Result<RoleResponse>.Failure(
                RoleErrorCodes.InvalidPermission,
                "One or more selected permissions are invalid.");
        }

        role.ReplacePermissions(
            permissions.Select(p => p.Id));

        await _roleRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Updated role {RoleId}",
            role.Id);

        return Result<RoleResponse>.Success(
            role.ToResponse());
    }

    public async Task<Result> DeleteAsync(
        Guid id,
        Guid? actorId,
        CancellationToken cancellationToken)
    {
        var role =
            await _roleRepository.GetByIdAsync(
                id,
                cancellationToken);

        if (role is null)
        {
            return Result.Failure(
                RoleErrorCodes.NotFound,
                $"Role '{id}' was not found.");
        }

        if (role.IsSystemRole)
        {
            return Result.Failure(
                RoleErrorCodes.SystemRoleDeletionForbidden,
                "System roles cannot be deleted.");
        }

        role.SoftDelete(actorId);

        await _roleRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Deleted role {RoleId}",
            role.Id);

        return Result.Success();
    }

    public async Task<Result<RoleResponse>> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var role =
            await _roleRepository.GetByIdAsync(
                id,
                cancellationToken);

        return role is null
            ? Result<RoleResponse>.Failure(
                RoleErrorCodes.NotFound,
                $"Role '{id}' was not found.")
            : Result<RoleResponse>.Success(
                role.ToResponse());
    }

    public async Task<PagedResult<RoleResponse>> GetPagedAsync(
        RoleListQuery query,
        CancellationToken cancellationToken)
    {
        var (items, totalCount) =
            await _roleRepository.GetPagedAsync(
                query,
                cancellationToken);

        var mapped =
            items.Select(r => r.ToResponse()).ToList();

        return new PagedResult<RoleResponse>(
            mapped,
            query.Page,
            query.PageSize,
            totalCount);
    }

    public async Task<Result<RoleResponse>> ActivateAsync(
        Guid id,
        Guid? actorId,
        CancellationToken cancellationToken)
    {
        var role =
            await _roleRepository.GetByIdAsync(id, cancellationToken);

        if (role is null)
        {
            return Result<RoleResponse>.Failure(
                RoleErrorCodes.NotFound,
                $"Role '{id}' was not found.");
        }

        role.Activate(actorId);

        await _roleRepository.SaveChangesAsync(cancellationToken);

        return Result<RoleResponse>.Success(role.ToResponse());
    }

    public async Task<Result<RoleResponse>> DeactivateAsync(
        Guid id,
        Guid? actorId,
        CancellationToken cancellationToken)
    {
        var role =
            await _roleRepository.GetByIdAsync(id, cancellationToken);

        if (role is null)
        {
            return Result<RoleResponse>.Failure(
                RoleErrorCodes.NotFound,
                $"Role '{id}' was not found.");
        }

        role.Deactivate(actorId);

        await _roleRepository.SaveChangesAsync(cancellationToken);

        return Result<RoleResponse>.Success(role.ToResponse());
    }
}