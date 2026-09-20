using HMS.Modules.ActivityLog.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HMS.Modules.ActivityLog.Infrastructure.Configurations;

internal class ActivityLogEntryConfiguration : IEntityTypeConfiguration<ActivityLogEntry>
{
    public void Configure(EntityTypeBuilder<ActivityLogEntry> builder)
    {
        builder.ToTable("activity_logs");

        builder.HasKey(a => a.Id).HasName("pk_activity_logs");
        builder.Property(a => a.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(a => a.TenantId).HasColumnName("tenant_id");

        // No FK constraint to identity.users — cross-module references are app-level only,
        // and an audit row must outlive (and never block) deletion of the user it names.
        builder.Property(a => a.UserId).HasColumnName("user_id");

        builder.Property(a => a.Action).HasColumnName("action").HasMaxLength(100).IsRequired();
        builder.Property(a => a.Module).HasColumnName("module").HasMaxLength(100).IsRequired();
        builder.Property(a => a.EntityType).HasColumnName("entity_type").HasMaxLength(100);
        builder.Property(a => a.EntityId).HasColumnName("entity_id").HasMaxLength(100);
        builder.Property(a => a.Description).HasColumnName("description").HasMaxLength(1000);

        builder.Property(a => a.OldValues).HasColumnName("old_values").HasColumnType("jsonb");
        builder.Property(a => a.NewValues).HasColumnName("new_values").HasColumnType("jsonb");

        builder.Property(a => a.IpAddress).HasColumnName("ip_address").HasMaxLength(45);
        builder.Property(a => a.UserAgent).HasColumnName("user_agent").HasMaxLength(512);
        builder.Property(a => a.CorrelationId).HasColumnName("correlation_id").HasMaxLength(100);
        builder.Property(a => a.IsSuccess).HasColumnName("is_success").IsRequired();
        builder.Property(a => a.CreatedAt).HasColumnName("created_at").IsRequired();

        // The default list view (newest first) and the date-range filter.
        builder.HasIndex(a => a.CreatedAt).HasDatabaseName("ix_activity_logs_created_at");
        builder.HasIndex(a => new { a.UserId, a.CreatedAt }).HasDatabaseName("ix_activity_logs_user_id_created_at");
        builder.HasIndex(a => new { a.Module, a.CreatedAt }).HasDatabaseName("ix_activity_logs_module_created_at");
        builder.HasIndex(a => new { a.EntityType, a.EntityId }).HasDatabaseName("ix_activity_logs_entity");
        builder.HasIndex(a => a.CorrelationId).HasDatabaseName("ix_activity_logs_correlation_id");
    }
}
