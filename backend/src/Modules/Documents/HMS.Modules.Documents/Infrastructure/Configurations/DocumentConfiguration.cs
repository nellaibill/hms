using HMS.Modules.Documents.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HMS.Modules.Documents.Infrastructure.Configurations;

/// <summary>
/// Maps <see cref="Document"/> to documents.documents, following the naming/PK/audit-column/
/// soft-delete standards mirrored from HMS.Modules.Patients.Infrastructure.Configurations.
/// PatientConfiguration. Internal (not public): Document is an internal domain type, so a
/// public Configure(EntityTypeBuilder&lt;Document&gt;) member would be a CS0051 accessibility
/// violation — EF Core's ApplyConfigurationsFromAssembly discovers and invokes this via
/// reflection regardless of the type's own visibility.
/// </summary>
internal class DocumentConfiguration : IEntityTypeConfiguration<Document>
{
    public void Configure(EntityTypeBuilder<Document> builder)
    {
        builder.ToTable("documents");

        builder.HasKey(d => d.Id).HasName("pk_documents");
        builder.Property(d => d.Id)
            .HasColumnName("id")
            .ValueGeneratedNever(); // Generated in the domain (Guid.CreateVersion7()), not by the database.

        builder.Property(d => d.OwnerType).HasColumnName("owner_type").HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(d => d.OwnerId).HasColumnName("owner_id").IsRequired();

        builder.Property(d => d.DocumentType).HasColumnName("document_type").HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(d => d.Classification).HasColumnName("classification").HasConversion<string>().HasMaxLength(20).IsRequired();

        builder.Property(d => d.StorageKey).HasColumnName("storage_key").HasMaxLength(500).IsRequired();
        builder.Property(d => d.OriginalFileName).HasColumnName("original_file_name").HasMaxLength(255).IsRequired();
        builder.Property(d => d.ContentType).HasColumnName("content_type").HasMaxLength(150).IsRequired();
        builder.Property(d => d.SizeBytes).HasColumnName("size_bytes").IsRequired();
        builder.Property(d => d.ChecksumSha256).HasColumnName("checksum_sha256").HasMaxLength(64).IsRequired();

        builder.Property(d => d.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(d => d.IsArchived).HasColumnName("is_archived").IsRequired().HasDefaultValue(false);

        builder.Property(d => d.UploadedByUserId).HasColumnName("uploaded_by_user_id");

        // Standard audit columns (mirrors PatientConfiguration).
        builder.Property(d => d.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(d => d.CreatedBy).HasColumnName("created_by");
        builder.Property(d => d.UpdatedAt).HasColumnName("updated_at");
        builder.Property(d => d.UpdatedBy).HasColumnName("updated_by");
        builder.Property(d => d.IsDeleted).HasColumnName("is_deleted").IsRequired().HasDefaultValue(false);
        builder.Property(d => d.DeletedAt).HasColumnName("deleted_at");
        builder.Property(d => d.DeletedBy).HasColumnName("deleted_by");

        // Optimistic concurrency via Postgres's own system column (mirrors PatientConfiguration
        // — Npgsql's UseXminAsConcurrencyToken() convenience method no longer exists in the
        // referenced 10.0.0 package).
        builder.Property<uint>("xmin").HasColumnName("xmin").IsRowVersion();

        builder.HasQueryFilter(d => !d.IsDeleted);

        // The query shape every list/filter/summary call actually uses (owner scoping first,
        // status filters second) — see Repositories.DocumentRepository.
        builder.HasIndex(d => new { d.OwnerType, d.OwnerId, d.IsDeleted }).HasDatabaseName("ix_documents_owner");
        builder.HasIndex(d => d.Status).HasDatabaseName("ix_documents_status");
        builder.HasIndex(d => d.UploadedByUserId).HasDatabaseName("ix_documents_uploaded_by_user_id");
    }
}
