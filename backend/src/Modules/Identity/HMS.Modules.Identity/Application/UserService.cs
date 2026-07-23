using HMS.Modules.Identity.Application.Abstractions;
using HMS.Modules.Identity.Application.Mapping;
using HMS.Modules.Identity.Contracts;
using HMS.Modules.Identity.Domain;
using HMS.Shared.Kernel;
using Microsoft.Extensions.Logging;

namespace HMS.Modules.Identity.Application;

/// <summary>
/// Orchestrates Users use cases: expected failures (not found, duplicate email) are
/// returned as <see cref="Result"/> failures, never thrown — see docs/Architecture.md's
/// exception handling strategy.
/// </summary>
internal class UserService : IUserService
{
    private readonly IUserRepository _repository;
    private readonly ILogger<UserService> _logger;

    public UserService(IUserRepository repository, ILogger<UserService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<Result<UserResponse>> CreateAsync(CreateUserRequest request, Guid? actorId, CancellationToken cancellationToken)
    {
        var existing = await _repository.GetByEmailAsync(request.Email, cancellationToken);
        if (existing is not null)
        {
            return Result<UserResponse>.Failure(
                UserErrorCodes.DuplicateEmail,
                $"A user with email '{request.Email}' already exists.");
        }

        var user = User.Create(request.FirstName, request.LastName, request.Email, request.PhoneNumber, actorId);
        await _repository.AddAsync(user, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Created user {UserId}", user.Id);

        return Result<UserResponse>.Success(user.ToResponse());
    }

    public async Task<Result<UserResponse>> UpdateAsync(Guid id, UpdateUserRequest request, Guid? actorId, CancellationToken cancellationToken)
    {
        var user = await _repository.GetByIdAsync(id, cancellationToken);
        if (user is null)
        {
            return Result<UserResponse>.Failure(UserErrorCodes.NotFound, $"User '{id}' was not found.");
        }

        if (!string.Equals(user.Email, request.Email.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            var existing = await _repository.GetByEmailAsync(request.Email, cancellationToken);
            if (existing is not null && existing.Id != id)
            {
                return Result<UserResponse>.Failure(
                    UserErrorCodes.DuplicateEmail,
                    $"A user with email '{request.Email}' already exists.");
            }

            user.ChangeEmail(request.Email, actorId);
        }

        user.UpdateProfile(request.FirstName, request.LastName, request.PhoneNumber, actorId);
        await _repository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Updated user {UserId}", user.Id);

        return Result<UserResponse>.Success(user.ToResponse());
    }

    public async Task<Result> DeleteAsync(Guid id, Guid? actorId, CancellationToken cancellationToken)
    {
        var user = await _repository.GetByIdAsync(id, cancellationToken);
        if (user is null)
        {
            return Result.Failure(UserErrorCodes.NotFound, $"User '{id}' was not found.");
        }

        user.SoftDelete(actorId);
        await _repository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Soft-deleted user {UserId}", user.Id);

        return Result.Success();
    }

    public async Task<Result<UserResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var user = await _repository.GetByIdAsync(id, cancellationToken);
        return user is null
            ? Result<UserResponse>.Failure(UserErrorCodes.NotFound, $"User '{id}' was not found.")
            : Result<UserResponse>.Success(user.ToResponse());
    }

    public async Task<PagedResult<UserResponse>> GetPagedAsync(UserListQuery query, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _repository.GetPagedAsync(query, cancellationToken);
        var mapped = items.Select(u => u.ToResponse()).ToList();

        return new PagedResult<UserResponse>(mapped, query.Page, query.PageSize, totalCount);
    }

    public async Task<Result<UserResponse>> ActivateAsync(Guid id, Guid? actorId, CancellationToken cancellationToken)
    {
        var user = await _repository.GetByIdAsync(id, cancellationToken);
        if (user is null)
        {
            return Result<UserResponse>.Failure(UserErrorCodes.NotFound, $"User '{id}' was not found.");
        }

        user.Activate(actorId);
        await _repository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Activated user {UserId}", user.Id);

        return Result<UserResponse>.Success(user.ToResponse());
    }

    public async Task<Result<UserResponse>> DeactivateAsync(Guid id, Guid? actorId, CancellationToken cancellationToken)
    {
        var user = await _repository.GetByIdAsync(id, cancellationToken);
        if (user is null)
        {
            return Result<UserResponse>.Failure(UserErrorCodes.NotFound, $"User '{id}' was not found.");
        }

        user.Deactivate(actorId);
        await _repository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Deactivated user {UserId}", user.Id);

        return Result<UserResponse>.Success(user.ToResponse());
    }
}
