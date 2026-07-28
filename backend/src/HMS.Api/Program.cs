using System.Text.Json.Serialization;
using HMS.Api.Configuration;
using HMS.Api.Middleware;
using HMS.Modules.Branding.Infrastructure;
using HMS.Modules.Identity.Infrastructure;
<<<<<<< HEAD
using HMS.Modules.Patients.Infrastructure;
=======
>>>>>>> c2c8cec (refactor(identity): move roles into identity module)
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
        .GetRequiredService<BrandingDbContext>()
        .Database.Migrate();
}

app.Run();

// Exposes the top-level-statements Program class to HMS.IntegrationTests via
// WebApplicationFactory<Program> — the standard pattern for testing minimal-hosting apps.
public partial class Program
{
}