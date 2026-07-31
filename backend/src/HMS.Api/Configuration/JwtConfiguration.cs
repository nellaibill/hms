using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace HMS.Api.Configuration;

/// <summary>
/// JWT bearer authentication for the whole host. Reads the same Jwt:Issuer/Audience/
/// SigningKey keys that HMS.Modules.Identity's JwtTokenGenerator uses to issue tokens, so
/// issuing and validating always agree. Registered here (not ModuleRegistration): it's
/// host-level pipeline configuration, like CorsConfiguration/SwaggerConfiguration.
/// </summary>
public static class JwtConfiguration
{
    public static IServiceCollection AddHmsJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var issuer = configuration["Jwt:Issuer"]
            ?? throw new InvalidOperationException("Missing 'Jwt:Issuer' configuration value.");
        var audience = configuration["Jwt:Audience"]
            ?? throw new InvalidOperationException("Missing 'Jwt:Audience' configuration value.");
        var signingKey = configuration["Jwt:SigningKey"]
            ?? throw new InvalidOperationException("Missing 'Jwt:SigningKey' configuration value.");

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                // Keeps claim type names exactly as JwtTokenGenerator issued them (e.g.
                // "UserId", "RoleName") — without this, JwtSecurityTokenHandler silently
                // remaps well-known short names (like "sub") to long
                // http://schemas.microsoft.com/... ClaimTypes URIs on the way in.
                options.MapInboundClaims = false;

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = issuer,
                    ValidateAudience = true,
                    ValidAudience = audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromSeconds(30),
                };
            });

        services.AddAuthorization();

        return services;
    }
}
