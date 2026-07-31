using HMS.Modules.Identity.Application.Abstractions;
using HMS.Modules.Identity.Contracts;
using HMS.Shared.Kernel;
using Microsoft.Extensions.Logging;

namespace HMS.Modules.Identity.Application;

/// <summary>
/// Orchestrates the Login use case. Every rejection reason (rules 1-4 below) returns the
/// same generic <see cref="AuthenticationErrorCodes.InvalidLogin"/> failure — never
/// revealing which check failed, per standard login-security practice (docs/ApiStandards.md
/// §8). Soft-deleted users need no special handling: IUserRepository.GetByUsernameAsync
/// already excludes them via User's EF global query filter, so they naturally fail rule 1
/// exactly like a username that never existed.
/// </summary>
internal class AuthenticationService : IAuthenticationService
{
    private const string GenericInvalidLoginMessage = "Invalid username or password.";

    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly ILogger<AuthenticationService> _logger;

    public AuthenticationService(
        IUserRepository userRepository,
        IRoleRepository roleRepository,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator jwtTokenGenerator,
        ILogger<AuthenticationService> logger)
    {
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _passwordHasher = passwordHasher;
        _jwtTokenGenerator = jwtTokenGenerator;
        _logger = logger;
    }

    public async Task<Result<LoginResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        // Rule 1: user exists.
        var user = await _userRepository.GetByUsernameAsync(request.Username, cancellationToken);
        if (user is null)
        {
            _logger.LogInformation("Login failed for {Username}: user not found", request.Username);
            return Fail();
        }

        // Rule 2: password matches. A user created before a password was ever set has a
        // null PasswordHash, which fails here rather than reaching the hasher.
        if (user.PasswordHash is null || !_passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
        {
            _logger.LogInformation("Login failed for {Username}: wrong password", request.Username);
            return Fail();
        }

        // Rule 3: account is active.
        if (!user.IsActive)
        {
            _logger.LogInformation("Login failed for {Username}: inactive account", request.Username);
            return Fail();
        }

        // Rule 4: the selected Login Type matches the user's assigned role.
        var role = await _roleRepository.GetByIdAsync(user.RoleId, cancellationToken);
        if (role is null || !LoginTypes.RoleMatches(request.LoginType, role.Name))
        {
            _logger.LogInformation("Login failed for {Username}: login type does not match assigned role", request.Username);
            return Fail();
        }

        user.RecordLogin(DateTime.UtcNow);
        await _userRepository.SaveChangesAsync(cancellationToken);

        var (token, expiresInSeconds) = _jwtTokenGenerator.GenerateToken(
            user.Id, user.Username, role.Id, role.Name, request.LoginType);

        _logger.LogInformation("User {UserId} logged in", user.Id);

        return Result<LoginResponse>.Success(new LoginResponse
        {
            Token = token,
            ExpiresIn = expiresInSeconds,
            User = new LoginUserResponse
            {
                Id = user.Id,
                Username = user.Username,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                RoleId = role.Id,
                RoleName = role.Name,
                LoginType = request.LoginType,
                ProfilePhotoUrl = user.ProfilePhotoUrl,
            },
        });

        static Result<LoginResponse> Fail() =>
            Result<LoginResponse>.Failure(AuthenticationErrorCodes.InvalidLogin, GenericInvalidLoginMessage);
    }
}
