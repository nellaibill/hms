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
    internal DbSet<DocumentChunk> DocumentChunks => Set<DocumentChunk>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(SchemaName);
        // pgvector, for document_chunks.embedding. Emitted by the migration as CREATE
        // EXTENSION IF NOT EXISTS, which needs a superuser the first time, so it's expected to
        // be pre-installed per database (and in template1 for newly provisioned tenants).
        // See docs/modules/Documents/DocumentManagement.md's pgvector setup section.
        modelBuilder.HasPostgresExtension("vector");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(DocumentsDbContext).Assembly);
    }
}
