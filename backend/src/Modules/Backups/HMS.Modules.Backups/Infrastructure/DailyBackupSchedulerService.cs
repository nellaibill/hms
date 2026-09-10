using HMS.Modules.Backups.Application.Abstractions;
using HMS.Modules.Platform.Application.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace HMS.Modules.Backups.Infrastructure;

/// <summary>
/// Runs the daily backup at 1:00 AM India Standard Time — masters first, then every active
/// tenant, then retention cleanup. An in-process timer rather than an OS-level scheduler (the
/// alternative this codebase's existing "migrate" one-shot command would otherwise suggest),
/// chosen so the whole feature is self-contained in the app with nothing extra to configure on
/// the host — the tradeoff is that an app restart right around 1 AM could skip a day, which
/// the startup catch-up check below exists specifically to cover.
/// </summary>
internal sealed class DailyBackupSchedulerService : BackgroundService
{
    private const string MastersKey = "masters";
    private const int RunHourIst = 1;

    private static readonly TimeZoneInfo IndiaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<DailyBackupSchedulerService> _logger;

    public DailyBackupSchedulerService(IServiceScopeFactory scopeFactory, IConfiguration configuration, ILogger<DailyBackupSchedulerService> logger)
    {
        _scopeFactory = scopeFactory;
        _configuration = configuration;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (await IsTodaysBackupMissingAsync(stoppingToken))
        {
            _logger.LogInformation("It is already past 1 AM IST and today's backup hasn't run yet (likely a restart) — running it now before resuming the normal schedule.");
            await RunBackupAsync(stoppingToken);
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            var delay = TimeUntilNextRun();
            _logger.LogInformation("Next daily database backup scheduled in {Delay} (1:00 AM IST).", delay);

            try
            {
                await Task.Delay(delay, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            await RunBackupAsync(stoppingToken);
        }
    }

    private static TimeSpan TimeUntilNextRun()
    {
        var nowIst = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, IndiaTimeZone);
        var nextRunIst = nowIst.Date.AddHours(RunHourIst);
        if (nextRunIst <= nowIst)
        {
            nextRunIst = nextRunIst.AddDays(1);
        }

        var nextRunUtc = TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(nextRunIst, DateTimeKind.Unspecified), IndiaTimeZone);
        var delay = nextRunUtc - DateTime.UtcNow;
        return delay > TimeSpan.Zero ? delay : TimeSpan.FromSeconds(1);
    }

    private async Task<bool> IsTodaysBackupMissingAsync(CancellationToken cancellationToken)
    {
        var nowIst = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, IndiaTimeZone);
        if (nowIst.Hour < RunHourIst)
        {
            // Not yet past today's scheduled time — nothing to catch up on; the wait loop
            // below will reach 1 AM normally.
            return false;
        }

        using var scope = _scopeFactory.CreateScope();
        var storage = scope.ServiceProvider.GetRequiredService<IBackupStorage>();
        var latestMasters = await storage.GetLatestAsync(MastersKey, cancellationToken);
        var todayIst = DateOnly.FromDateTime(nowIst);
        return latestMasters is null || latestMasters.Date < todayIst;
    }

    private async Task RunBackupAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var runner = scope.ServiceProvider.GetRequiredService<IPgDumpRunner>();
        var storage = scope.ServiceProvider.GetRequiredService<IBackupStorage>();
        var tenantDirectory = scope.ServiceProvider.GetRequiredService<ITenantDirectory>();

        var today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, IndiaTimeZone));
        _logger.LogInformation("Starting daily database backup run for {Date}.", today);

        var mastersConnectionString = _configuration.GetConnectionString("Platform")
            ?? throw new InvalidOperationException("Missing 'ConnectionStrings:Platform' configuration value.");
        await DumpOneAsync(runner, storage, MastersKey, "Masters", mastersConnectionString, today, cancellationToken);

        IReadOnlyList<TenantInfo> tenants;
        try
        {
            tenants = await tenantDirectory.GetAllActiveTenantsAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            // Masters still got its attempt above — a tenant-enumeration failure shouldn't be
            // reported as if nothing happened at all.
            _logger.LogError(ex, "Failed to enumerate active tenants for the daily backup run — no tenant backups were attempted today.");
            tenants = Array.Empty<TenantInfo>();
        }

        foreach (var tenant in tenants)
        {
            // One tenant's failure must not stop the rest — DumpOneAsync logs and returns
            // rather than throwing, so this loop always runs to completion.
            await DumpOneAsync(runner, storage, tenant.Id.ToString(), tenant.HospitalCode, tenant.ConnectionString, today, cancellationToken);
        }

        await storage.CleanupOldBackupsAsync(cancellationToken);
        _logger.LogInformation("Daily database backup run for {Date} complete ({TenantCount} active tenant(s) attempted).", today, tenants.Count);
    }

    private async Task DumpOneAsync(
        IPgDumpRunner runner,
        IBackupStorage storage,
        string key,
        string label,
        string connectionString,
        DateOnly date,
        CancellationToken cancellationToken)
    {
        var outputPath = storage.ResolveOutputPath(key, date);
        var result = await runner.RunAsync(connectionString, outputPath, cancellationToken);
        if (result.Succeeded)
        {
            _logger.LogInformation("Backed up {Label} successfully to {Path}.", label, outputPath);
        }
        else
        {
            _logger.LogError("Backup failed for {Label}: {Error}", label, result.ErrorOutput);
        }
    }
}
