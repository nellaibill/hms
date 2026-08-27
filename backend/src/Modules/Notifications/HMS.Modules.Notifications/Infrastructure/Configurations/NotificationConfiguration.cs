using HMS.Modules.Notifications.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HMS.Modules.Notifications.Infrastructure.Configurations;

internal class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("notifications");

        builder.HasKey(n => n.Id).HasName("pk_notifications");
        builder.Property(n => n.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(n => n.TemplateKey).HasColumnName("template_key").HasMaxLength(100).IsRequired();
        builder.Property(n => n.Category).HasColumnName("category").HasMaxLength(50).IsRequired();
        builder.Property(n => n.Title).HasColumnName("title").HasMaxLength(300).IsRequired();
        builder.Property(n => n.Body).HasColumnName("body").HasMaxLength(4000).IsRequired();
        builder.Property(n => n.SourceModule).HasColumnName("source_module").HasMaxLength(100).IsRequired();
        builder.Property(n => n.SourceEntityType).HasColumnName("source_entity_type").HasMaxLength(100);
        builder.Property(n => n.SourceEntityId).HasColumnName("source_entity_id");
        builder.Property(n => n.Severity).HasColumnName("severity").HasConversion<string>().HasMaxLength(20).IsRequired();

        builder.Property(n => n.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(n => n.CreatedBy).HasColumnName("created_by");
        builder.Property(n => n.UpdatedAt).HasColumnName("updated_at");
        builder.Property(n => n.UpdatedBy).HasColumnName("updated_by");
        builder.Property(n => n.IsDeleted).HasColumnName("is_deleted").IsRequired().HasDefaultValue(false);
        builder.Property(n => n.DeletedAt).HasColumnName("deleted_at");
        builder.Property(n => n.DeletedBy).HasColumnName("deleted_by");

        // Mapped for consistency with every Entity-derived configuration in this codebase,
        // even though Notification exposes no Update method (it's immutable once written —
        // see the class doc comment), same reasoning as PharmacyStockTransactionConfiguration.
        builder.Property<uint>("xmin").HasColumnName("xmin").IsRowVersion();

        builder.HasQueryFilter(n => !n.IsDeleted);

        builder.HasIndex(n => new { n.SourceEntityType, n.SourceEntityId }).HasDatabaseName("ix_notifications_source_entity");
    }
}
