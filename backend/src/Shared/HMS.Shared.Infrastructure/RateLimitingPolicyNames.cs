namespace HMS.Shared.Infrastructure;

/// <summary>
/// Named rate-limiting policy identifiers shared between HMS.Api (which registers the
/// policies — see RateLimitingConfiguration) and the module controllers that apply them via
/// <c>[EnableRateLimiting(...)]</c>. Lives here, not in HMS.Api.Configuration, because
/// modules must never reference HMS.Api (dependency direction is Api → Modules only).
/// </summary>
public static class RateLimitingPolicyNames
{
    public const string Login = "Login";
}
