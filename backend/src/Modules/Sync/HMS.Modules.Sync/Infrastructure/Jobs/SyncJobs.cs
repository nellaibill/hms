using System.ComponentModel;
using Hangfire;
using HMS.Modules.Platform.Application.Abstractions;
using HMS.Modules.Sync.Application;
using HMS.Shared.Kernel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HMS.Modules.Sync.Infrastructure.Jobs;

/// <summary>Recurring job ids, as shown in the Hangfire dashboard.</summary>
internal static class SyncJobIds
{
    public const string Dispatch = "sync-dispatch";
    public const string Probe = "sync-probe";
    public const string Cleanup = "sync-cleanup";
}

/// <summary>
/// Shared plumbing for the sync jobs. Every job argument is a tenant id at most: no patient data
/// and no secrets ever reach Hangfire's storage. Hangfire's automatic retries are off for every
/// sync job (<c>AutomaticRetry(Attempts = 0)</c>): the outbox row is the only retry record, so a
/// job retry can never send something twice.
/// </summary>
internal abstract class TenantSyncJobBase
{
    private readonly IServiceScopeFactory _scopeFactory;

    protected TenantSyncJobBase(IServiceScopeFactory scopeFactory, ITenantDirectory tenantDirectory, IOptions<SyncOptions> options)
    {
        _scopeFactory = scopeFactory;
        TenantDirectory = tenantDirectory;
        Options = options.Value;
    }

    protected ITenantDirectory TenantDirectory { get; }

    protected SyncOptions Options { get; }

    /// <summary>Active hospitals this Edge server holds a cloud key for.</summary>
    protected async Task<IReadOnlyList<TenantInfo>> GetSyncedTenantsAsync(CancellationToken cancellationToken)
    {
        var tenants = await TenantDirectory.GetAllActiveTenantsAsync(cancellationToken);
        return tenants.Where(t => Options.FindApiKey(t.HospitalCode) is not null).ToList();
    }

    /// <summary>Runs <paramref name="work"/> in a fresh DI scope bound to <paramref name="tenant"/>,
    /// the same way OpdDayCloseSchedulerService runs per-tenant work.</summary>
    protected async Task<T> InTenantScopeAsync<T>(TenantInfo tenant, Func<IServiceProvider, Task<T>> work)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        scope.ServiceProvider.GetRequiredService<ITenantContext>()
            .SetTenant(tenant.Id, tenant.ConnectionString, tenant.EnabledModules, tenant.EnabledFeatures);
        return await work(scope.ServiceProvider);
    }
}

/// <summary>Recurring (every <c>Sync:IntervalInMinutes</c>): queues one <see cref="SyncTenantJob"/>
/// per hospital that has a cloud key. Cheap, so overlapping runs are harmless.</summary>
internal sealed class SyncDispatchJob : TenantSyncJobBase
{
    private readonly IBackgroundJobClient _jobs;
    private readonly ILogger<SyncDispatchJob> _logger;

    public SyncDispatchJob(
        IServiceScopeFactory scopeFactory,
        ITenantDirectory tenantDirectory,
        IOptions<SyncOptions> options,
        IBackgroundJobClient jobs,
        ILogger<SyncDispatchJob> logger)
        : base(scopeFactory, tenantDirectory, options)
    {
        _jobs = jobs;
        _logger = logger;
    }

    [AutomaticRetry(Attempts = 0)]
    [DisplayName("Sync: dispatch hospitals")]
    public async Task<string> RunAsync(CancellationToken cancellationToken)
    {
        if (!Options.Enabled)
        {
            return "Sending is disabled (Sync:Enabled = false).";
        }

        var tenants = await GetSyncedTenantsAsync(cancellationToken);
        foreach (var tenant in tenants)
        {
            _jobs.Enqueue<SyncTenantJob>(job => job.RunAsync(tenant.Id, CancellationToken.None));
        }

        if (tenants.Count == 0)
        {
            _logger.LogDebug("Sync dispatch: no hospital on this server has a cloud key configured.");
        }

        return $"Queued {tenants.Count} hospital(s).";
    }
}

/// <summary>Fire-and-forget: one sync run for one hospital (see <see cref="SyncTenantProcessor"/>).
/// Two runs for the same hospital can overlap safely: they claim different patients.</summary>
internal sealed class SyncTenantJob : TenantSyncJobBase
{
    private readonly ILogger<SyncTenantJob> _logger;

    public SyncTenantJob(IServiceScopeFactory scopeFactory, ITenantDirectory tenantDirectory, IOptions<SyncOptions> options, ILogger<SyncTenantJob> logger)
        : base(scopeFactory, tenantDirectory, options)
    {
        _logger = logger;
    }

