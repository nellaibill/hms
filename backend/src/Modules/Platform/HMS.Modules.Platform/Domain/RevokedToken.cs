namespace HMS.Modules.Platform.Domain;

/// <summary>
/// A durable record that one specific Platform Admin JWT (by its "jti" claim) must be
/// rejected even though it hasn't naturally expired yet — HMS Security Hardening: previously
/// a leaked or "logged out" Platform token stayed valid until natural JWT expiry, since
/// nothing checked revocation server-side. <see cref="ExpiresAt"/> mirrors the token's own
/// "exp" claim so an expired row can be safely ignored/pruned — no point rejecting a token
/// that would already fail JWT validation on its own. Deliberately not an
/// <see cref="HMS.Shared.Kernel.Entity"/> — an operational record raised by the system, not
/// a business aggregate.
/// </summary>
internal sealed class RevokedToken
{
    public string Jti { get; private set; } = null!;
    public DateTime ExpiresAt { get; private set; }
    public DateTime RevokedAt { get; private set; }

    // Required by EF Core materialization.
    private RevokedToken()
    {
    }

    private RevokedToken(string jti, DateTime expiresAt)
    {
        Jti = jti;
        ExpiresAt = expiresAt;
        RevokedAt = DateTime.UtcNow;
    }

    public static RevokedToken Revoke(string jti, DateTime expiresAt) => new(jti, expiresAt);
}
