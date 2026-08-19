using HMS.Modules.Platform.Application.Abstractions;
using HMS.Modules.Platform.Contracts;
using HMS.Shared.Kernel;
using Microsoft.Extensions.Logging;

namespace HMS.Modules.Platform.Application;

/// <summary>
/// Orchestrates the Platform Admin login use case. Every rejection reason returns the same
/// generic InvalidLogin failure — never revealing which check failed, matching
/// HMS.Modules.Identity.Application.AuthenticationService's convention.
/// </summary>
internal class PlatformAuthenticationService : IPlatformAuthenticationService
{
    private const string GenericInvalidLoginMessage = "Invalid email or password.";

    /// <summary>Brute-force throttling thresholds — mirrors
    /// HMS.Modules.Identity.Application.AuthenticationService's identical constants.</summary>
    private const int MaxFailedLoginAttempts = 5;
    private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

    private readonly IPlatformUserRepository _repository;
    private readonly IPlatformPasswordHasher _passwordHasher;
    private readonly IPlatformJwtTokenGenerator _jwtTokenGenerator;
    private readonly ILogger<PlatformAuthenticationService> _logger;

    public PlatformAuthenticationService(
        IPlatformUserRepository repository,
        IPlatformPasswordHasher passwordHasher,
        IPlatformJwtTokenGenerator jwtTokenGenerator,
        ILogger<PlatformAuthenticationService> logger)
    {
        _repository = repository;
        _passwordHasher = passwordHasher;
        _jwtTokenGenerator = jwtTokenGenerator;
        _logger = logger;
    }

    public async Task<Result<PlatformLoginResponse>> LoginAsync(PlatformLoginRequest request, CancellationToken cancellationToken)
    {
        var user = await _repository.GetByEmailAsync(request.Email, cancellationToken);
        if (user is null)
        {
            _logger.LogInformation("Platform login failed for {Email}: invalid credentials", request.Email);
            return Fail();
        }

        // Brute-force throttling: a prior run of wrong-password attempts already locked this
        // account out. Rejected before even touching the password hasher.
        var now = DateTime.UtcNow;
        if (user.IsLockedOut(now))
        {
            _logger.LogWarning("Platform login failed for {Email}: account is locked out until {LockedOutUntil}", request.Email, user.LockedOutUntil);
            return Fail();
        }

        if (!_passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
        {
            user.RecordFailedLogin(now, MaxFailedLoginAttempts, LockoutDuration);
            await _repository.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Platform login failed for {Email}: invalid credentials ({FailedAttempts}/{MaxAttempts} failed attempts)",
                request.Email,
                user.FailedLoginAttempts,
                MaxFailedLoginAttempts);
            return Fail();
        }

        if (!user.IsActive)
        {
            _logger.LogInformation("Platform login failed for {Email}: inactive account", request.Email);
            return Fail();
        }

        user.RecordSuccessfulLogin();
        await _repository.SaveChangesAsync(cancellationToken);

        var (token, expiresInSeconds) = _jwtTokenGenerator.GenerateToken(user.Id, user.Email, user.FullName, user.Role);

        _logger.LogInformation("Platform user {PlatformUserId} logged in", user.Id);

        return Result<PlatformLoginResponse>.Success(new PlatformLoginResponse
        {
            Token = token,
            ExpiresIn = expiresInSeconds,
            User = new PlatformLoginUserResponse
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                Role = user.Role.ToString(),
            },
        });

        static Result<PlatformLoginResponse> Fail() =>
            Result<PlatformLoginResponse>.Failure(PlatformErrorCodes.InvalidLogin, GenericInvalidLoginMessage);
    }
}
