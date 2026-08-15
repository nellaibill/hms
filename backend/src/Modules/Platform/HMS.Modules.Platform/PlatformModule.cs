using FluentValidation;
using HMS.Modules.Platform.Application;
using HMS.Modules.Platform.Application.Abstractions;
using HMS.Modules.Platform.Application.Validators;
using HMS.Modules.Platform.Contracts;
using HMS.Modules.Platform.Infrastructure;
using HMS.Modules.Platform.Infrastructure.Repositories;
using HMS.Modules.Platform.Infrastructure.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HMS.Modules.Platform;

/// <summary>
/// Single composition entry point for this module, called once from HMS.Api/Configuration —
/// mirrors HMS.Modules.HR.HRModule. Points at ConnectionStrings:Platform (a separate physical
/// database, hms_platform) rather than the ConnectionStrings:Default every other module
/// shares — see docs/DatabaseArchitecture.md's SaaS provisioning ADR.
/// </summary>
public static class PlatformModule
{
    public static IServiceCollection AddPlatformModule(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Platform")
            ?? throw new InvalidOperationException("Missing 'ConnectionStrings:Platform' configuration value.");

        services.AddDbContext<PlatformDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql =>
            {
                npgsql.MigrationsHistoryTable("__ef_migrations_history", PlatformDbContext.SchemaName);

                // Migration classes live in HMS.Database.Migrations (per
                // docs/DatabaseArchitecture.md), not in this module's own assembly.
                npgsql.MigrationsAssembly("HMS.Database.Migrations");
            }));

        services.AddScoped<IPlatformUserRepository, PlatformUserRepository>();
        services.AddSingleton<IPlatformPasswordHasher, PlatformPasswordHasher>();
        services.AddSingleton<IPlatformJwtTokenGenerator, PlatformJwtTokenGenerator>();
        services.AddScoped<IPlatformAuthenticationService, PlatformAuthenticationService>();

        services.AddScoped<ITenantRepository, TenantRepository>();
        services.AddScoped<IHospitalRegistrationService, HospitalRegistrationService>();
        services.AddScoped<IPlatformDashboardService, PlatformDashboardService>();

        // Registered explicitly rather than via AddValidatorsFromAssemblyContaining: that
        // scanner only finds *public* IValidator<T> implementations — see HRModule.cs's
        // identical comment.
        services.AddScoped<IValidator<PlatformLoginRequest>, PlatformLoginRequestValidator>();
        services.AddScoped<IValidator<CreateHospitalRequest>, CreateHospitalRequestValidator>();

        services.Configure<PlatformAdminSeedOptions>(configuration.GetSection(PlatformAdminSeedOptions.SectionName));
        services.AddScoped<PlatformDataSeeder>();

        // ITenantProvisioner itself is NOT registered here — it's implemented in HMS.Api
        // (see ITenantProvisioner's own doc comment) and wired up in
        // HMS.Api/Configuration/ModuleRegistration.cs, after this method runs.

        return services;
    }

    /// <summary>
    /// Startup data seeding entry point, called once from Program.cs after
    /// PlatformDbContext.Database.Migrate() — the same "single public seam per module"
    /// shape as HMS.Modules.Identity.IdentityModule.SeedAsync.
    /// </summary>
    public static Task SeedAsync(IServiceProvider services, CancellationToken cancellationToken)
    {
        return services.GetRequiredService<PlatformDataSeeder>().SeedAsync(cancellationToken);
    }
}
