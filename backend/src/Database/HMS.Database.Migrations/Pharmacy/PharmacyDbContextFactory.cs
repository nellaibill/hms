using HMS.Modules.Pharmacy.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace HMS.Database.Migrations.Pharmacy;

public class PharmacyDbContextFactory : IDesignTimeDbContextFactory<PharmacyDbContext>
{
    public PharmacyDbContext CreateDbContext(string[] args)
    {
        // Fallback matches backend/src/HMS.Api/appsettings.Development.json's
        // ConnectionStrings:Default exactly (docs/DeveloperHandbook.md §20 — a prior module's
        // design-time factory drifted from the real dev database and `dotnet ef` commands
        // stopped working out of the box).
        var connectionString = Environment.GetEnvironmentVariable("HMS_DESIGN_TIME_CONNECTION_STRING")
            ?? "Host=localhost;Port=5432;Database=hms_qa;Username=hms;Password=hms";

        var optionsBuilder = new DbContextOptionsBuilder<PharmacyDbContext>();
        optionsBuilder.UseNpgsql(
            connectionString,
            npgsql =>
            {
                npgsql.MigrationsHistoryTable("__ef_migrations_history", PharmacyDbContext.SchemaName);
                npgsql.MigrationsAssembly("HMS.Database.Migrations");
            });

        return new PharmacyDbContext(optionsBuilder.Options);
    }
}
