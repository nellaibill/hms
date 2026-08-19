namespace HMS.Modules.Platform.Domain;

/// <summary>
/// The second leg of a two-step Platform Admin login: PlatformAuthenticationService.LoginAsync
/// issues one of these instead of a real JWT once the password checks out for an MFA-enabled
/// account, and PlatformAuthController's MFA-verify endpoint exchanges the opaque
/// <see cref="Token"/> plus a TOTP code for the real token. Mirrors RevokedToken/
/// IdempotencyRecord's shape — a short-lived operational record, not an
/// <see cref="HMS.Shared.Kernel.Entity"/>. Deliberately single-use and short-lived (a handful
/// of minutes): unlike a real session token, this only ever proves "the password step just
/// passed," so a wide validity window would widen the brute-force window on the 6-digit code.
/// </summary>
internal sealed class PlatformMfaChallenge
{
    public Guid Id { get; private set; }
    public Guid PlatformUserId { get; private set; }
    public string Token { get; private set; } = null!;
    public DateTime ExpiresAt { get; private set; }
    public DateTime? ConsumedAt { get; private set; }

    // Required by EF Core materialization.
    private PlatformMfaChallenge()
    {
    }

    private PlatformMfaChallenge(Guid id, Guid platformUserId, string token, DateTime expiresAt)
    {
        Id = id;
        PlatformUserId = platformUserId;
        Token = token;
        ExpiresAt = expiresAt;
    }

    public static PlatformMfaChallenge Create(Guid platformUserId, string token, DateTime expiresAt) =>
        new(Guid.CreateVersion7(), platformUserId, token, expiresAt);

    public bool IsUsable(DateTime asOf) => ConsumedAt is null && ExpiresAt > asOf;

    public void Consume()
    {
        ConsumedAt = DateTime.UtcNow;
    }
}