    [AutomaticRetry(Attempts = 0)]
    [DisplayName("Sync: send hospital {0}")]
    public async Task<string> RunAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        var tenant = await TenantDirectory.FindByIdAsync(tenantId, cancellationToken);
        if (tenant is null || !tenant.IsActive)
        {
            return "Hospital not found or inactive.";
        }

        var summary = await InTenantScopeAsync(tenant, sp => sp.GetRequiredService<SyncTenantProcessor>().RunAsync(tenant, cancellationToken));
        _logger.LogDebug("Sync run for {HospitalCode}: {Outcome}", tenant.HospitalCode, summary.Outcome);
        return $"{summary.Outcome}: {summary.Batches} batch(es), {summary.EventsSent} sent, {summary.EventsSynced} synced.";
    }
}

/// <summary>Recurring (every minute): pings the cloud with each hospital's key and records
/// reachable / unreachable / key rejected. Sending is gated on this, so hours offline cost no
/// retries, and a rejected key stops sending until it is fixed.</summary>
internal sealed class SyncProbeJob : TenantSyncJobBase
{
    private readonly ILogger<SyncProbeJob> _logger;

    public SyncProbeJob(IServiceScopeFactory scopeFactory, ITenantDirectory tenantDirectory, IOptions<SyncOptions> options, ILogger<SyncProbeJob> logger)
        : base(scopeFactory, tenantDirectory, options)
    {
        _logger = logger;
    }

    [AutomaticRetry(Attempts = 0)]
    [DisplayName("Sync: probe cloud")]
    public async Task<string> RunAsync(CancellationToken cancellationToken)
    {
        var results = new List<string>();
        foreach (var tenant in await GetSyncedTenantsAsync(cancellationToken))
        {
            try
            {
                var result = await InTenantScopeAsync(tenant, async sp =>
                {
                    var store = sp.GetRequiredService<OutboxStore>();
                    var cloud = sp.GetRequiredService<ICloudSyncClient>();
                    await store.GetStateAsync(cancellationToken);

                    var ping = await cloud.PingAsync(Options.FindApiKey(tenant.HospitalCode)!, cancellationToken);
                    if (ping.Outcome == CloudTransportOutcome.Cancelled)
                    {
                        return "cancelled";
                    }

                    // Only a successful, authenticated ping opens the gate for sending. A 5xx or
                    // an unknown endpoint keeps it closed too, so no retries are spent on a cloud
                    // that cannot take the data yet.
                    var ok = ping.Outcome == CloudTransportOutcome.Success;
                    await store.RecordProbeAsync(
                        ok,
                        ping.Outcome == CloudTransportOutcome.AuthFailed,
                        ok ? null : ping.Error,
                        ping.DurationMs,
                        CancellationToken.None);
                    return ping.Outcome.ToString();
                });

                results.Add($"{tenant.HospitalCode}: {result}");
            }
            catch (Exception exception)
            {
                // One hospital's problem (e.g. its database is down) must not stop the others.
                _logger.LogError(exception, "Sync probe failed for hospital {HospitalCode}.", tenant.HospitalCode);
                results.Add($"{tenant.HospitalCode}: error");
            }
        }

        return results.Count == 0 ? "No hospital has a cloud key configured." : string.Join("; ", results);
    }
}

/// <summary>Recurring (daily): purges synced events older than <c>Sync:RetentionDays</c>.
/// Unsynced events are never purged.</summary>
internal sealed class SyncCleanupJob : TenantSyncJobBase
{
    private readonly ILogger<SyncCleanupJob> _logger;

    public SyncCleanupJob(IServiceScopeFactory scopeFactory, ITenantDirectory tenantDirectory, IOptions<SyncOptions> options, ILogger<SyncCleanupJob> logger)
        : base(scopeFactory, tenantDirectory, options)
    {
        _logger = logger;
    }

    [AutomaticRetry(Attempts = 0)]
    [DisplayName("Sync: purge old synced events")]
    public async Task<string> RunAsync(CancellationToken cancellationToken)
    {
        var total = 0;
        foreach (var tenant in await TenantDirectory.GetAllActiveTenantsAsync(cancellationToken))
        {
            try
            {
                total += await InTenantScopeAsync(tenant, sp => sp.GetRequiredService<OutboxStore>().PurgeSyncedAsync(Options.RetentionDays, cancellationToken));
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Sync cleanup failed for hospital {HospitalCode}.", tenant.HospitalCode);
            }
        }

        _logger.LogInformation("Sync cleanup purged {Count} synced event(s) older than {Days} days.", total, Options.RetentionDays);
        return $"Purged {total} event(s).";
    }
}
