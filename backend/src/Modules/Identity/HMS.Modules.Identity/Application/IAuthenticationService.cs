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
}
