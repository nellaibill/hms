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

    /// <summary>Sensitive-write endpoints beyond login that the global limiter alone doesn't
    /// specifically protect — message-send, notification broadcast, and file-upload actions
    /// (ADR-076). Partitioned per caller (<see cref="RateLimitPartitionKeys.Caller"/>), not
    /// per IP, since UseAuthentication() now runs before the limiter (ADR-084) and a hospital
    /// behind one NAT'd IP would otherwise share a single write budget.</summary>
    public const string Write = "Write";
}
