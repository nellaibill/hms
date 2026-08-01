using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using HMS.Modules.Identity.Application.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace HMS.Modules.Identity.Infrastructure;

/// <summary>
/// Issues signed (HMAC-SHA256) JWTs. Reads Jwt:Issuer/Audience/SigningKey/ExpiresInMinutes
/// directly from IConfiguration in the constructor, matching how IdentityModule already
/// reads ConnectionStrings:Default — no IOptions&lt;T&gt; wrapper, since nothing else in this
/// codebase uses that pattern yet. HMS.Api's JwtConfiguration reads the same keys to
/// configure bearer-token validation, so issuing and validating always agree.
/// </summary>
internal sealed class JwtTokenGenerator : IJwtTokenGenerator
{
    private readonly string _issuer;
    private readonly string _audience;
    private readonly SigningCredentials _signingCredentials;
    private readonly int _expiresInMinutes;

    public JwtTokenGenerator(IConfiguration configuration)
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

    public (string Token, int ExpiresInSeconds) GenerateToken(
        Guid userId,
        string username,
        Guid roleId,
        string roleName,
        string loginType)
    {
        // Literal claim type names (UserId/Username/RoleId/RoleName/LoginType), not the
        // long http://schemas.microsoft.com/... URIs JwtSecurityTokenHandler's default
        // inbound claim map would otherwise remap well-known types to — HMS.Api's
        // JwtConfiguration disables that mapping so these names survive validation intact.
        var claims = new[]
        {
            new Claim("UserId", userId.ToString()),
            new Claim("Username", username),
            new Claim("RoleId", roleId.ToString()),
            new Claim("RoleName", roleName),
            new Claim("LoginType", loginType),
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
