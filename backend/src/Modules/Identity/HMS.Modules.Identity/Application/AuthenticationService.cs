using HMS.Modules.Identity.Application.Abstractions;
using HMS.Modules.Identity.Contracts;
using HMS.Shared.Kernel;
using Microsoft.Extensions.Logging;

namespace HMS.Modules.Identity.Application;

/// <summary>
/// Orchestrates the Login use case. Every rejection reason (rules 1-5 below) returns the
/// same generic <see cref="AuthenticationErrorCodes.InvalidLogin"/> failure — never
/// revealing which check failed, per standard login-security practice (docs/ApiStandards.md
/// §8). Soft-deleted users need no special handling: IUserRepository.GetByUsernameAsync
/// already excludes them via User's EF global query filter, so they naturally fail rule 1
/// exactly like a username that never existed.
///
/// HMS Multi-Tenancy Phase C: by the time this runs, <see cref="ITenantContext"/> has
/// already been resolved and populated by HMS.Api's TenantResolutionMiddleware (from the
/// X-Hospital-Code header on this same login request — see that middleware's own doc
/// comment for why tenant resolution can't happen here, inside this service, instead).
/// IUserRepository/IdentityDbContext are already tenant-aware by the time this method's
/// first line runs.
/// </summary>
internal class AuthenticationService : IAuthenticationService
{
    private const string GenericInvalidLoginMessage = "Invalid username or password.";

    /// <summary>Brute-force throttling thresholds (HMS Security Hardening: login had no
    /// account lockout at all). Not user-configurable — a single deployment target, no
    /// evidence yet of needing per-environment tuning.</summary>
    private const int MaxFailedLoginAttempts = 5;
    private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<AuthenticationService> _logger;

    public AuthenticationService(
        IUserRepository userRepository,
        IRoleRepository roleRepository,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator jwtTokenGenerator,
        ITenantContext tenantContext,
        ILogger<AuthenticationService> logger)
    {
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _passwordHasher = passwordHasher;
        _jwtTokenGenerator = jwtTokenGenerator;
        _tenantContext = tenantContext;
        _logger = logger;
    }

    public async Task<Result<LoginResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        if (!_tenantContext.IsResolved)
        {
            // Should be unreachable in production — TenantResolutionMiddleware always
            // resolves the tenant before this action runs (see its own doc comment). A
            // hard failure here, not a generic login rejection, since it means the request
            // reached this method through some path other than that middleware.
            throw new InvalidOperationException("LoginAsync was called without a tenant having been resolved for this request.");
        }

        // Rule 1: user exists.
        var user = await _userRepository.GetByUsernameAsync(request.Username, cancellationToken);
        if (user is null)
        {
            _logger.LogInformation("Login failed for {Username}: user not found", request.Username);
            return Fail();
        }

        // Rule 2 (brute-force throttling): a prior run of wrong-password attempts already
        // locked this account out. Rejected before even touching the password hasher — a
        // locked-out account gains nothing from an accurate password.
        var now = DateTime.UtcNow;
        if (user.IsLockedOut(now))
        {
            _logger.LogWarning("Login failed for {Username}: account is locked out until {LockedOutUntil}", request.Username, user.LockedOutUntil);
            return Fail();
        }

        // Rule 3: password matches. A user created before a password was ever set has a
        // null PasswordHash, which fails here rather than reaching the hasher.
        if (user.PasswordHash is null || !_passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
        {
            user.RecordFailedLogin(now, MaxFailedLoginAttempts, LockoutDuration);
            await _userRepository.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Login failed for {Username}: wrong password ({FailedAttempts}/{MaxAttempts} failed attempts)",
                request.Username,
                user.FailedLoginAttempts,
                MaxFailedLoginAttempts);
            return Fail();
        }

        // Rule 4: account is active.
        if (!user.IsActive)
        {
            _logger.LogInformation("Login failed for {Username}: inactive account", request.Username);
            return Fail();
        }

        // Rule 5: the selected Login Type matches the user's assigned role.
        var role = await _roleRepository.GetByIdAsync(user.RoleId, cancellationToken);
        if (role is null || !LoginTypes.RoleMatches(request.LoginType, role.Name))
        {
            _logger.LogInformation("Login failed for {Username}: login type does not match assigned role", request.Username);
            return Fail();
        }

        user.RecordLogin(now);
        await _userRepository.SaveChangesAsync(cancellationToken);

        var permissionKeys = role.RolePermissions
            .Where(rp => rp.Permission.IsActive && !rp.Permission.IsDeleted)
            .Select(rp => rp.Permission.Key)
            .ToList();

        var (token, expiresInSeconds) = _jwtTokenGenerator.GenerateToken(
            user.Id, user.Username, role.Id, role.Name, request.LoginType, permissionKeys, _tenantContext.TenantId!.Value);

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
                PermissionKeys = permissionKeys,
            },
        });

        static Result<LoginResponse> Fail() =>
            Result<LoginResponse>.Failure(AuthenticationErrorCodes.InvalidLogin, GenericInvalidLoginMessage);
    }
}
