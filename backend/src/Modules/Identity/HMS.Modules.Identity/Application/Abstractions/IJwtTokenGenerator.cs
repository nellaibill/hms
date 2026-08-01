namespace HMS.Modules.Identity.Application.Abstractions;

/// <summary>
/// Defined here (Application) and implemented in Infrastructure, per the dependency
/// inversion rule — Application never references the JWT library's types directly.
/// Takes primitive claim values rather than the User/Role domain entities, so callers
/// don't need to construct one just to generate a token (see JwtTokenGeneratorTests).
/// </summary>
internal interface IJwtTokenGenerator
{
    (string Token, int ExpiresInSeconds) GenerateToken(
        Guid userId,
        string username,
        Guid roleId,
        string roleName,
        string loginType);
}
