using HMS.Modules.Platform.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HMS.Modules.Platform.Infrastructure.Configurations;

internal class IdempotencyRecordConfiguration : IEntityTypeConfiguration<IdempotencyRecord>
{
    public void Configure(EntityTypeBuilder<IdempotencyRecord> builder)
    {
        builder.ToTable("idempotency_keys");

        builder.HasKey(r => r.Id).HasName("pk_idempotency_keys");
        builder.Property(r => r.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(r => r.Key).HasColumnName("key").HasMaxLength(200).IsRequired();
        builder.Property(r => r.RequestHash).HasColumnName("request_hash").HasMaxLength(64).IsRequired();
        builder.Property(r => r.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(r => r.ResultIsSuccess).HasColumnName("result_is_success");
        builder.Property(r => r.ResultErrorCode).HasColumnName("result_error_code").HasMaxLength(100);
        builder.Property(r => r.ResultErrorMessage).HasColumnName("result_error_message");
        builder.Property(r => r.ResultResponseJson).HasColumnName("result_response_json");
        builder.Property(r => r.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(r => r.CompletedAt).HasColumnName("completed_at");

        // The single source of the race-safety guarantee: two concurrent requests reserving
        // the same key can never both succeed at inserting — see IdempotencyRecord's own doc
        // comment.
        builder.HasIndex(r => r.Key).IsUnique().HasDatabaseName("ux_idempotency_keys_key");
    }
}
