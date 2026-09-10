namespace HMS.Api.Configuration;

/// <summary>
/// A small set of response headers with no request-time configuration to speak of — no
/// corresponding <c>AddHmsSecurityHeaders</c> is needed, unlike CorsConfiguration/
/// RateLimitingConfiguration. Registered before <c>UseStaticFiles()</c> in Program.cs so the
/// headers also land on the raster/SVG uploads served from that pipeline stage (Branding
/// logos, product images, patient photos under wwwroot/uploads) — Documents' own uploads are
/// deliberately stored outside wwwroot and never reach UseStaticFiles at all (see
/// DocumentFileStorage's own doc comment), so this is defense-in-depth for the uploads that
/// genuinely do get served as static files, not a substitute for that isolation.
/// </summary>
public static class SecurityHeadersConfiguration
{
    // A pure JSON API plus a handful of served-as-static uploads has no legitimate reason to
    // execute a script or load a subresource from anywhere on this origin, and should never be
    // framed by another site — same reasoning that already applies to Documents' auth-gated
    // serving endpoint, extended here to cover UseStaticFiles' raw file responses too.
    private const string ContentSecurityPolicy = "default-src 'self'; script-src 'none'; object-src 'none'; frame-ancestors 'none'; base-uri 'none'";

    public static WebApplication UseHmsSecurityHeaders(this WebApplication app)
    {
        app.Use(async (context, next) =>
        {
            var headers = context.Response.Headers;
            headers["X-Content-Type-Options"] = "nosniff";
            headers["X-Frame-Options"] = "DENY";

            // Swagger UI (Development only, SwaggerConfiguration.cs) serves its own inline
            // scripts/styles and would break under this CSP — excluded by path rather than by
            // environment, so a stray non-Development Swagger exposure (see
            // docs/DecisionLog.md's deployment-hardening ADR) still gets every other header
            // this middleware sets, just not the CSP.
            if (!context.Request.Path.StartsWithSegments("/swagger"))
            {
                headers["Content-Security-Policy"] = ContentSecurityPolicy;
            }

            await next(context);
        });

        return app;
    }
}
