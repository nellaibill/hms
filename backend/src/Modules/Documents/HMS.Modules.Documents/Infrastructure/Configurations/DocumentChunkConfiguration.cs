using HMS.Modules.Documents.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HMS.Modules.Documents.Infrastructure.Configurations;

/// <summary>
/// Maps <see cref="DocumentChunk"/> to documents.document_chunks — the RAG retrieval table.
/// Internal for the same CS0051 reason as <see cref="DocumentConfiguration"/>.
/// </summary>
internal class DocumentChunkConfiguration : IEntityTypeConfiguration<DocumentChunk>
{
    public void Configure(EntityTypeBuilder<DocumentChunk> builder)
    {
        builder.ToTable("document_chunks");

        builder.HasKey(c => c.Id).HasName("pk_document_chunks");
        builder.Property(c => c.Id)
            .HasColumnName("id")
            .ValueGeneratedNever(); // Generated in the domain (Guid.CreateVersion7()), not by the database.

        builder.Property(c => c.SourceType).HasColumnName("source_type").HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(c => c.SourceId).HasColumnName("source_id").IsRequired();

        builder.Property(c => c.OwnerType).HasColumnName("owner_type").HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(c => c.OwnerId).HasColumnName("owner_id").IsRequired();
        builder.Property(c => c.Classification).HasColumnName("classification").HasConversion<string>().HasMaxLength(20).IsRequired();

        builder.Property(c => c.ChunkIndex).HasColumnName("chunk_index").IsRequired();
        builder.Property(c => c.Content).HasColumnName("content").HasColumnType("text").IsRequired();
        builder.Property(c => c.TokenCount).HasColumnName("token_count").IsRequired();

        builder.Property(c => c.Embedding).HasColumnName("embedding").HasColumnType($"vector({DocumentChunk.EmbeddingDimensions})");
        builder.Property(c => c.EmbeddingModel).HasColumnName("embedding_model").HasMaxLength(100);
        builder.Property(c => c.EmbeddedAt).HasColumnName("embedded_at");

        // 'simple' rather than 'english': no stemming or stop-word removal, so drug names,
        // lab codes (HbA1c) and Tamil/transliterated text are matched as written.
        builder.HasGeneratedTsVectorColumn(c => c.SearchVector, "simple", c => new { c.Content })
            .Property(c => c.SearchVector)
            .HasColumnName("search_vector");

        builder.Property(c => c.CreatedAt).HasColumnName("created_at").IsRequired();

        // Re-indexing a source replaces its chunks wholesale; this also blocks a double-run
        // of the indexer from writing the same chunk twice.
        builder.HasIndex(c => new { c.SourceType, c.SourceId, c.ChunkIndex })
            .IsUnique()
            .HasDatabaseName("ux_document_chunks_source_chunk");

        // Scoped searches ("this patient's documents") filter on owner first.
        builder.HasIndex(c => new { c.OwnerType, c.OwnerId }).HasDatabaseName("ix_document_chunks_owner");

        // Approximate nearest-neighbour search by cosine distance (the <=> operator). Rows
        // with a null embedding are simply absent from the index.
        builder.HasIndex(c => c.Embedding)
            .HasMethod("hnsw")
            .HasOperators("vector_cosine_ops")
            .HasDatabaseName("ix_document_chunks_embedding_hnsw");

        builder.HasIndex(c => c.SearchVector)
            .HasMethod("GIN")
            .HasDatabaseName("ix_document_chunks_search_vector");
    }
}
