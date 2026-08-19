using HMS.Modules.Platform.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace HMS.Database.Migrations.Platform;

public class PlatformDbContextFactory : IDesignTimeDbContextFactory<PlatformDbContext>
{
    public PlatformDbContext CreateDbContext(string[] args)
    {
        // hms_platform, not hms_dev — PlatformDbContext lives in its own physical database
        // (see PlatformModule.cs's own doc comment), unlike every other module's DbContext.
        var connectionString = Environment.GetEnvironmentVariable("HMS_PLATFORM_DESIGN_TIME_CONNECTION_STRING")
            ?? "Host=localhost;Port=5432;Database=hms_platform;Username=hms;Password=hms";

        var optionsBuilder = new DbContextOptionsBuilder<PlatformDbContext>();
        optionsBuilder.UseNpgsql(
            connectionString,
            npgsql =>
            {
                npgsql.MigrationsHistoryTable("__ef_migrations_history", PlatformDbContext.SchemaName);
                npgsql.MigrationsAssembly("HMS.Database.Migrations");
            });

        return new PlatformDbContext(optionsBuilder.Options);
    }
}
