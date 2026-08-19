using System.Security.Claims;

namespace HMS.Shared.Infrastructure;

public static class ClaimsPrincipalExtensions
{
    /// <summary>
    /// The authenticated caller's user id, read from the "UserId" claim
    /// (HMS.Modules.Identity.Infrastructure.JwtTokenGenerator issues it as a literal claim
    /// type, not a ClaimTypes URI — see JwtConfiguration's MapInboundClaims = false). Null
    /// if the claim is missing or malformed, e.g. an anonymous request to an endpoint that
    /// allows it.
    /// </summary>
    public static Guid? GetUserId(this ClaimsPrincipal user)
        => Guid.TryParse(user.FindFirst("UserId")?.Value, out var userId) ? userId : null;

    /// <summary>
    /// The authenticated Platform Admin's id, read from the "PlatformUserId" claim
    /// (HMS.Modules.Platform.Infrastructure.PlatformJwtTokenGenerator issues it as a literal
    /// claim type, not a ClaimTypes URI). Null if the claim is missing or malformed, e.g. a
    /// hospital-user token presented to a Platform-only endpoint.
    /// </summary>
    public static Guid? GetPlatformUserId(this ClaimsPrincipal user)
        => Guid.TryParse(user.FindFirst("PlatformUserId")?.Value, out var platformUserId) ? platformUserId : null;
}
