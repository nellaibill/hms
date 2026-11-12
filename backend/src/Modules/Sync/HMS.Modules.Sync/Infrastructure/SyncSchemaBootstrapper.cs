using HMS.Modules.Platform.Application.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace HMS.Modules.Sync.Infrastructure;

/// <summary>
/// Edge servers only (ADR-133). Capture writes every patient save's outbox event into the
/// hospital's <c>sync</c> schema, so a hospital database without that schema would refuse every
/// patient save. A hospital provisioned while this server's role was None, or not re-migrated
/// since the role changed, would hit exactly that. This brings the <c>sync</c> schema of every
/// active hospital up to date before the server accepts requests (hosted services start before
/// Kestrel listens). Idempotent: an up-to-date database is a no-op. One hospital failing is
/// logged and does not stop the others or the server.
/// </summary>
internal sealed class SyncSchemaBootstrapper : IHostedService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SyncSchemaBootstrapper> _logger;

    public SyncSchemaBootstrapper(IServiceScopeFactory scopeFactory, ILogger<SyncSchemaBootstrapper> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<TenantInfo> tenants;
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            tenants = await scope.ServiceProvider.GetRequiredService<ITenantDirectory>().GetAllActiveTenantsAsync(cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            _logger.LogError(exception, "Cloud sync: could not list hospitals to prepare their sync tables.");
            return;
        }

        foreach (var tenant in tenants)
        {
            try
            {
                var options = new DbContextOptionsBuilder<SyncDbContext>()
                    .UseNpgsql(tenant.ConnectionString, npgsql =>
                    {
                        npgsql.MigrationsHistoryTable("__ef_migrations_history", SyncDbContext.SchemaName);
                        npgsql.MigrationsAssembly("HMS.Database.Migrations");
                    })
                    .Options;

                await using var db = new SyncDbContext(options);
                await db.Database.MigrateAsync(cancellationToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                // Patient saves for this hospital fail until its sync tables exist: say so loudly.
                _logger.LogCritical(
                    exception,
                    "Cloud sync: the sync tables for hospital {HospitalCode} could not be created or updated. Patient saves for this hospital will fail until they are (run Migrate for it).",
                    tenant.HospitalCode);
            }
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
