namespace HMS.Modules.Platform.Application.Abstractions;

/// <summary>
/// Public seam (like <see cref="ITenantProvisioner"/>) so HMS.Api's JwtConfiguration can
/// check token revocation during JWT validation, and PlatformAuthController can revoke a
/// token on logout, without either needing to know how revocation is stored. See
/// RevokedToken's own doc comment for why this exists.
/// </summary>
public interface IRevokedTokenStore
{
    Task RevokeAsync(string jti, DateTime expiresAt, CancellationToken cancellationToken);

    Task<bool> IsRevokedAsync(string jti, CancellationToken cancellationToken);
}
