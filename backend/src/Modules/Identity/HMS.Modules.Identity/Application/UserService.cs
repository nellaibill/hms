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
    private static readonly string[] AllowedProfilePhotoExtensions = [".jpg", ".jpeg", ".png", ".webp"];
    private static readonly string[] AllowedProfilePhotoContentTypes = ["image/jpeg", "image/png", "image/webp"];
    private const long MaxProfilePhotoSizeBytes = 2 * 1024 * 1024; // 2MB, per the profile-photo-upload requirement.

    private readonly IUserRepository _repository;
    private readonly IRoleRepository _roleRepository;
    private readonly IUserFileStorage _fileStorage;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ILogger<UserService> _logger;

    public UserService(
        IUserRepository repository,
        IRoleRepository roleRepository,
        IUserFileStorage fileStorage,
        IPasswordHasher passwordHasher,
        ILogger<UserService> logger)
    {
        _repository = repository;
        _roleRepository = roleRepository;
        _fileStorage = fileStorage;
        _passwordHasher = passwordHasher;
        _logger = logger;
    }

    public async Task<Result<UserResponse>> CreateAsync(CreateUserRequest request, Guid? actorId, CancellationToken cancellationToken)
    {
        var existingUsername = await _repository.GetByUsernameAsync(request.Username, cancellationToken);
        if (existingUsername is not null)
        {
            return Result<UserResponse>.Failure(
                UserErrorCodes.DuplicateUsername,
                $"A user with username '{request.Username}' already exists.");
        }

        var existing = await _repository.GetByEmailAsync(request.Email, cancellationToken);
        if (existing is not null)
        {
            return Result<UserResponse>.Failure(
                UserErrorCodes.DuplicateEmail,
                $"A user with email '{request.Email}' already exists.");
        }

        var role = await _roleRepository.GetByIdAsync(request.RoleId, cancellationToken);
        if (role is null)
        {
            return Result<UserResponse>.Failure(
                UserErrorCodes.InvalidRole,
                $"Role '{request.RoleId}' was not found.");
        }

        var user = User.Create(
            request.Username,
            request.FirstName,
            request.LastName,
            request.Email,
            request.PhoneNumber,
            request.RoleId,
            actorId);

        await _repository.AddAsync(user, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Created user {UserId}", user.Id);

        return Result<UserResponse>.Success(user.ToResponse(role.Name));
    }

    public async Task<Result<UserResponse>> UpdateAsync(Guid id, UpdateUserRequest request, Guid? actorId, CancellationToken cancellationToken)
    {
        var user = await _repository.GetByIdAsync(id, cancellationToken);
        if (user is null)
        {
            return Result<UserResponse>.Failure(UserErrorCodes.NotFound, $"User '{id}' was not found.");
        }

        if (!string.Equals(user.Username, request.Username.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            var existingUsername = await _repository.GetByUsernameAsync(request.Username, cancellationToken);
            if (existingUsername is not null && existingUsername.Id != id)
            {
                return Result<UserResponse>.Failure(
                    UserErrorCodes.DuplicateUsername,
                    $"A user with username '{request.Username}' already exists.");
            }

            user.ChangeUsername(request.Username, actorId);
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

        Role? role = null;
        if (user.RoleId != request.RoleId)
        {
            role = await _roleRepository.GetByIdAsync(request.RoleId, cancellationToken);
            if (role is null)
            {
                return Result<UserResponse>.Failure(
                    UserErrorCodes.InvalidRole,
                    $"Role '{request.RoleId}' was not found.");
            }

            user.ChangeRole(request.RoleId, actorId);
        }

        user.UpdateProfile(request.FirstName, request.LastName, request.PhoneNumber, actorId);
        await _repository.SaveChangesAsync(cancellationToken);

        // Role stayed the same, so it was never looked up above — needed now only for the
        // response's RoleName.
        role ??= await _roleRepository.GetByIdAsync(user.RoleId, cancellationToken);

        _logger.LogInformation("Updated user {UserId}", user.Id);

        return Result<UserResponse>.Success(user.ToResponse(role!.Name));
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
        if (user is null)
        {
            return Result<UserResponse>.Failure(UserErrorCodes.NotFound, $"User '{id}' was not found.");
        }

        var role = await _roleRepository.GetByIdAsync(user.RoleId, cancellationToken);
        return Result<UserResponse>.Success(user.ToResponse(role!.Name));
    }

    public async Task<PagedResult<UserResponse>> GetPagedAsync(UserListQuery query, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _repository.GetPagedAsync(query, cancellationToken);

        // One batched role lookup for the whole page rather than one per row (N+1).
        var roleIds = items.Select(u => u.RoleId).Distinct();
        var roles = await _roleRepository.GetManyByIdsAsync(roleIds, cancellationToken);
        var roleNameById = roles.ToDictionary(r => r.Id, r => r.Name);

        var mapped = items
            .Select(u => u.ToResponse(roleNameById.GetValueOrDefault(u.RoleId, string.Empty)))
            .ToList();

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

        var role = await _roleRepository.GetByIdAsync(user.RoleId, cancellationToken);

        _logger.LogInformation("Activated user {UserId}", user.Id);

        return Result<UserResponse>.Success(user.ToResponse(role!.Name));
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

        var role = await _roleRepository.GetByIdAsync(user.RoleId, cancellationToken);

        _logger.LogInformation("Deactivated user {UserId}", user.Id);

        return Result<UserResponse>.Success(user.ToResponse(role!.Name));
    }

    // Deliberately not part of CreateAsync — per the profile-photo-upload requirement, a
    // photo is only ever added later, from the User Details page, never at creation time.
    //
    // Deliberately does NOT check user.IsActive: no other write operation in this service
    // (UpdateAsync, ChangeRole, etc.) gates on the target user's active status either, so
    // an inactive user's photo can still be uploaded/replaced — consistent with the rest
    // of this file, not a new business rule.
    //
    // Validation order is cheapest-first: extension/content-type/size are free (already on
    // the request), so they run before the magic-bytes check, which has to read the stream.
    // File save happens only after every check passes and strictly before SetProfilePhoto/
    // SaveChangesAsync — if SaveProfilePhotoAsync throws (e.g. a disk write failure), the
    // domain and database are never touched, and the exception propagates to
    // GlobalExceptionHandler like any other unexpected infrastructure failure
    // (docs/Architecture.md's exception handling strategy) rather than being reported as a
    // normal Result failure.
    public async Task<Result<UserResponse>> UploadProfilePhotoAsync(Guid id, Stream content, string fileName, string contentType, long length, Guid? actorId, CancellationToken cancellationToken)
    {
        var user = await _repository.GetByIdAsync(id, cancellationToken);
        if (user is null)
        {
            return Result<UserResponse>.Failure(UserErrorCodes.NotFound, $"User '{id}' was not found.");
        }

        var extension = Path.GetExtension(fileName);
        if (!AllowedProfilePhotoExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase) ||
            !AllowedProfilePhotoContentTypes.Contains(contentType))
        {
            return Result<UserResponse>.Failure(UserErrorCodes.InvalidFile, "Only JPG, JPEG, PNG and WEBP files are allowed.");
        }

        if (length > MaxProfilePhotoSizeBytes)
        {
            return Result<UserResponse>.Failure(UserErrorCodes.InvalidFile, "Profile photo must not exceed 2MB.");
        }

        if (!await LooksLikeAnAllowedImageAsync(content, cancellationToken))
        {
            return Result<UserResponse>.Failure(UserErrorCodes.InvalidFile, "The uploaded file is not a valid image.");
        }

        var path = await _fileStorage.SaveProfilePhotoAsync(id, fileName, content, cancellationToken);
        user.SetProfilePhoto(path, actorId);
        await _repository.SaveChangesAsync(cancellationToken);

        var role = await _roleRepository.GetByIdAsync(user.RoleId, cancellationToken);

        _logger.LogInformation("Uploaded profile photo for user {UserId}", user.Id);

        return Result<UserResponse>.Success(user.ToResponse(role!.Name));
    }

    // The only place PasswordHash ever gets set — user creation has no credential step,
    // so without this a created user could never satisfy AuthenticationService's rule 2.
    // Deliberately does not gate on user.IsActive, consistent with every other write
    // operation in this file.
    public async Task<Result<UserResponse>> SetPasswordAsync(Guid id, string password, Guid? actorId, CancellationToken cancellationToken)
    {
        var user = await _repository.GetByIdAsync(id, cancellationToken);
        if (user is null)
        {
            return Result<UserResponse>.Failure(UserErrorCodes.NotFound, $"User '{id}' was not found.");
        }

        user.SetPasswordHash(_passwordHasher.HashPassword(password), actorId);
        await _repository.SaveChangesAsync(cancellationToken);

        var role = await _roleRepository.GetByIdAsync(user.RoleId, cancellationToken);

        _logger.LogInformation("Set password for user {UserId}", user.Id);

        return Result<UserResponse>.Success(user.ToResponse(role!.Name));
    }

    // A client-supplied extension and Content-Type header are just claims — this checks the
    // file's actual bytes against the well-known signatures for the three allowed formats,
    // so a renamed non-image (e.g. "virus.jpg" containing plain text) is still rejected.
    // Resets the stream position afterward so the full content is still there for SaveAsync.
    private static async Task<bool> LooksLikeAnAllowedImageAsync(Stream content, CancellationToken cancellationToken)
    {
        var header = new byte[12];
        var bytesRead = await content.ReadAsync(header.AsMemory(0, header.Length), cancellationToken);
        content.Position = 0;

        // JPEG: FF D8 FF
        if (bytesRead >= 3 && header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF)
        {
            return true;
        }

        // PNG: 89 50 4E 47 0D 0A 1A 0A
        if (bytesRead >= 8 &&
            header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47 &&
            header[4] == 0x0D && header[5] == 0x0A && header[6] == 0x1A && header[7] == 0x0A)
        {
            return true;
        }

        // WEBP: "RIFF" .... "WEBP"
        if (bytesRead >= 12 &&
            header[0] == 0x52 && header[1] == 0x49 && header[2] == 0x46 && header[3] == 0x46 &&
            header[8] == 0x57 && header[9] == 0x45 && header[10] == 0x42 && header[11] == 0x50)
        {
            return true;
        }

        return false;
    }
}
