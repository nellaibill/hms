using HMS.Modules.Billing.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace HMS.Database.Migrations.Billing;

public class BillingDbContextFactory : IDesignTimeDbContextFactory<BillingDbContext>
{
    public BillingDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("HMS_DESIGN_TIME_CONNECTION_STRING")
            ?? "Host=localhost;Port=5432;Database=hms_dev;Username=hms;Password=hms";

        var optionsBuilder = new DbContextOptionsBuilder<BillingDbContext>();
        optionsBuilder.UseNpgsql(
            connectionString,
            npgsql =>
            {
                npgsql.MigrationsHistoryTable("__ef_migrations_history", BillingDbContext.SchemaName);
                npgsql.MigrationsAssembly("HMS.Database.Migrations");
            });

        return new BillingDbContext(optionsBuilder.Options);
    }
}
