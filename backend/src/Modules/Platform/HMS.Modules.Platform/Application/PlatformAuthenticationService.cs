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
        if (user is null || !_passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
        {
            _logger.LogInformation("Platform login failed for {Email}: invalid credentials", request.Email);
            return Fail();
        }

        if (!user.IsActive)
        {
            _logger.LogInformation("Platform login failed for {Email}: inactive account", request.Email);
            return Fail();
        }

        var (token, expiresInSeconds) = _jwtTokenGenerator.GenerateToken(user.Id, user.Email, user.FullName);

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
            },
        });

        static Result<PlatformLoginResponse> Fail() =>
            Result<PlatformLoginResponse>.Failure(PlatformErrorCodes.InvalidLogin, GenericInvalidLoginMessage);
    }
}
