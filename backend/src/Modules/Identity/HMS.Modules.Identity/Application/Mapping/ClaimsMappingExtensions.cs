using System.Security.Claims;

namespace HMS.Modules.Identity.Application.Mapping;

internal static class ClaimsMappingExtensions
{
    /// <summary>
    /// Flattens a principal's claims into one entry per claim type for GET /api/v1/auth/me.
    /// A token carries one Permission (and one Feature) claim per grant, so a plain
    /// ToDictionary on claim type throws for any user with more than one — repeated types are
    /// joined into a single comma-separated value instead, in token order. Permission and
    /// feature keys never contain commas, so the joined value splits back losslessly.
    /// </summary>
    public static Dictionary<string, string> ToClaimsDictionary(this IEnumerable<Claim> claims)
    {
        return claims
            .GroupBy(claim => claim.Type)
            .ToDictionary(group => group.Key, group => string.Join(",", group.Select(claim => claim.Value)));
    }
}
