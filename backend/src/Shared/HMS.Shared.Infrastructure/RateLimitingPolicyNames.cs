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

    /// <summary>Sensitive-write endpoints beyond login that the global 200/min limiter alone
    /// doesn't specifically protect — message-send, notification broadcast, and file-upload
    /// actions (ADR-076). Same per-IP partitioning as every other policy here, not per-user:
    /// this limiter runs before UseAuthentication() in the pipeline (ADR-018's own reasoning —
    /// an unauthenticated flood shouldn't spend JWT-validation work first), so no verified
    /// user identity exists yet to partition by at this point.</summary>
    public const string Write = "Write";
}
