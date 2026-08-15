using System.Text.Json.Serialization;
using HMS.Api.Configuration;
using HMS.Api.Middleware;
using HMS.Modules.Branding.Infrastructure;
using HMS.Modules.Calendar.Infrastructure;
using HMS.Modules.Documents.Infrastructure;
using HMS.Modules.HR.Infrastructure;
using HMS.Modules.Identity;
using HMS.Modules.Identity.Infrastructure;
using HMS.Modules.IPD.Infrastructure;
using HMS.Modules.Masters.Infrastructure;
using HMS.Modules.Patients.Infrastructure;
using HMS.Modules.Platform;
using HMS.Modules.Platform.Infrastructure;
using HMS.Modules.Products.Infrastructure;
using HMS.Shared.Infrastructure;
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

var app = builder.Build();

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseExceptionHandler();

// Must run before MapControllers(): CORS has to sit between routing and endpoint
// execution so it can short-circuit browser preflight OPTIONS requests (no controller
// action handles OPTIONS) and attach Access-Control-* headers to every real response
// before it's written. Registered after UseExceptionHandler so a CORS-preflight or
// rejected request still gets a properly-shaped error response, not a raw failure.
app.UseHmsCors();
app.UseHmsSwagger();

// Serves patient photos/ID proofs saved by PatientFileStorage under wwwroot/uploads —
// the app's first static-file surface (see docs/DecisionLog.md's file-upload ADR).
app.UseStaticFiles();

// Must run after CORS and before MapControllers, in that order: Authentication decides
// who the caller is, Authorization then checks [Authorize] on the matched endpoint.
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

if (app.Environment.IsDevelopment())
{
    // MVP convenience only — see docs/Deployment.md for the real deployment-time
    // migration step.
    using var scope = app.Services.CreateScope();

    scope.ServiceProvider
        .GetRequiredService<IdentityDbContext>()
        .Database.Migrate();

    scope.ServiceProvider
        .GetRequiredService<PatientsDbContext>()
        .Database.Migrate();

    scope.ServiceProvider
        .GetRequiredService<DocumentsDbContext>()
        .Database.Migrate();

    scope.ServiceProvider
        .GetRequiredService<BrandingDbContext>()
        .Database.Migrate();

    scope.ServiceProvider
        .GetRequiredService<MastersDbContext>()
        .Database.Migrate();

    scope.ServiceProvider
        .GetRequiredService<ProductsDbContext>()
        .Database.Migrate();

    scope.ServiceProvider
        .GetRequiredService<HRDbContext>()
        .Database.Migrate();

    scope.ServiceProvider
        .GetRequiredService<CalendarDbContext>()
        .Database.Migrate();

    scope.ServiceProvider
        .GetRequiredService<IPDDbContext>()
        .Database.Migrate();

    // Platform owns a separate physical database (hms_platform via
    // ConnectionStrings:Platform), not another schema in hms_qa — see
    // docs/DatabaseArchitecture.md's SaaS provisioning ADR.
    scope.ServiceProvider
        .GetRequiredService<PlatformDbContext>()
        .Database.Migrate();

    // Idempotent: safe to run on every startup. Seeds the Permission catalog's
    // dependents — the "Super Admin" role (every permission attached) and a default
    // Super Admin user — only when they don't already exist. Must run after the
    // Identity migration above, since it reads the Permission rows that migration's
    // HasData just inserted.
    await IdentityModule.SeedAsync(scope.ServiceProvider, CancellationToken.None);

    // Idempotent: seeds the one default Platform Admin account only when it doesn't
    // already exist. Independent of IdentityModule.SeedAsync — separate database,
    // separate account.
    await PlatformModule.SeedAsync(scope.ServiceProvider, CancellationToken.None);
}

app.Run();

// Exposes the top-level-statements Program class to HMS.IntegrationTests via
// WebApplicationFactory<Program> — the standard pattern for testing minimal-hosting apps.
public partial class Program
{
}