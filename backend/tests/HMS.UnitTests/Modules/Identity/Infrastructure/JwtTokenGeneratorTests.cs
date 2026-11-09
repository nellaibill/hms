using System.IdentityModel.Tokens.Jwt;
using System.Text;
using FluentAssertions;
using HMS.Modules.Identity.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace HMS.UnitTests.Modules.Identity.Infrastructure;

public class JwtTokenGeneratorTests
{
    private static JwtTokenGenerator CreateSut(int expiresInMinutes = 60)
    {
        // Jwt:ExpiresInMinutes is set (as in appsettings.json) to prove the hospital
        // generator ignores it — it only applies to Platform tokens (ADR-131).
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

        var tenantId = Guid.NewGuid();

        var token = sut.GenerateToken(userId, "dr.ada", roleId, "Doctor / Consultant", "doctor", [], tenantId, []);

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        jwt.Claims.Should().Contain(c => c.Type == "UserId" && c.Value == userId.ToString());
        jwt.Claims.Should().Contain(c => c.Type == "Username" && c.Value == "dr.ada");
        jwt.Claims.Should().Contain(c => c.Type == "RoleId" && c.Value == roleId.ToString());
        jwt.Claims.Should().Contain(c => c.Type == "RoleName" && c.Value == "Doctor / Consultant");
        jwt.Claims.Should().Contain(c => c.Type == "LoginType" && c.Value == "doctor");
        jwt.Claims.Should().Contain(c => c.Type == "TenantId" && c.Value == tenantId.ToString());
    }

    [Fact]
    public void GenerateToken_IncludesOnePermissionClaimPerKey()
    {
        var sut = CreateSut();

        var token = sut.GenerateToken(
            Guid.NewGuid(), "dr.ada", Guid.NewGuid(), "Doctor / Consultant", "doctor",
            ["patient-management.view", "patient-management.create"], Guid.NewGuid(), []);

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        jwt.Claims.Should().Contain(c => c.Type == "Permission" && c.Value == "patient-management.view");
        jwt.Claims.Should().Contain(c => c.Type == "Permission" && c.Value == "patient-management.create");
        jwt.Claims.Count(c => c.Type == "Permission").Should().Be(2);
    }

    [Fact]
    public void GenerateToken_IncludesOneFeatureClaimPerKey()
    {
        var sut = CreateSut();

        var token = sut.GenerateToken(
            Guid.NewGuid(), "dr.ada", Guid.NewGuid(), "Doctor / Consultant", "doctor",
            [], Guid.NewGuid(), ["patients", "ipd"]);

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        jwt.Claims.Should().Contain(c => c.Type == "Feature" && c.Value == "patients");
        jwt.Claims.Should().Contain(c => c.Type == "Feature" && c.Value == "ipd");
        jwt.Claims.Count(c => c.Type == "Feature").Should().Be(2);
    }

    [Fact]
    public void GenerateToken_IncludesConsultantIdClaim_WhenConsultantIdIsProvided()
    {
        var sut = CreateSut();
        var consultantId = Guid.NewGuid();

        var token = sut.GenerateToken(
            Guid.NewGuid(), "dr.ada", Guid.NewGuid(), "Doctor / Consultant", "doctor", [], Guid.NewGuid(), [], consultantId);

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        jwt.Claims.Should().Contain(c => c.Type == "ConsultantId" && c.Value == consultantId.ToString());
    }

    [Fact]
    public void GenerateToken_OmitsConsultantIdClaim_WhenConsultantIdIsNull()
    {
        var sut = CreateSut();

        var token = sut.GenerateToken(
            Guid.NewGuid(), "dr.ada", Guid.NewGuid(), "Doctor / Consultant", "doctor", [], Guid.NewGuid(), [], consultantId: null);

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        jwt.Claims.Should().NotContain(c => c.Type == "ConsultantId");
    }

    [Fact]
    public void GenerateToken_SetsIssuerAndAudienceFromConfiguration()
    {
        var sut = CreateSut();

        var token = sut.GenerateToken(Guid.NewGuid(), "dr.ada", Guid.NewGuid(), "Doctor / Consultant", "doctor", [], Guid.NewGuid(), []);

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        jwt.Issuer.Should().Be("HMS");
        jwt.Audiences.Should().Contain("HMS.Clients");
    }

    [Fact]
    public void GenerateToken_IssuesATokenThatNeverExpires()
    {
        var sut = CreateSut(expiresInMinutes: 30);

        var token = sut.GenerateToken(Guid.NewGuid(), "dr.ada", Guid.NewGuid(), "Doctor / Consultant", "doctor", [], Guid.NewGuid(), []);

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        jwt.Payload.Expiration.Should().BeNull();
        jwt.Claims.Should().NotContain(c => c.Type == "exp");
    }

    [Fact]
    public void GenerateToken_PassesValidationThatAllowsAMissingExpiry()
    {
        // Same lifetime settings as HMS.Api's JwtConfiguration (ADR-131).
        const string signingKey = "unit-test-signing-key-at-least-32-bytes-long!";
        var sut = CreateSut();
        var token = sut.GenerateToken(Guid.NewGuid(), "dr.ada", Guid.NewGuid(), "Doctor / Consultant", "doctor", [], Guid.NewGuid(), []);

        var parameters = new TokenValidationParameters
        {
            ValidIssuer = "HMS",
            ValidAudience = "HMS.Clients",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
            ValidateLifetime = true,
            RequireExpirationTime = false,
        };

        var act = () => new JwtSecurityTokenHandler().ValidateToken(token, parameters, out _);

        act.Should().NotThrow();
    }
}
