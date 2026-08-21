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

        // Platform Admin MFA (RFC 6238 TOTP) — see ITotpService/IPlatformMfaSecretProtector/
        // IPlatformMfaChallengeStore's own doc comments.
        services.AddSingleton<ITotpService, TotpService>();
        services.AddSingleton<IPlatformMfaSecretProtector, PlatformMfaSecretProtector>();
        services.AddScoped<IPlatformMfaChallengeStore, PlatformMfaChallengeStore>();

        services.AddScoped<ITenantRepository, TenantRepository>();
        services.AddScoped<ITenantFeatureRepository, TenantFeatureRepository>();
        services.AddScoped<IHospitalRegistrationIdempotencyStore, HospitalRegistrationIdempotencyStore>();
        services.AddScoped<IHospitalRegistrationService, HospitalRegistrationService>();
        services.AddScoped<IPlatformDashboardService, PlatformDashboardService>();
        services.AddScoped<ITenantFeatureService, TenantFeatureService>();

        // Consumed by HMS.Api's TenantProvisioningService (ITenantProvisioner) to raise a
        // durable, dashboard-visible alert when a rollback fails — see
        // IProvisioningAlertStore's own doc comment for why this is public.
        services.AddScoped<IProvisioningAlertStore, ProvisioningAlertStore>();

        // Consumed by HMS.Api's JwtConfiguration (checked on every platform-token
        // validation) and by PlatformAuthController.Logout (revokes the caller's own
        // token) — see IRevokedTokenStore's own doc comment for why this is public.
        services.AddScoped<IRevokedTokenStore, RevokedTokenStore>();

        // HMS Multi-Tenancy Phase C's tenant-resolution seam — consumed by
        // HMS.Api's TenantResolutionMiddleware and by HMS.Modules.Identity's
        // AuthenticationService (login-time resolution). See ITenantDirectory's own doc
        // comment for why this one lives here rather than being implemented in HMS.Api.
        services.AddScoped<ITenantDirectory, TenantDirectory>();

        // Registered explicitly rather than via AddValidatorsFromAssemblyContaining: that
        // scanner only finds *public* IValidator<T> implementations — see HRModule.cs's
        // identical comment.
        services.AddScoped<IValidator<PlatformLoginRequest>, PlatformLoginRequestValidator>();
        services.AddScoped<IValidator<CreateHospitalRequest>, CreateHospitalRequestValidator>();
        services.AddScoped<IValidator<PlatformMfaVerifyRequest>, PlatformMfaVerifyRequestValidator>();
        services.AddScoped<IValidator<PlatformMfaEnableRequest>, PlatformMfaEnableRequestValidator>();
        services.AddScoped<IValidator<PlatformMfaDisableRequest>, PlatformMfaDisableRequestValidator>();
        services.AddScoped<IValidator<UpdateTenantConfigurationRequest>, UpdateTenantConfigurationRequestValidator>();
        services.AddScoped<IValidator<UpdateTenantFeaturesRequest>, UpdateTenantFeaturesRequestValidator>();

        services.Configure<PlatformAdminSeedOptions>(configuration.GetSection(PlatformAdminSeedOptions.SectionName));
        services.Configure<LegacyTenantSeedOptions>(configuration.GetSection(LegacyTenantSeedOptions.SectionName));
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
    /// <param name="seedLegacyTenant">
    /// Whether to also seed the platform.tenants row for the pre-existing legacy dev
    /// database (see LegacyTenantSeedOptions) — true everywhere except a from-scratch
    /// "Platform DB only" reset (Bootstrap:SeedLegacyTenant=false), where nothing should
    /// exist yet for every hospital to be registered through the real flow instead.
    /// </param>
    public static Task SeedAsync(IServiceProvider services, CancellationToken cancellationToken, bool seedLegacyTenant = true)
    {
        return services.GetRequiredService<PlatformDataSeeder>().SeedAsync(cancellationToken, seedLegacyTenant);
    }
}
