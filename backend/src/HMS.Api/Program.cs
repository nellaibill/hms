using System.Text.Json.Serialization;
using HMS.Api.Configuration;
using HMS.Api.HealthChecks;
using HMS.Api.Middleware;
using HMS.Modules.Branding.Infrastructure;
using HMS.Modules.Identity;
using HMS.Modules.Platform;
using HMS.Modules.Platform.Application.Abstractions;
using HMS.Modules.Platform.Infrastructure;
using HMS.Shared.Infrastructure;
using HMS.Shared.Kernel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

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
builder.Services.AddHmsRateLimiting();

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

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseExceptionHandler();

// Must run before MapControllers(): CORS has to sit between routing and endpoint
// execution so it can short-circuit browser preflight OPTIONS requests (no controller
// action handles OPTIONS) and attach Access-Control-* headers to every real response
// before it's written. Registered after UseExceptionHandler so a CORS-preflight or
// rejected request still gets a properly-shaped error response, not a raw failure.
app.UseHmsCors();

// Must run before MapControllers(), and deliberately before authentication/authorization
// too — an unauthenticated request flood (e.g. login brute-forcing) should be throttled
// before it spends any JWT-validation or tenant-resolution work, not after.
app.UseHmsRateLimiting();

app.UseHmsSwagger();

// Serves patient photos/ID proofs saved by PatientFileStorage under wwwroot/uploads —
// the app's first static-file surface (see docs/DecisionLog.md's file-upload ADR).
app.UseStaticFiles();

// Must run after CORS and before MapControllers, in that order: Authentication decides
// who the caller is, Authorization then checks [Authorize] on the matched endpoint.
app.UseAuthentication();

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

if (app.Environment.IsDevelopment())
{
    // MVP convenience only — see docs/Deployment.md for the real deployment-time
    // migration step.
    using var scope = app.Services.CreateScope();
    var sp = scope.ServiceProvider;

    // Normally true: the pre-existing legacy dev database (ConnectionStrings:Default)
    // gets migrated and seeded alongside Platform, so a plain `dotnet run` always has a
    // working hospital to log into. Set to false (e.g. cicd/scripts/reset-dev-databases
    // .ps1) for a from-scratch reset where hms_platform should come up with only the
    // Platform Admin account — every hospital, including this one, gets created through
    // the real Register Hospital flow instead of a dev-only shortcut. Read before the
    // Branding migration below since that also targets ConnectionStrings:Default and
    // must be skipped the same way everything else tenant-related is.
    var seedLegacyTenant = builder.Configuration.GetValue("Bootstrap:SeedLegacyTenant", true);

    if (seedLegacyTenant)
    {
        // Branding is the one hospital module NOT made tenant-aware in HMS
        // Multi-Tenancy Phase C (see BrandingModule.cs's own comment) — still migrated
        // via its DI-registered, statically-connected DbContext, same as before this
        // phase. Targets ConnectionStrings:Default, same as everything else gated here.
        sp.GetRequiredService<BrandingDbContext>().Database.Migrate();
    }

    // Platform owns a separate physical database (hms_platform via
    // ConnectionStrings:Platform), not another schema in hms_qa — see
    // docs/DatabaseArchitecture.md's SaaS provisioning ADR. Migrated (and seeded) before
    // anything tenant-aware below, since seeding the legacy tenant row needs it.
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
}

app.Run();

// Exposes the top-level-statements Program class to HMS.IntegrationTests via
// WebApplicationFactory<Program> — the standard pattern for testing minimal-hosting apps.
public partial class Program
{
}