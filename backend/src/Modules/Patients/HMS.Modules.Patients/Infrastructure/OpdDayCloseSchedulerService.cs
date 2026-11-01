using HMS.Modules.Patients.Application;
using HMS.Modules.Platform.Application.Abstractions;
using HMS.Shared.Kernel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace HMS.Modules.Patients.Infrastructure;

/// <summary>
/// Runs the OPD day close (ADR-110) at 3:00 AM India Standard Time for every active tenant:
/// OP consultation lines still Waiting/CheckedIn/InConsultation from an earlier day become
/// AutoCompleted. 3 AM rather than midnight so a patient registered late in the evening isn't
/// closed while still in the consultant's room. Same in-process timer shape as
/// HMS.Modules.Backups' DailyBackupSchedulerService. The close itself is idempotent, so the
/// startup catch-up below simply runs it again if the app starts after 3 AM — no record of the
/// last run is needed. Before 3 AM it waits, for the same late-evening reason.
/// </summary>
internal sealed class OpdDayCloseSchedulerService : BackgroundService
{
    private const int RunHourIst = 3;

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OpdDayCloseSchedulerService> _logger;

    public OpdDayCloseSchedulerService(IServiceScopeFactory scopeFactory, ILogger<OpdDayCloseSchedulerService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, MonthlySeries.HospitalTimeZone).Hour >= RunHourIst)
        {
            await RunForAllTenantsAsync(stoppingToken);
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            var delay = TimeUntilNextRun();
            _logger.LogInformation("Next OPD day close scheduled in {Delay} (3:00 AM IST).", delay);

            try
            {
                await Task.Delay(delay, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            await RunForAllTenantsAsync(stoppingToken);
        }
    }

    private static TimeSpan TimeUntilNextRun()
    {
        var nowIst = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, MonthlySeries.HospitalTimeZone);
        var nextRunIst = nowIst.Date.AddHours(RunHourIst);
        if (nextRunIst <= nowIst)
        {
            nextRunIst = nextRunIst.AddDays(1);
        }

        var nextRunUtc = TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(nextRunIst, DateTimeKind.Unspecified), MonthlySeries.HospitalTimeZone);
        var delay = nextRunUtc - DateTime.UtcNow;
        return delay > TimeSpan.Zero ? delay : TimeSpan.FromSeconds(1);
    }

    private async Task RunForAllTenantsAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<TenantInfo> tenants;
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var tenantDirectory = scope.ServiceProvider.GetRequiredService<ITenantDirectory>();
            tenants = await tenantDirectory.GetAllActiveTenantsAsync(cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Failed to enumerate active tenants for the OPD day close — no consultations were auto-completed this run.");
            return;
        }

        var nowUtc = DateTime.UtcNow;
        foreach (var tenant in tenants)
        {
            // One tenant's failure must not stop the rest — its own scope (and DbContext) is
            // simply discarded, and tomorrow's run picks up whatever it left open.
            try
            {
                using var scope = _scopeFactory.CreateScope();
                scope.ServiceProvider.GetRequiredService<ITenantContext>().SetTenant(tenant.Id, tenant.ConnectionString);

                var dayClose = scope.ServiceProvider.GetRequiredService<IOpdDayCloseService>();
                var closed = await dayClose.AutoCompleteStaleConsultationsAsync(nowUtc, cancellationToken);
                if (closed > 0)
                {
                    _logger.LogInformation("OPD day close auto-completed {Count} consultation(s) for {HospitalCode}.", closed, tenant.HospitalCode);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "OPD day close failed for {HospitalCode}.", tenant.HospitalCode);
            }
        }
    }
}
