using HMS.Modules.IPD.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace HMS.Database.Migrations.IPD;

public class IPDDbContextFactory : IDesignTimeDbContextFactory<IPDDbContext>
{
    public IPDDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("HMS_DESIGN_TIME_CONNECTION_STRING")
            ?? "Host=localhost;Port=5432;Database=hms_dev;Username=hms;Password=hms";

        var optionsBuilder = new DbContextOptionsBuilder<IPDDbContext>();
        optionsBuilder.UseNpgsql(
            connectionString,
            npgsql =>
            {
                npgsql.MigrationsHistoryTable("__ef_migrations_history", IPDDbContext.SchemaName);
                npgsql.MigrationsAssembly("HMS.Database.Migrations");
            });

        return new IPDDbContext(optionsBuilder.Options);
    }
}
