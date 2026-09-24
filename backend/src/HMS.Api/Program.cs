using System.Text.Json.Serialization;
using HMS.Api.Configuration;
using HMS.Api.HealthChecks;
using HMS.Api.Middleware;
using HMS.Api.Provisioning;
using HMS.Modules.Identity;
using HMS.Modules.Platform;
using HMS.Modules.Platform.Application.Abstractions;
using HMS.Modules.Platform.Infrastructure;
using HMS.Shared.Infrastructure;
using HMS.Shared.Kernel;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

// Explicit deploy-time migration step (docs/DatabaseArchitecture.md's Migration
// Strategy: "pending migrations are applied as an explicit, logged step in the
// deployment pipeline before the new application version begins serving traffic").
// `dotnet HMS.Api.dll migrate` runs the same migrate+seed block below that Development
// already runs automatically, then exits without starting Kestrel — the counterpart to
// the "MVP convenience only" auto-migrate further down, usable in any environment
// including Production. A bare positional token like this is ignored by the command-line
// configuration provider CreateBuilder(args) below wires up, so it's safe to pass through.
var isMigrationOnlyRun = args.Contains("migrate", StringComparer.OrdinalIgnoreCase);

// One-time fixup for the per-tenant file storage change (see TenantFileStorageMigrator's own
// doc comment) — deliberately its own explicit command, never folded into the `migrate` step
// or Development's auto-migrate, since unlike a schema migration this moves real files and
// rewrites real rows. `--dry-run` reports what it would do without touching anything.
var isTenantFileMigrationRun = args.Contains("migrate-tenant-files", StringComparer.OrdinalIgnoreCase);
var isTenantFileMigrationDryRun = isTenantFileMigrationRun && args.Contains("--dry-run", StringComparer.OrdinalIgnoreCase);

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<TenantFileStorageMigrator>();

// FluentValidation is invoked explicitly by controllers (see UsersController), so the
// framework's own ModelState-based 400s are suppressed to keep one consistent error shape.
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.SuppressModelStateInvalidFilter = true;
});



builder.Services.AddControllers()
    // Patients is the first module with enum fields (Title, Gender, EncounterType, ...) —
    // serialize them as their string names, not the default integer ordinal, so the JSON
    // contract is self-describing for Swagger and every frontend consumer.
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddHmsModules(builder.Configuration);
builder.Services.AddHmsSwagger();
builder.Services.AddHmsCors(builder.Configuration);
builder.Services.AddHmsJwtAuthentication(builder.Configuration);
builder.Services.AddHmsRateLimiting(builder.Configuration);

// Backs the Docker Compose healthcheck (and any future orchestrator) — see
// DatabaseHealthCheck's own doc comment for why it targets PlatformDbContext.
builder.Services.AddHealthChecks().AddCheck<DatabaseHealthCheck>("postgres");

// Backs PlatformMfaSecretProtector (encrypts Platform Admin TOTP secrets at rest) — the
// default file-system key ring under the machine's user profile is fine for this app's
// current single-instance deployment; a distributed key ring (Redis/blob storage) would
// only be needed once this runs as more than one instance, same "no infra beyond what's
// already available" posture as ADR-021/ADR-023's other deferrals.
builder.Services.AddDataProtection();

var app = builder.Build();

// Must be the very first middleware: everything downstream (rate limiting's ClientKey,
// request logging, audit columns) reads HttpContext.Connection.RemoteIpAddress, and this is
// what rewrites it from X-Forwarded-For when the request actually came through a trusted
// local reverse proxy (ADR-076). KnownNetworks/KnownProxies are deliberately left at their
// framework default (loopback only, never widened here) — only a request whose *immediate*
// connection is 127.0.0.1/::1 (i.e., already relayed by nginx running on this same host) gets
// its header trusted; a request claiming to be from a trusted proxy over the network can't
// spoof this. Before this, the Windows/nginx reverse-proxy deployment path collapsed every
// real client's IP into nginx's own loopback address at this property, silently defeating
// per-client rate-limit partitioning (RateLimitingConfiguration.ClientKey) — see ADR-076 for
// the full before/after analysis. No effect on the Docker Compose deployment (browser talks
// directly to the API container, no proxy hop, so no X-Forwarded-For header is ever present).
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
});

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseExceptionHandler();

