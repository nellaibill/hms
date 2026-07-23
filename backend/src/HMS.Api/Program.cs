using HMS.Api.Configuration;
using HMS.Api.Middleware;
using HMS.Modules.Identity.Infrastructure;
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

builder.Services.AddControllers();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddHmsModules(builder.Configuration);
builder.Services.AddHmsSwagger();

var app = builder.Build();

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseExceptionHandler();
app.UseHmsSwagger();

app.MapControllers();

if (app.Environment.IsDevelopment())
{
    // MVP convenience only — see docs/Deployment.md for the real deployment-time
    // migration step.
    using var scope = app.Services.CreateScope();
    scope.ServiceProvider.GetRequiredService<IdentityDbContext>().Database.Migrate();
}

app.Run();

// Exposes the top-level-statements Program class to HMS.IntegrationTests via
// WebApplicationFactory<Program> — the standard pattern for testing minimal-hosting apps.
public partial class Program
{
}
