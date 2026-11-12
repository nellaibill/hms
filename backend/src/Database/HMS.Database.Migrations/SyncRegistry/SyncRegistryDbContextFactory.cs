using HMS.Modules.Sync.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace HMS.Database.Migrations.SyncRegistry;

/// <summary>
/// Design-time factory for <see cref="SyncRegistryDbContext"/> (schema "sync_registry" in the
/// platform database, hms_platform). Design time only.
/// </summary>
public class SyncRegistryDbContextFactory : IDesignTimeDbContextFactory<SyncRegistryDbContext>
{
    public SyncRegistryDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("HMS_PLATFORM_DESIGN_TIME_CONNECTION_STRING")
            ?? "Host=localhost;Port=5432;Database=hms_platform;Username=hms;Password=hms";

        var optionsBuilder = new DbContextOptionsBuilder<SyncRegistryDbContext>();
        optionsBuilder.UseNpgsql(
            connectionString,
            npgsql =>
            {
                npgsql.MigrationsHistoryTable("__ef_migrations_history", SyncRegistryDbContext.SchemaName);
                npgsql.MigrationsAssembly("HMS.Database.Migrations");
            });

        return new SyncRegistryDbContext(optionsBuilder.Options);
    }
}
