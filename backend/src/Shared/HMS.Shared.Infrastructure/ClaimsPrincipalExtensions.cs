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
}