// Must run before MapControllers(): CORS has to sit between routing and endpoint
// execution so it can short-circuit browser preflight OPTIONS requests (no controller
// action handles OPTIONS) and attach Access-Control-* headers to every real response
// before it's written. Registered after UseExceptionHandler so a CORS-preflight or
// rejected request still gets a properly-shaped error response, not a raw failure.
app.UseHmsCors();

// Authentication runs before rate limiting (ADR-084, revising ADR-018's ordering) so the
// global limiter can partition by the verified user instead of only by client IP — a whole
// hospital behind one NAT'd IP otherwise shares a single budget. JWT validation is a cheap
// signature check (plus a revoked-token lookup, but only for validly signed Platform
// tokens); tenant resolution (a DB lookup) still runs after the limiter, and a
// per-IP ceiling plus the per-IP Login policy keep unauthenticated floods throttled.
// Authorization stays after TenantResolutionMiddleware below.
app.UseAuthentication();

// Must run before MapControllers() and before tenant resolution, so a flood is rejected
// before it spends any per-request database work.
app.UseHmsRateLimiting();

app.UseHmsSwagger();

// Before UseStaticFiles() so the raster/SVG uploads served from wwwroot (Branding logos,
// product images, patient photos) also get nosniff/X-Frame-Options/CSP, not just the JSON API
// responses below.
app.UseHmsSecurityHeaders();

// Serves patient photos/ID proofs saved by PatientFileStorage under wwwroot/uploads —
// the app's first static-file surface (see docs/DecisionLog.md's file-upload ADR).
app.UseStaticFiles();

// HMS Multi-Tenancy Phase C: must run after authentication (so it has a JWT's "UserId"/
// "TenantId" claims to key off) but BEFORE authorization — Tenant Feature/Module
// Management's FeatureAuthorizationHandler reads ITenantContext.EnabledFeatures (live,
// re-resolved from platform.tenant_features on every request, deliberately never a JWT
// claim — see FeatureAuthorizationHandler's own doc comment), so ITenantContext must
// already be populated by the time UseAuthorization() evaluates policies, not after. Also
// still before MapControllers(), so every tenant-aware hospital DbContext sees a resolved
// ITenantContext the first time it's constructed within this request's scope. Trade-off:
// an authenticated request now always resolves its tenant even if authorization will go on
// to reject it for an unrelated reason (e.g. a missing permission) — an acceptable single
// extra lookup against the same seam every request already needed anyway.
app.UseMiddleware<TenantResolutionMiddleware>();

app.UseAuthorization();

app.MapControllers();

// AllowAnonymous is required here: JwtConfiguration's FallbackPolicy requires a Hospital
// token on any endpoint without an explicit authorization attribute, and the container
// healthcheck has no token to present.
app.MapHealthChecks("/health").AllowAnonymous();

