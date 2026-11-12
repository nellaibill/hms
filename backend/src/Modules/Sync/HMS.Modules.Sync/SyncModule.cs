using Hangfire;
using Hangfire.PostgreSql;
using HMS.Modules.Sync.Application;
using HMS.Modules.Sync.Contracts;
using HMS.Modules.Sync.Infrastructure;
using HMS.Modules.Sync.Infrastructure.Jobs;
using HMS.Shared.Kernel;
using HMS.Shared.Kernel.Sync;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HMS.Modules.Sync;

/// <summary>
/// Single composition entry point for the offline-first sync module (ADR-133), called once from
/// HMS.Api/Configuration/ModuleRegistration.cs. What gets registered depends on <c>Sync:Role</c>:
/// <list type="bullet">
/// <item><b>None</b> (default): DbContexts (so the schema is migrated), options and the monitoring
/// API, which then reports "not enabled". Nothing runs and nothing is captured.</item>
/// <item><b>Edge</b>: plus the outbox writer (which switches on patient capture in
/// PatientsModule), the cloud HTTP client, Hangfire (storage in hms_platform, schema
/// <c>hangfire</c>), its server, the recurring jobs and the cookie-gated dashboard.</item>
/// <item><b>Cloud</b>: plus the patient write guard for hospitals cut over to their local
/// server. The <c>HmsSync</c> key scheme is registered on every role but refuses everything
/// unless the role is Cloud.</item>
/// </list>
/// </summary>
public static class SyncModule
{
    /// <summary>
    /// True when this server takes part in sync (Sync:Role is Edge or Cloud). Only then are the
    /// sync schemas migrated: a server left at the default None never needs CREATE rights for
    /// them, so merging this module cannot break a deployment whose database role lacks that
    /// right (e.g. an hms_platform owned by another role).
    /// </summary>
    public static bool IsSyncEnabled(IConfiguration configuration) =>
        configuration.GetSection(SyncOptions.SectionName).GetValue(nameof(SyncOptions.Role), SyncRole.None) != SyncRole.None;

    /// <summary>True on the Cloud server, the only role that uses the sync_registry schema in
    /// the platform database.</summary>
    public static bool IsCloud(IConfiguration configuration) =>
        configuration.GetSection(SyncOptions.SectionName).GetValue(nameof(SyncOptions.Role), SyncRole.None) == SyncRole.Cloud;

    public static IServiceCollection AddSyncModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<SyncOptions>()
            .Bind(configuration.GetSection(SyncOptions.SectionName))
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<SyncOptions>, SyncOptionsValidator>();
        services.AddOptions<SyncHangfireOptions>().Bind(configuration.GetSection(SyncHangfireOptions.SectionName));

        // Read once here only to decide what to register; everything at runtime reads IOptions.
        var role = configuration.GetSection(SyncOptions.SectionName).GetValue(nameof(SyncOptions.Role), SyncRole.None);
        var platformConnectionString = configuration.GetConnectionString("Platform");

        // Tenant-aware, resolved per request (or per background scope) from ITenantContext,
        // exactly like every other hospital module's DbContext.
        services.AddDbContext<SyncDbContext>((sp, options) =>
        {
            var tenantContext = sp.GetRequiredService<ITenantContext>();
            if (!tenantContext.IsResolved)
            {
                throw new InvalidOperationException(
                    "SyncDbContext was resolved without a tenant having been established for this request.");
            }

            options.UseNpgsql(tenantContext.ConnectionString, npgsql =>
            {
                npgsql.MigrationsHistoryTable("__ef_migrations_history", SyncDbContext.SchemaName);
                npgsql.MigrationsAssembly("HMS.Database.Migrations");
            });
        });

        // The platform database (hms_platform), like PlatformDbContext.
        services.AddDbContext<SyncRegistryDbContext>(options =>
        {
            options.UseNpgsql(
                platformConnectionString ?? throw new InvalidOperationException("Missing 'ConnectionStrings:Platform' configuration value."),
                npgsql =>
                {
                    npgsql.MigrationsHistoryTable("__ef_migrations_history", SyncRegistryDbContext.SchemaName);
                    npgsql.MigrationsAssembly("HMS.Database.Migrations");
                });
        });

        services.AddMemoryCache();
        services.AddScoped<OutboxStore>();
        services.AddScoped<ISyncMonitorService, SyncMonitorService>();
        services.AddScoped<ISyncReceiverService, SyncReceiverService>();

        // The Edge → Cloud API-key scheme. Only SyncReceiverController names it, so it can never
        // authenticate a request to any other endpoint.
        services.AddAuthentication()
            .AddScheme<AuthenticationSchemeOptions, SyncApiKeyAuthenticationHandler>(SyncAuthenticationDefaults.Scheme, _ => { });
        services.AddAuthorization(options => options.AddPolicy(SyncAuthenticationDefaults.Policy, policy => policy
            .AddAuthenticationSchemes(SyncAuthenticationDefaults.Scheme)
            .RequireAuthenticatedUser()
            .RequireClaim(SyncAuthenticationDefaults.SyncClientIdClaim)));

        if (role == SyncRole.Cloud)
        {
            services.Configure<MvcOptions>(options => options.Filters.Add<CloudPatientWriteGuardFilter>());
        }

        if (role == SyncRole.Edge)
        {
            AddEdgeServices(services, configuration, platformConnectionString);
        }

