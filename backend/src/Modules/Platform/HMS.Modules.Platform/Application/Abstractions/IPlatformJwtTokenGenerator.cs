using HMS.Modules.Platform.Contracts;

namespace HMS.Modules.Platform.Application.Abstractions;

internal interface IPlatformJwtTokenGenerator
{
    (string Token, int ExpiresInSeconds) GenerateToken(Guid platformUserId, string email, string fullName, PlatformRole role);
}