if (app.Environment.IsDevelopment() || isMigrationOnlyRun)
{
    // Development still runs this automatically on a plain `dotnet run` — MVP
    // convenience, unchanged. `isMigrationOnlyRun` is the real deployment-time path (see
    // docs/Deployment.md): same migrate+seed logic, explicitly invoked, in any environment.
    var migrationLogger = app.Services.GetRequiredService<ILogger<Program>>();
    migrationLogger.LogInformation(
        "Starting migration step (environment: {Environment}, explicit: {Explicit})",
        app.Environment.EnvironmentName,
        isMigrationOnlyRun);

    using var scope = app.Services.CreateScope();
    var sp = scope.ServiceProvider;

    // Normally true: the pre-existing legacy dev database (ConnectionStrings:Default)
    // gets seeded alongside Platform, so a plain `dotnet run` always has a working
    // hospital to log into. Set to false (e.g. cicd/scripts/reset-dev-databases.ps1) for
    // a from-scratch reset where hms_platform should come up with only the Platform
    // Admin account — every hospital, including this one, gets created through the real
    // Register Hospital flow instead of a dev-only shortcut.
    var seedLegacyTenant = builder.Configuration.GetValue("Bootstrap:SeedLegacyTenant", true);

    // Platform's own tables live under the "platform" schema, isolated from (when
    // SeedLegacyTenant is true) the legacy tenant's own per-module schemas below — see
    // docs/DatabaseArchitecture.md's SaaS provisioning ADR. Local dev/the Windows installer
    // point ConnectionStrings:Default at the same physical database as
    // ConnectionStrings:Platform (hms_platform); a real production deployment still keeps
    // Default pointed at its own database when SeedLegacyTenant is true there (see
    // docs/Deployment.md). Migrated (and seeded) before anything tenant-aware below, since
    // seeding the legacy tenant row needs it.
    sp.GetRequiredService<PlatformDbContext>().Database.Migrate();

    // Idempotent: seeds the one default Platform Admin account, and — only when
    // seedLegacyTenant is true — the platform.tenants row associating
    // ConnectionStrings:Default with a real tenant identity (see LegacyTenantSeedOptions's
    // own doc comment). Must run before resolving that tenant below.
    await PlatformModule.SeedAsync(sp, CancellationToken.None, seedLegacyTenant);

    if (seedLegacyTenant)
    {
        // HMS Multi-Tenancy Phase C: every other hospital module's DbContext is now
        // tenant-aware and can no longer be resolved via DI outside a request with an
        // already-resolved ITenantContext. Migrated directly against
        // ConnectionStrings:Default instead, through the same ITenantMigrationService
        // provisioning and the Platform migrate-tenant endpoint use —
        // ConnectionStrings:Default *is* the legacy tenant's database.
        var defaultConnectionString = builder.Configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("Missing 'ConnectionStrings:Default' configuration value.");
        // Full feature set — preserves this legacy tenant's existing behavior exactly (it
        // has always had every module) under the new selective-migration seam.
        await sp.GetRequiredService<ITenantMigrationService>().MigrateAsync(defaultConnectionString, FeatureCatalog.All, CancellationToken.None);

        // Resolves the just-seeded legacy tenant and populates this startup scope's
        // ITenantContext, so IdentityDbContext — tenant-aware like every other hospital
        // module — connects to the right database when IdentityModule.SeedAsync below
        // resolves it via DI.
        var legacyHospitalCode = builder.Configuration["LegacyTenantSeed:HospitalCode"] ?? "legacy";
        var legacyTenant = await sp.GetRequiredService<ITenantDirectory>().FindByHospitalCodeAsync(legacyHospitalCode, CancellationToken.None)
            ?? throw new InvalidOperationException($"Legacy tenant '{legacyHospitalCode}' was not found after seeding.");
        sp.GetRequiredService<ITenantContext>().SetTenant(legacyTenant.Id, legacyTenant.ConnectionString);

        // Idempotent: safe to run on every startup. Seeds the Permission catalog's
        // dependents — the "Super Admin" role (every permission attached) and a default
        // Super Admin user — only when they don't already exist. Must run after the
        // tenant migration above, since it reads the Permission rows that migration's
        // HasData just inserted into the (now-resolved) legacy tenant database.
        await IdentityModule.SeedAsync(sp, CancellationToken.None);
    }

    migrationLogger.LogInformation("Migration step complete.");
}

if (isMigrationOnlyRun)
{
    // The deploy pipeline's migration step ends here — it never starts serving traffic.
    return;
}

if (isTenantFileMigrationRun)
{
    var tenantFileMigrationLogger = app.Services.GetRequiredService<ILogger<Program>>();
    using var scope = app.Services.CreateScope();
    var tenants = await scope.ServiceProvider.GetRequiredService<ITenantDirectory>().GetAllActiveTenantsAsync(CancellationToken.None);

    tenantFileMigrationLogger.LogInformation(
        "Starting tenant file storage migration for {Count} active tenant(s){DryRunSuffix}.",
        tenants.Count,
        isTenantFileMigrationDryRun ? " (DRY RUN — nothing will be changed)" : string.Empty);

    await app.Services.GetRequiredService<TenantFileStorageMigrator>().RunAsync(tenants, isTenantFileMigrationDryRun, CancellationToken.None);

    tenantFileMigrationLogger.LogInformation("Tenant file storage migration {Verb}.", isTenantFileMigrationDryRun ? "dry run complete — re-run without --dry-run to apply" : "complete");
    return;
}

app.Run();

// Exposes the top-level-statements Program class to HMS.IntegrationTests via
// WebApplicationFactory<Program> — the standard pattern for testing minimal-hosting apps.
public partial class Program
{
}