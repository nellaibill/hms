using HMS.Modules.Platform.Contracts;
using HMS.Shared.Kernel;

namespace HMS.Modules.Platform.Application.Abstractions;

/// <summary>
/// Public (not internal): PlatformAuthController — which ASP.NET Core requires to be a
/// public class with a public constructor for controller discovery/DI activation — takes
/// this as a constructor dependency; a public constructor cannot have an internal
/// parameter type (CS0051).
/// </summary>
public interface IPlatformAuthenticationService
{
    Task<Result<PlatformLoginResponse>> LoginAsync(PlatformLoginRequest request, CancellationToken cancellationToken);

    /// <summary>Second step of a two-step MFA login — exchanges a challenge token
    /// LoginAsync issued for the real bearer token once the TOTP code checks out.</summary>
    Task<Result<PlatformLoginResponse>> VerifyMfaAsync(PlatformMfaVerifyRequest request, CancellationToken cancellationToken);

    /// <summary>Whether the given Platform Admin's own account currently has MFA enabled.</summary>
    Task<Result<PlatformMfaStatusResponse>> GetMfaStatusAsync(Guid platformUserId, CancellationToken cancellationToken);

    /// <summary>Starts (or restarts) MFA setup for the given Platform Admin's own account.</summary>
    Task<Result<PlatformMfaSetupResponse>> SetupMfaAsync(Guid platformUserId, CancellationToken cancellationToken);

    /// <summary>Confirms a pending setup, turning MfaEnabled on.</summary>
    Task<Result> EnableMfaAsync(Guid platformUserId, string code, CancellationToken cancellationToken);

    /// <summary>Turns MFA back off, after proving the caller still controls the authenticator.</summary>
    Task<Result> DisableMfaAsync(Guid platformUserId, string code, CancellationToken cancellationToken);

    /// <summary>Self-service password change for the given Platform Admin's own account —
    /// verifies the current password before rotating it. Mirrors
    /// HMS.Modules.Identity.Application.IAuthenticationService.ChangePasswordAsync.</summary>
    Task<Result> ChangePasswordAsync(Guid platformUserId, string currentPassword, string newPassword, CancellationToken cancellationToken);
}
