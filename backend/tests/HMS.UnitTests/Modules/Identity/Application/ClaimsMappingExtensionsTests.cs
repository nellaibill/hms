using System.Security.Claims;
using FluentAssertions;
using HMS.Modules.Identity.Application.Mapping;
using Xunit;

namespace HMS.UnitTests.Modules.Identity.Application;

public class ClaimsMappingExtensionsTests
{
    [Fact]
    public void ToClaimsDictionary_JoinsRepeatedClaimTypes_InsteadOfThrowing()
    {
        Claim[] claims =
        [
            new("Username", "lhsadmin"),
            new("Permission", "patients.view"),
            new("Permission", "patients.create"),
            new("Permission", "finance-billing.view"),
        ];

        var dictionary = claims.ToClaimsDictionary();

        dictionary.Should().HaveCount(2);
        dictionary["Username"].Should().Be("lhsadmin");
        dictionary["Permission"].Should().Be("patients.view,patients.create,finance-billing.view");
    }

    [Fact]
    public void ToClaimsDictionary_KeepsSingleValuedClaimsAsIs()
    {
        Claim[] claims = [new("UserId", "abc"), new("RoleName", "Super Admin")];

        claims.ToClaimsDictionary().Should().BeEquivalentTo(new Dictionary<string, string>
        {
            ["UserId"] = "abc",
            ["RoleName"] = "Super Admin",
        });
    }
}
