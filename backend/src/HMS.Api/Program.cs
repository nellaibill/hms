using System.Text.Json.Serialization;
using HMS.Api.Configuration;
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
app.UseAuthorization();

// HMS Multi-Tenancy Phase C: must run after authorization (so an unauthenticated/
// wrong-policy request is already rejected by then, never triggering a tenant lookup) and
// before the controller executes (so every tenant-aware hospital DbContext sees a resolved
// ITenantContext the first time it's constructed within this request's scope).
app.UseMiddleware<TenantResolutionMiddleware>();

app.MapControllers();

if (app.Environment.IsDevelopment())
{
    // MVP convenience only — see docs/Deployment.md for the real deployment-time
    // migration step.
    using var scope = app.Services.CreateScope();
    var sp = scope.ServiceProvider;

    // Branding is the one hospital module NOT made tenant-aware in HMS Multi-Tenancy
    // Phase C (see BrandingModule.cs's own comment) — still migrated via its
    // DI-registered, statically-connected DbContext, same as before this phase.
    sp.GetRequiredService<BrandingDbContext>().Database.Migrate();

    // Platform owns a separate physical database (hms_platform via
    // ConnectionStrings:Platform), not another schema in hms_qa — see
    // docs/DatabaseArchitecture.md's SaaS provisioning ADR. Migrated (and seeded) before
    // anything tenant-aware below, since seeding the legacy tenant row needs it.
    sp.GetRequiredService<PlatformDbContext>().Database.Migrate();

    // HMS Multi-Tenancy Phase C: every other hospital module's DbContext is now
    // tenant-aware and can no longer be resolved via DI outside a request with an
    // already-resolved ITenantContext. Migrated directly against ConnectionStrings:Default
    // instead, through the same ITenantMigrationService provisioning and the Platform
    // migrate-tenant endpoint use — ConnectionStrings:Default *is* the legacy tenant's
    // database.
    var defaultConnectionString = builder.Configuration.GetConnectionString("Default")
        ?? throw new InvalidOperationException("Missing 'ConnectionStrings:Default' configuration value.");
    await sp.GetRequiredService<ITenantMigrationService>().MigrateAsync(defaultConnectionString, CancellationToken.None);

    // Idempotent: seeds the one default Platform Admin account, and (Phase C) the
    // platform.tenants row associating ConnectionStrings:Default with a real tenant
    // identity — see LegacyTenantSeedOptions's own doc comment. Must run before resolving
    // that tenant below.
    await PlatformModule.SeedAsync(sp, CancellationToken.None);

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
    // Super Admin user — only when they don't already exist. Must run after the tenant
    // migration above, since it reads the Permission rows that migration's HasData just
    // inserted into the (now-resolved) legacy tenant database.
    await IdentityModule.SeedAsync(sp, CancellationToken.None);
}

app.Run();

// Exposes the top-level-statements Program class to HMS.IntegrationTests via
// WebApplicationFactory<Program> — the standard pattern for testing minimal-hosting apps.
public partial class Program
{
}