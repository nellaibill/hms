using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using HMS.Modules.Platform.Application.Abstractions;
using HMS.Modules.Platform.Contracts;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace HMS.Modules.Platform.Infrastructure;

/// <summary>
/// Issues signed (HMAC-SHA256) JWTs for Platform Admins. Reads the same Jwt:Issuer/Audience/
/// SigningKey/ExpiresInMinutes config HMS.Modules.Identity's JwtTokenGenerator already uses —
/// one bearer scheme for the whole host, not a second auth pipeline — but issues a distinct
/// claim shape (PlatformUserId/Email/FullName/PlatformRole/LoginType="platform", no
/// RoleId/RoleName) so a platform token can never be mistaken for a hospital one. HMS.Api's
/// JwtConfiguration enforces LoginType=="platform" on Platform-only endpoints, and
/// PlatformRole=="SuperAdmin" on the subset of those that are destructive, via authorization
/// policies.
/// </summary>
internal sealed class PlatformJwtTokenGenerator : IPlatformJwtTokenGenerator
{
    private readonly string _issuer;
    private readonly string _audience;
    private readonly SigningCredentials _signingCredentials;
    private readonly int _expiresInMinutes;

    public PlatformJwtTokenGenerator(IConfiguration configuration)
    {
        _issuer = configuration["Jwt:Issuer"]
            ?? throw new InvalidOperationException("Missing 'Jwt:Issuer' configuration value.");
        _audience = configuration["Jwt:Audience"]
            ?? throw new InvalidOperationException("Missing 'Jwt:Audience' configuration value.");
        var signingKey = configuration["Jwt:SigningKey"]
            ?? throw new InvalidOperationException("Missing 'Jwt:SigningKey' configuration value.");
        _expiresInMinutes = configuration.GetValue<int?>("Jwt:ExpiresInMinutes") ?? 60;

        _signingCredentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
            SecurityAlgorithms.HmacSha256);
    }

    public (string Token, int ExpiresInSeconds) GenerateToken(Guid platformUserId, string email, string fullName, PlatformRole role)
    {
        var claims = new[]
        {
            new Claim("PlatformUserId", platformUserId.ToString()),
            new Claim("Email", email),
            new Claim("FullName", fullName),
            new Claim("PlatformRole", role.ToString()),
            new Claim("LoginType", "platform"),
            // Server-side revocation (HMS Security Hardening) needs some way to identify
            // *this specific token* independent of its content — the standard "jti" claim
            // (a literal claim type, like every other claim here — see MapInboundClaims
            // false above), checked against IRevokedTokenStore on every request
            // (JwtConfiguration) and written to it on logout (PlatformAuthController).
            new Claim("jti", Guid.NewGuid().ToString()),
        };

        var expiresAt = DateTime.UtcNow.AddMinutes(_expiresInMinutes);
        var token = new JwtSecurityToken(
            issuer: _issuer,
            audience: _audience,
            claims: claims,
            expires: expiresAt,
            signingCredentials: _signingCredentials);

        var tokenValue = new JwtSecurityTokenHandler().WriteToken(token);
        return (tokenValue, _expiresInMinutes * 60);
    }
}
