using HMS.Modules.OpdConsultation.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace HMS.Database.Migrations.OpdConsultation;

/// <summary>
/// Lets `dotnet ef migrations add` / `dotnet ef database update` construct
/// <see cref="OpdConsultationDbContext"/> without running the full HMS.Api host. The connection
/// string here is used only at design time — the running application always supplies its own
/// via HMS.Api configuration (see docs/Configuration.md).
/// </summary>
public class OpdConsultationDbContextFactory : IDesignTimeDbContextFactory<OpdConsultationDbContext>
{
    public OpdConsultationDbContext CreateDbContext(string[] args)
    {
        // Fallback matches HMS.Api's appsettings.Development.json exactly, so `dotnet ef`
        // works out of the box against the same local dev database the running API uses.
        // Override with HMS_DESIGN_TIME_CONNECTION_STRING for any other target.
        var connectionString = Environment.GetEnvironmentVariable("HMS_DESIGN_TIME_CONNECTION_STRING")
            ?? "Host=localhost;Port=5432;Database=hms_dev;Username=hms;Password=hms";

        var optionsBuilder = new DbContextOptionsBuilder<OpdConsultationDbContext>();
        optionsBuilder.UseNpgsql(
            connectionString,
            npgsql =>
            {
                npgsql.MigrationsHistoryTable("__ef_migrations_history", OpdConsultationDbContext.SchemaName);

                // Must match HMS.Modules.OpdConsultation.OpdConsultationModule's runtime
                // registration — migration classes live in this assembly
                // (HMS.Database.Migrations), not in HMS.Modules.OpdConsultation, which is
                // where EF Core looks for them by default.
                npgsql.MigrationsAssembly("HMS.Database.Migrations");
            });

        return new OpdConsultationDbContext(optionsBuilder.Options);
    }
}
