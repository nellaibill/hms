using System.Net;
using System.Security.Cryptography;
using System.Text;
using FluentAssertions;
using HMS.Api.Configuration;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace HMS.IntegrationTests.Host;

/// <summary>
/// Boots the real HMS.Api pipeline in Production with a React build in its webapp folder, the way
/// the packaged Windows install runs it (WebAppHostingConfiguration). Needs no database and no
/// Docker: these requests never get past static files, the fallback and routing, and the
/// background jobs that would reach for the database are removed.
/// </summary>
public class WebAppHostFactory : WebApplicationFactory<Program>
{
    /// <summary>An inline script with Windows line endings, as a build made on Windows has.</summary>
    public const string InlineScript = "\r\n      document.documentElement.classList.add('dark');\r\n    ";
    public const string AppMarker = "HMS test app";
    public const string AssetPath = "/assets/index-abc123.js";
    public const string AssetContent = "console.log('hms');";

    private const string NoDatabase = "Host=127.0.0.1;Port=1;Database=none;Username=none;Password=none;Timeout=1";

    public string RootPath { get; } = Path.Combine(Path.GetTempPath(), "hms-webapp-" + Guid.NewGuid().ToString("N"));

    /// <summary>Whether the webapp folder holds a build. Without one the API must behave as before.</summary>
    protected virtual bool HasBuild => true;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        if (HasBuild)
        {
            Directory.CreateDirectory(Path.Combine(RootPath, "assets"));
            File.WriteAllText(
                Path.Combine(RootPath, "index.html"),
                "<!doctype html>\r\n<html>\r\n  <head>\r\n" +
                $"    <script type=\"text/javascript\">{InlineScript}</script>\r\n" +
                $"    <script type=\"module\" crossorigin src=\"{AssetPath}\"></script>\r\n" +
                $"  </head>\r\n  <body><div id=\"root\">{AppMarker}</div></body>\r\n</html>\r\n");
            File.WriteAllText(Path.Combine(RootPath, "assets", "index-abc123.js"), AssetContent);
        }

        builder.UseEnvironment("Production");
        builder.UseSetting("ConnectionStrings:Default", NoDatabase);
        builder.UseSetting("ConnectionStrings:Platform", NoDatabase);
        builder.UseSetting("ConnectionStrings:PlatformAdmin", NoDatabase);
        builder.UseSetting("Jwt:SigningKey", "web-app-hosting-tests-signing-key-" + new string('k', 64));
        builder.UseSetting("WebApp:RootPath", RootPath);

        builder.ConfigureTestServices(services =>
        {
            var backgroundJobs = services
                .Where(descriptor => descriptor.ServiceType == typeof(IHostedService)
                    && descriptor.ImplementationType?.Namespace?.StartsWith("HMS.", StringComparison.Ordinal) == true)
                .ToList();
            foreach (var job in backgroundJobs)
            {
                services.Remove(job);
            }
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing && Directory.Exists(RootPath))
        {
            Directory.Delete(RootPath, recursive: true);
        }
    }
}

/// <summary>The same host with an empty webapp folder: dev, nginx and Docker deployments.</summary>
public sealed class WebAppHostWithoutBuildFactory : WebAppHostFactory
{
    protected override bool HasBuild => false;
}

public class WebAppHostingTests : IClassFixture<WebAppHostFactory>
{
    private const string StrictApiPolicyMarker = "script-src 'none'";

    private readonly HttpClient _client;

    public WebAppHostingTests(WebAppHostFactory factory)
    {
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
    }

    private static string HashOf(string script) =>
        $"'sha256-{Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(script.Replace("\r\n", "\n"))))}'";

    [Fact]
    public async Task Root_serves_index_html_with_the_app_policy()
    {
        var response = await _client.GetAsync("/");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be("text/html");
        (await response.Content.ReadAsStringAsync()).Should().Contain(WebAppHostFactory.AppMarker);
        response.Headers.CacheControl!.NoCache.Should().BeTrue();
        response.Headers.GetValues("X-Frame-Options").Should().Equal("SAMEORIGIN");

        var policy = response.Headers.GetValues("Content-Security-Policy").Single();
        policy.Should().Contain($"script-src 'self' {HashOf(WebAppHostFactory.InlineScript)};");
        policy.Should().NotContain(StrictApiPolicyMarker);
    }

