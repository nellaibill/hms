using HMS.Modules.Backups.Application.Abstractions;
using HMS.Modules.Backups.Configuration;
using HMS.Modules.Backups.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HMS.Modules.Backups;

/// <summary>
/// Single composition entry point for this module — mirrors every other module's
/// Add&lt;X&gt;Module convention (see BrandingModule). Deliberately has no DbContext/migration
/// of its own: a backup is a pg_dump file on disk (see IBackupStorage), not a row in any
/// database, so there is nothing here for EF Core to own. IPgDumpRunner/IBackupStorage/
/// ITenantFilesArchive are singletons (they hold no per-request state, only configuration plus
/// filesystem/process calls — the tenant is always passed in, never read from the request) —
/// safe both for the background scheduler and for the two controllers to share.
/// </summary>
public static class BackupsModule
{
    public static IServiceCollection AddBackupsModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<BackupOptions>(configuration.GetSection(BackupOptions.SectionName));

        services.AddSingleton<IPgDumpRunner, PgDumpRunner>();
        services.AddSingleton<IBackupStorage, FileSystemBackupStorage>();
        services.AddSingleton<ITenantFilesArchive, TenantFilesArchive>();
        services.AddHostedService<DailyBackupSchedulerService>();

        return services;
    }
}
