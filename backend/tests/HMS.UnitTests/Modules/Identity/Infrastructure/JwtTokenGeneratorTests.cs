using System.IdentityModel.Tokens.Jwt;
using FluentAssertions;
using HMS.Modules.Identity.Infrastructure;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace HMS.UnitTests.Modules.Identity.Infrastructure;

public class JwtTokenGeneratorTests
{
    private static JwtTokenGenerator CreateSut(int expiresInMinutes = 60)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Issuer"] = "HMS",
                ["Jwt:Audience"] = "HMS.Clients",
                ["Jwt:SigningKey"] = "unit-test-signing-key-at-least-32-bytes-long!",
                ["Jwt:ExpiresInMinutes"] = expiresInMinutes.ToString(),
            })
            .Build();

        return new JwtTokenGenerator(configuration);
    }

    [Fact]
    public void GenerateToken_IncludesAllFiveRequiredClaims()
    {
        var sut = CreateSut();
        var userId = Guid.NewGuid();
        var roleId = Guid.NewGuid();

        var (token, _) = sut.GenerateToken(userId, "dr.ada", roleId, "Doctor / Consultant", "doctor");

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        jwt.Claims.Should().Contain(c => c.Type == "UserId" && c.Value == userId.ToString());
        jwt.Claims.Should().Contain(c => c.Type == "Username" && c.Value == "dr.ada");
        jwt.Claims.Should().Contain(c => c.Type == "RoleId" && c.Value == roleId.ToString());
        jwt.Claims.Should().Contain(c => c.Type == "RoleName" && c.Value == "Doctor / Consultant");
        jwt.Claims.Should().Contain(c => c.Type == "LoginType" && c.Value == "doctor");
    }

    [Fact]
    public void GenerateToken_SetsIssuerAndAudienceFromConfiguration()
    {
        var sut = CreateSut();

        var (token, _) = sut.GenerateToken(Guid.NewGuid(), "dr.ada", Guid.NewGuid(), "Doctor / Consultant", "doctor");

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        jwt.Issuer.Should().Be("HMS");
        jwt.Audiences.Should().Contain("HMS.Clients");
    }

    [Fact]
    public void GenerateToken_ReturnsExpiresInSecondsMatchingConfiguredMinutes()
    {
        var sut = CreateSut(expiresInMinutes: 30);

        var (_, expiresInSeconds) = sut.GenerateToken(Guid.NewGuid(), "dr.ada", Guid.NewGuid(), "Doctor / Consultant", "doctor");

        expiresInSeconds.Should().Be(30 * 60);
    }

    [Fact]
    public void GenerateToken_SetsExpiryApproximatelyExpiresInMinutesFromNow()
    {
        var sut = CreateSut(expiresInMinutes: 60);

        var (token, _) = sut.GenerateToken(Guid.NewGuid(), "dr.ada", Guid.NewGuid(), "Doctor / Consultant", "doctor");

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        jwt.ValidTo.Should().BeCloseTo(DateTime.UtcNow.AddMinutes(60), TimeSpan.FromMinutes(1));
    }
}
