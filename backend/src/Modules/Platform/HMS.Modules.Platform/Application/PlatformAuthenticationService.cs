using HMS.Modules.Platform.Application.Abstractions;
using HMS.Modules.Platform.Contracts;
using HMS.Modules.Platform.Domain;
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
    private readonly IPlatformMfaChallengeStore _mfaChallengeStore;
    private readonly ITotpService _totpService;
    private readonly IPlatformMfaSecretProtector _mfaSecretProtector;
    private readonly ILogger<PlatformAuthenticationService> _logger;

    public PlatformAuthenticationService(
        IPlatformUserRepository repository,
        IPlatformPasswordHasher passwordHasher,
        IPlatformJwtTokenGenerator jwtTokenGenerator,
        IPlatformMfaChallengeStore mfaChallengeStore,
        ITotpService totpService,
        IPlatformMfaSecretProtector mfaSecretProtector,
        ILogger<PlatformAuthenticationService> logger)
    {
        _repository = repository;
        _passwordHasher = passwordHasher;
        _jwtTokenGenerator = jwtTokenGenerator;
        _mfaChallengeStore = mfaChallengeStore;
        _totpService = totpService;
        _mfaSecretProtector = mfaSecretProtector;
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

        // Password verified — but for an MFA-enabled account, that's only the first of two
        // factors. Issue a short-lived challenge instead of the real token; the real token
        // is only issued once VerifyMfaAsync confirms the second factor too.
        if (user.MfaEnabled)
        {
            var challengeToken = await _mfaChallengeStore.CreateAsync(user.Id, cancellationToken);

            _logger.LogInformation("Platform user {PlatformUserId} passed the password step; awaiting MFA code", user.Id);

            return Result<PlatformLoginResponse>.Success(new PlatformLoginResponse
            {
                MfaRequired = true,
                MfaChallengeToken = challengeToken,
            });
        }

        return Result<PlatformLoginResponse>.Success(IssueLoginResponse(user));

        static Result<PlatformLoginResponse> Fail() =>
            Result<PlatformLoginResponse>.Failure(PlatformErrorCodes.InvalidLogin, GenericInvalidLoginMessage);
    }

    public async Task<Result<PlatformLoginResponse>> VerifyMfaAsync(PlatformMfaVerifyRequest request, CancellationToken cancellationToken)
    {
        // Deliberately a peek, not a consume: a wrong code must not burn the challenge — the
        // admin can retry as many times as they need until either the code is right or the
        // challenge naturally expires. Only ConsumeAsync (below, once the code checks out)
        // actually invalidates it, closing the replay window on the token itself.
        var platformUserId = await _mfaChallengeStore.ValidateAsync(request.ChallengeToken, cancellationToken);
        if (platformUserId is null)
        {
            _logger.LogInformation("MFA verify failed: challenge token was missing, expired, or already used");
            return Result<PlatformLoginResponse>.Failure(PlatformErrorCodes.MfaChallengeInvalid, "This MFA challenge is invalid or has expired. Please sign in again.");
        }

        var user = await _repository.GetByIdAsync(platformUserId.Value, cancellationToken);
        if (user is null || !user.MfaEnabled || user.MfaSecret is null || !VerifyTotpCode(user.MfaSecret, request.Code))
        {
            _logger.LogInformation("MFA verify failed for platform user {PlatformUserId}: wrong code", platformUserId);
            return Result<PlatformLoginResponse>.Failure(PlatformErrorCodes.InvalidMfaCode, "The verification code is incorrect.");
        }

        await _mfaChallengeStore.ConsumeAsync(request.ChallengeToken, cancellationToken);

        _logger.LogInformation("Platform user {PlatformUserId} completed MFA login", user.Id);

        return Result<PlatformLoginResponse>.Success(IssueLoginResponse(user));
    }

    public async Task<Result<PlatformMfaStatusResponse>> GetMfaStatusAsync(Guid platformUserId, CancellationToken cancellationToken)
    {
        var user = await _repository.GetByIdAsync(platformUserId, cancellationToken);
        if (user is null)
        {
            return Result<PlatformMfaStatusResponse>.Failure(PlatformErrorCodes.InvalidLogin, "Platform user not found.");
        }

        return Result<PlatformMfaStatusResponse>.Success(new PlatformMfaStatusResponse { Enabled = user.MfaEnabled });
    }

    public async Task<Result<PlatformMfaSetupResponse>> SetupMfaAsync(Guid platformUserId, CancellationToken cancellationToken)
    {
        var user = await _repository.GetByIdAsync(platformUserId, cancellationToken);
        if (user is null)
        {
            return Result<PlatformMfaSetupResponse>.Failure(PlatformErrorCodes.InvalidLogin, "Platform user not found.");
        }

        var secret = _totpService.GenerateSecret();
        user.SetPendingMfaSecret(_mfaSecretProtector.Protect(secret));
        await _repository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Platform user {PlatformUserId} started MFA setup", user.Id);

        return Result<PlatformMfaSetupResponse>.Success(new PlatformMfaSetupResponse
        {
            Secret = secret,
            OtpAuthUri = _totpService.BuildOtpAuthUri(secret, user.Email),
        });
    }

    public async Task<Result> EnableMfaAsync(Guid platformUserId, string code, CancellationToken cancellationToken)
    {
        var user = await _repository.GetByIdAsync(platformUserId, cancellationToken);
        if (user is null)
        {
            return Result.Failure(PlatformErrorCodes.InvalidLogin, "Platform user not found.");
        }

        if (user.MfaEnabled)
        {
            return Result.Failure(PlatformErrorCodes.MfaAlreadyEnabled, "MFA is already enabled.");
        }

        if (user.MfaSecret is null || !VerifyTotpCode(user.MfaSecret, code))
        {
            _logger.LogInformation("MFA enable failed for platform user {PlatformUserId}: wrong code", platformUserId);
            return Result.Failure(PlatformErrorCodes.InvalidMfaCode, "The verification code is incorrect.");
        }

        user.EnableMfa();
        await _repository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Platform user {PlatformUserId} enabled MFA", user.Id);

        return Result.Success();
    }

    public async Task<Result> DisableMfaAsync(Guid platformUserId, string code, CancellationToken cancellationToken)
    {
        var user = await _repository.GetByIdAsync(platformUserId, cancellationToken);
        if (user is null)
        {
            return Result.Failure(PlatformErrorCodes.InvalidLogin, "Platform user not found.");
        }

        if (!user.MfaEnabled || user.MfaSecret is null)
        {
            return Result.Failure(PlatformErrorCodes.MfaNotEnabled, "MFA is not enabled.");
        }

        if (!VerifyTotpCode(user.MfaSecret, code))
        {
            _logger.LogInformation("MFA disable failed for platform user {PlatformUserId}: wrong code", platformUserId);
            return Result.Failure(PlatformErrorCodes.InvalidMfaCode, "The verification code is incorrect.");
        }

        user.DisableMfa();
        await _repository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Platform user {PlatformUserId} disabled MFA", user.Id);

        return Result.Success();
    }

    private bool VerifyTotpCode(string encryptedSecret, string code) =>
        _totpService.VerifyCode(_mfaSecretProtector.Unprotect(encryptedSecret), code);

    private PlatformLoginResponse IssueLoginResponse(PlatformUser user)
    {
        var (token, expiresInSeconds) = _jwtTokenGenerator.GenerateToken(user.Id, user.Email, user.FullName, user.Role);

        _logger.LogInformation("Platform user {PlatformUserId} logged in", user.Id);

        return new PlatformLoginResponse
        {
            MfaRequired = false,
            Token = token,
            ExpiresIn = expiresInSeconds,
            User = new PlatformLoginUserResponse
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                Role = user.Role.ToString(),
            },
        };
    }
}
