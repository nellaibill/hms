using HMS.Modules.Identity.Contracts;
using HMS.Shared.Kernel;

namespace HMS.Modules.Identity.Application;

/// <summary>
/// Public (not internal), for the same reason as <see cref="IUserService"/>:
/// AuthenticationController's public constructor needs a public parameter type.
/// <see cref="AuthenticationService"/> stays internal.
/// </summary>
public interface IAuthenticationService
{
    Task<Result<LoginResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken);

    /// <summary>Self-service password change for the currently-authenticated user (Rule 1
    /// in AuthenticationService.ChangePasswordAsync's own doc comment covers what "currently
    /// authenticated" means for a caller with no PasswordHash yet).</summary>
    Task<Result> ChangePasswordAsync(Guid userId, string currentPassword, string newPassword, CancellationToken cancellationToken);
}
