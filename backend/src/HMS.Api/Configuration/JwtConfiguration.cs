using HMS.Shared.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
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

        services.AddAuthorization(options =>
        {
            // Platform Admin and hospital-user tokens share one bearer scheme (same
            // Jwt:Issuer/Audience/SigningKey) but carry different claim shapes — this
            // policy is the seam that keeps them from being used against each other's
            // endpoints. Hospital tokens never carry a "platform" LoginType claim value.
            options.AddPolicy("Platform", policy => policy.RequireClaim("LoginType", "platform"));

            // A stricter subset of "Platform": destructive/high-privilege Platform Portal
            // actions (register a hospital, enable/disable a hospital, trigger a tenant
            // migration) additionally require the PlatformRole=="SuperAdmin" claim
            // (PlatformJwtTokenGenerator) — a SupportUser-role platform token can still view
            // the dashboard but not perform any of those actions. See docs/DecisionLog.md
            // ADR-014.
            options.AddPolicy("PlatformSuperAdmin", policy => policy
                .RequireClaim("LoginType", "platform")
                .RequireClaim("PlatformRole", "SuperAdmin"));

            // Mirrors "Platform" above: only hospital-issued tokens carry a "UserId" claim
            // (JwtTokenGenerator) — Platform tokens carry "PlatformUserId" instead
            // (PlatformJwtTokenGenerator) — so this is exclusive to hospital tokens the same
            // way "Platform" is exclusive to platform tokens.
            var hospitalPolicy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .RequireClaim("UserId")
                .Build();
            options.AddPolicy("Hospital", hospitalPolicy);

            // Secure by default (HMS Security Hardening Phase A): every controller action
            // requires a valid Hospital token unless it explicitly opts out via
            // [AllowAnonymous] or a different policy (e.g. [Authorize(Policy = "Platform")]).
            // DefaultPolicy governs a bare [Authorize] (e.g. DocumentsController,
            // PatientsController, AuthenticationController.Me); FallbackPolicy governs
            // actions with no authorization attribute at all — previously the majority of
            // hospital-facing controllers (Users, Roles, Permissions, Masters, Products, HR,
            // IPD, Calendar, Branding) had none, meaning they accepted anonymous requests.
            options.DefaultPolicy = hospitalPolicy;
            options.FallbackPolicy = hospitalPolicy;
        });

        // HMS Security Hardening Phase B: backs [RequirePermission("...")] — apply it
        // alongside a bare [Authorize] on any hospital action that needs a specific
        // permission, e.g. RolesController.Create. See PermissionAuthorization.cs.
        services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();

        return services;
    }
}