        return services;
    }

    /// <summary>
    /// Maps the Hangfire dashboard on an Edge server (no-op otherwise). Call after
    /// UseHmsSecurityHeaders (which leaves the dashboard path's CSP to Hangfire) and before
    /// UseStaticFiles. The dashboard's only way in is the one-time link from the Cloud Sync page.
    /// </summary>
    public static IApplicationBuilder UseSyncDashboard(this IApplicationBuilder app)
    {
        if (!app.ApplicationServices.GetRequiredService<IOptions<SyncOptions>>().Value.IsEdge)
        {
            return app;
        }

        var hangfire = app.ApplicationServices.GetRequiredService<IOptions<SyncHangfireOptions>>().Value;
        app.UseMiddleware<HangfireDashboardHandoffMiddleware>();
        app.UseHangfireDashboard(hangfire.DashboardPath, new DashboardOptions
        {
            Authorization = [new HangfireDashboardAuthorizationFilter()],
            DisplayStorageConnectionString = false,
            DashboardTitle = "HMS Cloud Sync Jobs",
            AppPath = "/admin/cloud-sync",
        });

        return app;
    }

    private static void AddEdgeServices(IServiceCollection services, IConfiguration configuration, string? platformConnectionString)
    {
        // Its presence is what switches on patient capture (see PatientsModule).
        services.AddSingleton<IOutboxWriter, OutboxWriter>();

        // Registered before the Hangfire server so every hospital's sync tables exist before
        // capture or any job can need them (hosted services start in registration order).
        services.AddHostedService<SyncSchemaBootstrapper>();

        services.AddScoped<SyncTenantProcessor>();
        services.AddScoped<ICloudSyncClient, CloudSyncClient>();
        services.AddHttpClient(CloudSyncClient.HttpClientName, (sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<SyncOptions>>().Value;
            client.BaseAddress = new Uri(options.Cloud.BaseUrl.TrimEnd('/') + "/");
            client.Timeout = TimeSpan.FromSeconds(options.HttpTimeoutInSeconds);
        });

        services.AddDataProtection();
        services.AddSingleton<HangfireDashboardAccess>();

        services.AddTransient<SyncDispatchJob>();
        services.AddTransient<SyncTenantJob>();
        services.AddTransient<SyncProbeJob>();
        services.AddTransient<SyncCleanupJob>();

        var hangfire = configuration.GetSection(SyncHangfireOptions.SectionName).Get<SyncHangfireOptions>() ?? new SyncHangfireOptions();

        // Hangfire keeps its job state in the platform database by default (schema "hangfire",
        // created by Hangfire on first start, which needs CREATE rights there). An optional
        // ConnectionStrings:Hangfire points it somewhere else instead.
        var hangfireConnectionString = configuration.GetConnectionString("Hangfire") ?? platformConnectionString;
        services.AddHangfire(config => config
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            // Belt and braces with each job's own [AutomaticRetry(Attempts = 0)]: the outbox is
            // the only retry mechanism, so Hangfire never re-runs a sync job by itself.
            .UseFilter(new AutomaticRetryAttribute { Attempts = 0 })
            .UsePostgreSqlStorage(
                bootstrapper => bootstrapper.UseNpgsqlConnection(
                    hangfireConnectionString ?? throw new InvalidOperationException("Missing 'ConnectionStrings:Platform' configuration value.")),
                new PostgreSqlStorageOptions
                {
                    SchemaName = hangfire.SchemaName,
                    PrepareSchemaIfNecessary = true,
                    QueuePollInterval = TimeSpan.FromSeconds(5),
                }));

        services.AddHangfireServer(options =>
        {
            options.WorkerCount = Math.Max(1, hangfire.WorkerCount);
            options.ServerName = $"hms-sync:{Environment.MachineName}";
        });

        services.AddHostedService<SyncRecurringJobsRegistrar>();
    }
}

/// <summary>Registers (or updates) the recurring sync jobs on every Edge start-up, so a changed
/// <c>Sync:IntervalInMinutes</c> takes effect on restart.</summary>
internal sealed class SyncRecurringJobsRegistrar : IHostedService
{
    private readonly IRecurringJobManager _recurringJobs;
    private readonly SyncOptions _options;
    private readonly ILogger<SyncRecurringJobsRegistrar> _logger;

    public SyncRecurringJobsRegistrar(IRecurringJobManager recurringJobs, IOptions<SyncOptions> options, ILogger<SyncRecurringJobsRegistrar> logger)
    {
        _recurringJobs = recurringJobs;
        _options = options.Value;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        var utc = new RecurringJobOptions { TimeZone = TimeZoneInfo.Utc };
        var dispatchCron = _options.IntervalInMinutes == 1 ? Cron.Minutely() : $"*/{_options.IntervalInMinutes} * * * *";

        try
        {
            _recurringJobs.AddOrUpdate<SyncDispatchJob>(SyncJobIds.Dispatch, job => job.RunAsync(CancellationToken.None), dispatchCron, utc);
            _recurringJobs.AddOrUpdate<SyncProbeJob>(SyncJobIds.Probe, job => job.RunAsync(CancellationToken.None), Cron.Minutely(), utc);

            // 02:30 UTC = 08:00 IST, outside the overnight backup window.
            _recurringJobs.AddOrUpdate<SyncCleanupJob>(SyncJobIds.Cleanup, job => job.RunAsync(CancellationToken.None), Cron.Daily(2, 30), utc);

            _logger.LogInformation(
                "Cloud sync (Edge) scheduled: dispatch '{DispatchCron}', probe every minute, cleanup daily.",
                dispatchCron);
        }
        catch (Exception exception)
        {
            // Hangfire's storage (hms_platform) is unreachable: the app still serves patients
            // locally; the jobs are registered on the next start.
            _logger.LogError(exception, "Cloud sync: could not register the recurring jobs.");
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
