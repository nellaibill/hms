using HMS.Modules.Identity.Application.Abstractions;
using HMS.Modules.Identity.Application.Mapping;
using HMS.Modules.Identity.Contracts;
using HMS.Modules.Identity.Domain;
using HMS.Shared.Kernel;
using Microsoft.Extensions.Logging;

namespace HMS.Modules.Identity.Application;

internal class RoleService : IRoleService
{
    private readonly IRoleRepository _repository;
    private readonly ILogger<RoleService> _logger;

    public RoleService(
        IRoleRepository repository,
        ILogger<RoleService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<Result<RoleResponse>> CreateAsync(
        CreateRoleRequest request,
        Guid? actorId,
        CancellationToken cancellationToken)
    {
        var existingName =
            await _repository.GetByNameAsync(
                request.Name,
                cancellationToken);

        if (existingName is not null)
        {
            return Result<RoleResponse>.Failure(
                RoleErrorCodes.DuplicateName,
                $"A role with name '{request.Name}' already exists.");
        }

        var existingCode =
            await _repository.GetByCodeAsync(
                request.Code,
                cancellationToken);

        if (existingCode is not null)
        {
            return Result<RoleResponse>.Failure(
                RoleErrorCodes.DuplicateCode,
                $"A role with code '{request.Code}' already exists.");
        }

        var role = Role.Create(
            request.Name,
            request.Code,
            request.Description,
            request.IsSystemRole,
            request.DisplayOrder,
            actorId);

        await _repository.AddAsync(role, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

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
            await _repository.GetByIdAsync(
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
                await _repository.GetByNameAsync(
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

        if (!string.Equals(
                role.Code,
                request.Code.Trim(),
                StringComparison.OrdinalIgnoreCase))
        {
            var existing =
                await _repository.GetByCodeAsync(
                    request.Code,
                    cancellationToken);

            if (existing is not null &&
                existing.Id != id)
            {
                return Result<RoleResponse>.Failure(
                    RoleErrorCodes.DuplicateCode,
                    $"A role with code '{request.Code}' already exists.");
            }
        }

        role.Update(
            request.Name,
            request.Code,
            request.Description,
            request.DisplayOrder,
            actorId);

        await _repository.SaveChangesAsync(cancellationToken);

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
            await _repository.GetByIdAsync(
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

        await _repository.SaveChangesAsync(cancellationToken);

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
            await _repository.GetByIdAsync(
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
            await _repository.GetPagedAsync(
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
            await _repository.GetByIdAsync(id, cancellationToken);

        if (role is null)
        {
            return Result<RoleResponse>.Failure(
                RoleErrorCodes.NotFound,
                $"Role '{id}' was not found.");
        }

        role.Activate(actorId);

        await _repository.SaveChangesAsync(cancellationToken);

        return Result<RoleResponse>.Success(role.ToResponse());
    }

    public async Task<Result<RoleResponse>> DeactivateAsync(
        Guid id,
        Guid? actorId,
        CancellationToken cancellationToken)
    {
        var role =
            await _repository.GetByIdAsync(id, cancellationToken);

        if (role is null)
        {
            return Result<RoleResponse>.Failure(
                RoleErrorCodes.NotFound,
                $"Role '{id}' was not found.");
        }

        role.Deactivate(actorId);

        await _repository.SaveChangesAsync(cancellationToken);

        return Result<RoleResponse>.Success(role.ToResponse());
    }
}