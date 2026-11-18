using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.FileProviders;

namespace HMS.Api.Configuration;

/// <summary>
/// Serves the built React app (frontend/web's <c>npm run build</c> output, built with
/// <c>VITE_API_BASE_URL=same-origin</c>) from this API, for the packaged Windows install: one HTTPS port,
/// no nginx, no CORS, and no hospital IP baked into the bundle.
///
/// Only on when a build has been copied into the folder <c>WebApp:RootPath</c> names (default
/// <c>webapp</c> under the content root). <c>dotnet run</c> (Vite serves the app) and the
/// nginx/Docker deployments (nginx serves frontend/web/dist) have no such folder, so they behave
/// exactly as before.
/// </summary>
public static partial class WebAppHostingConfiguration
{
    private const string DefaultRootPath = "webapp";

    // Vite puts a content hash in every file name under /assets, so a new build always brings new
    // names and these can be cached for good. Everything else (index.html) has to be re-checked on
    // every load, or browsers keep running the old app after an upgrade.
    private const string ImmutableCacheControl = "public, max-age=31536000, immutable";
    private const string RevalidateCacheControl = "no-cache";

    // First path segments that belong to the API itself. They are never answered with the app's
    // index.html, and they keep the strict API Content-Security-Policy (SecurityHeadersConfiguration).
    private static readonly string[] ApiPathSegments = ["api", "uploads", "health", "swagger", "hangfire"];

    private static readonly PathString[] ApiPaths = ApiPathSegments.Select(segment => new PathString("/" + segment)).ToArray();

    // Every path without a file extension, except the API's. Leaving the API's paths out of the
    // route itself (rather than checking them in the handler) keeps their responses exactly as
    // before: a GET on a POST-only API route still gets 405, an unknown one still gets 404.
    private static readonly string FallbackPattern =
        $"{{*path:nonfile:regex(^(?!({string.Join('|', ApiPathSegments)})(/|$)))}}";

    /// <summary>Whether <paramref name="path"/> belongs to the API rather than to the React app.</summary>
    public static bool IsApiPath(PathString path) => ApiPaths.Any(apiPath => path.StartsWithSegments(apiPath));

    /// <summary>
    /// Finds the app build, or returns null when there is none, in which case nothing in this
    /// class changes how the API behaves.
    /// </summary>
    public static WebAppFiles? Find(IWebHostEnvironment environment, IConfiguration configuration)
    {
        var configuredRoot = configuration["WebApp:RootPath"];
        var rootPath = Path.GetFullPath(Path.Combine(
            environment.ContentRootPath,
            string.IsNullOrWhiteSpace(configuredRoot) ? DefaultRootPath : configuredRoot));
        var indexFilePath = Path.Combine(rootPath, "index.html");

        return File.Exists(indexFilePath)
            ? new WebAppFiles(rootPath, indexFilePath, BuildContentSecurityPolicy(File.ReadAllText(indexFilePath)))
            : null;
    }

    /// <summary>
    /// The policy for the app's own pages. The API's policy (<c>script-src 'none'</c>) would stop
    /// the app from running at all. Scripts may only come from this server, plus the inline
    /// scripts in this exact index.html, allowed by their SHA-256 hashes. Styles may be inline,
    /// because React and Radix set style attributes. <c>blob:</c> and <c>data:</c> cover photo
    /// previews, camera capture, and documents and PDFs opened in a frame or a new tab.
    /// </summary>
    public static string BuildContentSecurityPolicy(string indexHtml)
    {
        var scriptSources = new List<string> { "'self'" };
        foreach (Match script in InlineScriptRegex().Matches(indexHtml))
        {
            if (SrcAttributeRegex().IsMatch(script.Groups["attributes"].Value))
            {
                continue;
            }

            // Browsers hash a script's text after the HTML parser has turned every CRLF and CR into
            // LF, so a build with Windows line endings has to be hashed the same way.
            var text = script.Groups["body"].Value.Replace("\r\n", "\n").Replace('\r', '\n');
            scriptSources.Add($"'sha256-{Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(text)))}'");
        }

        return string.Join("; ",
            "default-src 'self'",
            $"script-src {string.Join(' ', scriptSources.Distinct())}",
            "style-src 'self' 'unsafe-inline'",
            "img-src 'self' data: blob:",
            "font-src 'self' data:",
            "connect-src 'self' blob: data:",
            "media-src 'self' blob:",
            "frame-src 'self' blob:",
            "worker-src 'self' blob:",
            "object-src 'self' blob:",
            "base-uri 'self'",
            "form-action 'self'",
            "frame-ancestors 'self'");
    }

    /// <summary>
    /// Serves the app's files (index.html at <c>/</c>, /assets/…). Registered early in Program.cs,
    /// before authentication and rate limiting, the same way nginx serves them without touching the
    /// API: the build has hundreds of small files, and they would otherwise count against each
    /// desk's anonymous request limit.
    /// </summary>
    public static WebApplication UseHmsWebAppFiles(this WebApplication app, WebAppFiles? webApp)
    {
        if (webApp is null)
        {
            return app;
        }

        var fileProvider = new PhysicalFileProvider(webApp.RootPath);
        var defaultFiles = new DefaultFilesOptions { FileProvider = fileProvider };
        defaultFiles.DefaultFileNames.Clear();
        defaultFiles.DefaultFileNames.Add("index.html");

        var staticFiles = new StaticFileOptions
        {
            FileProvider = fileProvider,
            OnPrepareResponse = context =>
                context.Context.Response.Headers.CacheControl = context.Context.Request.Path.StartsWithSegments("/assets")
                    ? ImmutableCacheControl
                    : RevalidateCacheControl,
        };

        app.UseWhen(
            context => !IsApiPath(context.Request.Path),
            branch => branch.UseDefaultFiles(defaultFiles).UseStaticFiles(staticFiles));
        return app;
    }

    /// <summary>
    /// Answers every other GET for an app route (a deep link or a refresh on /patients/…) with
    /// index.html, so React Router can take over. A path with a file extension, or one under
    /// <see cref="ApiPathSegments"/>, is never answered this way: when nothing matches it gets the
    /// same response it always has. <c>/</c> itself is served by <see cref="UseHmsWebAppFiles"/>,
    /// since a regex route constraint never matches an empty catch-all.
    /// </summary>
    public static WebApplication MapHmsWebAppFallback(this WebApplication app, WebAppFiles? webApp)
    {
        if (webApp is null)
        {
            return app;
        }

        app.MapFallback(FallbackPattern, async context =>
            {
                context.Response.ContentType = "text/html; charset=utf-8";
                context.Response.Headers.CacheControl = RevalidateCacheControl;
                await context.Response.SendFileAsync(webApp.IndexFilePath);
            })
            .WithMetadata(new HttpMethodMetadata(["GET", "HEAD"]))
            .AllowAnonymous();

        return app;
    }

    [GeneratedRegex(@"<script(?<attributes>[^>]*)>(?<body>.*?)</script\s*>", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex InlineScriptRegex();

    [GeneratedRegex(@"\bsrc\s*=", RegexOptions.IgnoreCase)]
    private static partial Regex SrcAttributeRegex();
}
