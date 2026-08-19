using HMS.Shared.Infrastructure;
using Microsoft.AspNetCore.Http;

namespace HMS.Api.Configuration;

/// <summary>
/// CORS for the whole host, via a single named policy read from configuration
/// (<c>Cors:AllowedOrigins</c> in appsettings). Registered here (not ModuleRegistration)
/// since it's host-level pipeline configuration, not a business module. This is the
/// reference implementation every future module's browser-facing endpoints should rely on
/// — modules must not register their own CORS policies.
/// </summary>
public static class CorsConfiguration
{
    public const string PolicyName = "HmsCorsPolicy";

    public static IServiceCollection AddHmsCors(this IServiceCollection services, IConfiguration configuration)
    {
        var allowedOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];

        services.AddCors(options =>
        {
            options.AddPolicy(PolicyName, policy =>
            {
                // No origins configured (e.g. no Cors:AllowedOrigins section in this
                // environment) means the policy allows nothing — fails closed rather than
                // falling back to AllowAnyOrigin.
                policy.WithOrigins(allowedOrigins)
                    .WithMethods(
                        HttpMethods.Get,
                        HttpMethods.Post,
                        HttpMethods.Put,
                        HttpMethods.Patch,
                        HttpMethods.Delete,
                        HttpMethods.Options)
                    // Idempotency-Key (HospitalsController.Create) was added after this
                    // allowlist and never added here — the browser's CORS preflight silently
                    // blocked the actual POST from ever reaching the server, since curl-based
                    // verification (which doesn't enforce CORS) never would have caught it.
                    .WithHeaders("Content-Type", "Authorization", "Accept", CorrelationIdMiddleware.HeaderName, "X-Hospital-Code", "Idempotency-Key")
                    .WithExposedHeaders(CorrelationIdMiddleware.HeaderName);

                // Deliberately no AllowCredentials(): HttpClient.ts (frontend/shared)
                // authenticates via an Authorization: Bearer header, not cookies, and never
                // sets fetch's credentials: "include". Browsers reject AllowAnyOrigin()
                // combined with AllowCredentials() outright, and there's no cookie-based
                // session here for credentials to apply to. If cookie/session auth ships
                // later, this needs AllowCredentials() plus keeping WithOrigins() to an
                // explicit allowlist — never AllowAnyOrigin() at that point.
            });
        });

        return services;
    }

    public static WebApplication UseHmsCors(this WebApplication app)
    {
        app.UseCors(PolicyName);
        return app;
    }
}
