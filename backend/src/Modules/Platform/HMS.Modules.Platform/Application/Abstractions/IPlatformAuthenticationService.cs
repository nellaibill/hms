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
}
