using HMS.Modules.Documents.Domain;
using Microsoft.EntityFrameworkCore;

namespace HMS.Modules.Documents.Infrastructure;

/// <summary>
/// Owns the "documents" PostgreSQL schema. Per the pattern established by
/// HMS.Modules.Patients.Infrastructure.PatientsDbContext, only this module's own code
/// constructs/migrates this context — no other module references it.
/// </summary>
public class DocumentsDbContext : DbContext
{
    public const string SchemaName = "documents";

    public DocumentsDbContext(DbContextOptions<DocumentsDbContext> options) : base(options)
    {
    }

    // Internal (not public): Document is an internal domain type, so a public DbSet<T>
    // property would be a CS0053 accessibility violation. The context itself stays public
    // (HMS.Api's Program.cs resolves it by type for the startup migration call).
    internal DbSet<Document> Documents => Set<Document>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(SchemaName);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(DocumentsDbContext).Assembly);
    }
}
