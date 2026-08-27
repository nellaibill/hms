using HMS.Modules.Notifications.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HMS.Modules.Notifications.Infrastructure.Configurations;

internal class NotificationTemplateConfiguration : IEntityTypeConfiguration<NotificationTemplate>
{
    public void Configure(EntityTypeBuilder<NotificationTemplate> builder)
    {
        builder.ToTable("notification_templates");

        builder.HasKey(t => t.Id).HasName("pk_notification_templates");
        builder.Property(t => t.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(t => t.TemplateKey).HasColumnName("template_key").HasMaxLength(100).IsRequired();
        builder.Property(t => t.Channel).HasColumnName("channel").HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(t => t.Subject).HasColumnName("subject").HasMaxLength(500);
        builder.Property(t => t.BodyTemplate).HasColumnName("body_template").HasMaxLength(4000).IsRequired();
        builder.Property(t => t.IsActive).HasColumnName("is_active").IsRequired().HasDefaultValue(true);

        builder.Property(t => t.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(t => t.CreatedBy).HasColumnName("created_by");
        builder.Property(t => t.UpdatedAt).HasColumnName("updated_at");
        builder.Property(t => t.UpdatedBy).HasColumnName("updated_by");
        builder.Property(t => t.IsDeleted).HasColumnName("is_deleted").IsRequired().HasDefaultValue(false);
        builder.Property(t => t.DeletedAt).HasColumnName("deleted_at");
        builder.Property(t => t.DeletedBy).HasColumnName("deleted_by");

        // UseXminAsConcurrencyToken() doesn't exist in the pinned Npgsql EF Core provider
        // version — manual mapping instead (docs/DeveloperHandbook.md §20).
        builder.Property<uint>("xmin").HasColumnName("xmin").IsRowVersion();

        builder.HasQueryFilter(t => !t.IsDeleted);

        builder.HasIndex(t => new { t.TemplateKey, t.Channel })
            .IsUnique()
            .HasDatabaseName("ux_notification_templates_key_channel")
            .HasFilter("is_deleted = false");
    }
}