    [Theory]
    [InlineData("/patients/123/edit")]
    [InlineData("/login")]
    public async Task App_routes_serve_index_html_so_deep_links_and_refreshes_work(string path)
    {
        var response = await _client.GetAsync(path);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync()).Should().Contain(WebAppHostFactory.AppMarker);
        response.Headers.CacheControl!.NoCache.Should().BeTrue();
    }

    [Fact]
    public async Task Hashed_assets_are_cached_for_good()
    {
        var response = await _client.GetAsync(WebAppHostFactory.AssetPath);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync()).Should().Be(WebAppHostFactory.AssetContent);
        response.Headers.CacheControl!.ToString().Should().Be("public, max-age=31536000, immutable");
    }

    [Theory]
    [InlineData("/api/does-not-exist")]
    [InlineData("/uploads/missing.png")]
    [InlineData("/assets/missing.js")]
    public async Task Missing_api_paths_and_files_never_get_index_html(string path)
    {
        var response = await _client.GetAsync(path);

        response.StatusCode.Should().NotBe(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync()).Should().NotContain(WebAppHostFactory.AppMarker);
    }

    [Theory]
    [InlineData("/api/does-not-exist")]
    [InlineData("/uploads/missing.png")]
    public async Task Api_and_upload_paths_keep_the_strict_policy(string path)
    {
        var response = await _client.GetAsync(path);

        response.Headers.GetValues("Content-Security-Policy").Single().Should().Contain(StrictApiPolicyMarker);
        response.Headers.GetValues("X-Frame-Options").Should().Equal("DENY");
    }

    [Fact]
    public void Policy_hashes_only_inline_scripts_with_line_endings_normalized()
    {
        const string html = "<head><script>\r\nfirst();\r\n</script><script type=\"module\" src=\"/assets/a.js\"></script><SCRIPT>second();</SCRIPT></head>";

        var policy = WebAppHostingConfiguration.BuildContentSecurityPolicy(html);

        policy.Should().Contain($"script-src 'self' {HashOf("\nfirst();\n")} {HashOf("second();")};");
    }
}

public class WebAppHostingWithoutBuildTests : IClassFixture<WebAppHostWithoutBuildFactory>
{
    private readonly HttpClient _client;

    public WebAppHostingWithoutBuildTests(WebAppHostWithoutBuildFactory factory)
    {
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
    }

    [Theory]
    [InlineData("/")]
    [InlineData("/patients/123/edit")]
    public async Task Without_a_build_app_routes_get_no_app_and_the_strict_policy_as_before(string path)
    {
        var response = await _client.GetAsync(path);

        response.StatusCode.Should().NotBe(HttpStatusCode.OK);
        response.Headers.GetValues("Content-Security-Policy").Single().Should().Contain("script-src 'none'");
        response.Headers.GetValues("X-Frame-Options").Should().Equal("DENY");
    }
}

/// <summary>Serving the app must not change any response that belongs to the API.</summary>
public class WebAppHostingApiUnchangedTests : IClassFixture<WebAppHostFactory>, IClassFixture<WebAppHostWithoutBuildFactory>
{
    private readonly HttpClient _withBuild;
    private readonly HttpClient _withoutBuild;

    public WebAppHostingApiUnchangedTests(WebAppHostFactory withBuild, WebAppHostWithoutBuildFactory withoutBuild)
    {
        var options = new WebApplicationFactoryClientOptions { AllowAutoRedirect = false };
        _withBuild = withBuild.CreateClient(options);
        _withoutBuild = withoutBuild.CreateClient(options);
    }

    [Theory]
    [InlineData("GET", "/api/does-not-exist")]
    [InlineData("GET", "/api/platform/auth/login")] // a POST-only route
    [InlineData("DELETE", "/api/platform/auth/login")]
    [InlineData("GET", "/uploads/missing.png")]
    [InlineData("POST", "/patients/123")] // an app route, but not a GET
    public async Task Api_responses_are_the_same_with_and_without_a_build(string method, string path)
    {
        var withBuild = await _withBuild.SendAsync(new HttpRequestMessage(new HttpMethod(method), path));
        var withoutBuild = await _withoutBuild.SendAsync(new HttpRequestMessage(new HttpMethod(method), path));

        withBuild.StatusCode.Should().Be(withoutBuild.StatusCode);
        (await withBuild.Content.ReadAsStringAsync()).Should().NotContain(WebAppHostFactory.AppMarker);
    }
}
