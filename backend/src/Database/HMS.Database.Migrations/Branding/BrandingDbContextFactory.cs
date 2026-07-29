using HMS.Modules.Branding.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace HMS.Database.Migrations.Branding;

/// <summary>
/// Lets `dotnet ef migrations add` / `dotnet ef database update` construct
/// <see cref="BrandingDbContext"/> without running the full HMS.Api host — mirrors
/// HMS.Database.Migrations.Patients.PatientsDbContextFactory.
/// </summary>
public class BrandingDbContextFactory : IDesignTimeDbContextFactory<BrandingDbContext>
{
    public BrandingDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("HMS_DESIGN_TIME_CONNECTION_STRING")
            ?? "Host=localhost;Port=5432;Database=hms_dev;Username=hms;Password=hms";

        var optionsBuilder = new DbContextOptionsBuilder<BrandingDbContext>();
        optionsBuilder.UseNpgsql(
            connectionString,
            npgsql =>
            {
                npgsql.MigrationsHistoryTable("__ef_migrations_history", BrandingDbContext.SchemaName);
                npgsql.MigrationsAssembly("HMS.Database.Migrations");
            });

        return new BrandingDbContext(optionsBuilder.Options);
    }
}
