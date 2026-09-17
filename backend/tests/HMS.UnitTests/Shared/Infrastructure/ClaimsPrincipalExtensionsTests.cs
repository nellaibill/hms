using System.Security.Claims;
using FluentAssertions;
using HMS.Shared.Infrastructure;
using Xunit;

namespace HMS.UnitTests.Shared.Infrastructure;

public class ClaimsPrincipalExtensionsTests
{
    private static ClaimsPrincipal PrincipalWith(Guid? consultantId, string? roleName)
    {
        var claims = new List<Claim>();
        if (consultantId is not null)
        {
            claims.Add(new Claim("ConsultantId", consultantId.Value.ToString()));
        }
        if (roleName is not null)
        {
            claims.Add(new Claim("RoleName", roleName));
        }

        return new ClaimsPrincipal(new ClaimsIdentity(claims));
    }

    [Fact]
    public void GetScopedConsultantId_ReturnsNull_WhenNoConsultantIdClaim()
    {
        var user = PrincipalWith(consultantId: null, roleName: "Doctor / Consultant");

        user.GetScopedConsultantId().Should().BeNull();
    }

    [Fact]
    public void GetScopedConsultantId_ReturnsNull_WhenLinkedButRoleIsNotConsultant()
    {
        // Mirrors a Super Admin or Admin who happens to also be linked to a consultant
        // record — the link is independent of role (User.ConsultantId), so it must not
        // restrict an administrative role's visibility.
        var consultantId = Guid.NewGuid();
        var user = PrincipalWith(consultantId, roleName: "Hospital Administrator");

        user.GetScopedConsultantId().Should().BeNull();
    }

    [Fact]
    public void GetScopedConsultantId_ReturnsConsultantId_WhenLinkedAndRoleContainsConsultant()
    {
        var consultantId = Guid.NewGuid();
        var user = PrincipalWith(consultantId, roleName: "Doctor / Consultant");

        user.GetScopedConsultantId().Should().Be(consultantId);
    }

    [Fact]
    public void GetScopedConsultantId_MatchesRoleNameCaseInsensitively()
    {
        var consultantId = Guid.NewGuid();
        var user = PrincipalWith(consultantId, roleName: "CONSULTANT");

        user.GetScopedConsultantId().Should().Be(consultantId);
    }

    [Fact]
    public void GetConsultantId_ReturnsNull_WhenClaimIsMissing()
    {
        var user = PrincipalWith(consultantId: null, roleName: null);

        user.GetConsultantId().Should().BeNull();
    }
}
