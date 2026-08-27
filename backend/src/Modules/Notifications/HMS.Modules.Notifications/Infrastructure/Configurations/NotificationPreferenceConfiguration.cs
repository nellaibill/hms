using HMS.Modules.Notifications.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HMS.Modules.Notifications.Infrastructure.Configurations;

internal class NotificationPreferenceConfiguration : IEntityTypeConfiguration<NotificationPreference>
{
    public void Configure(EntityTypeBuilder<NotificationPreference> builder)
    {
        builder.ToTable("notification_preferences");

        builder.HasKey(p => p.Id).HasName("pk_notification_preferences");
        builder.Property(p => p.Id).HasColumnName("id").ValueGeneratedNever();

        // No FK constraint to identity.users — cross-schema references are a deliberate,
        // reviewed exception (docs/DatabaseArchitecture.md §7), not a default; mirrors
        // HMS.Modules.Pharmacy's treatment of PatientId/ProductId (plain indexed column,
        // no HasOne<>()).
        builder.Property(p => p.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(p => p.Category).HasColumnName("category").HasMaxLength(50).IsRequired();
        builder.Property(p => p.InAppEnabled).HasColumnName("in_app_enabled").IsRequired();
        builder.Property(p => p.EmailEnabled).HasColumnName("email_enabled").IsRequired();
        builder.Property(p => p.SmsEnabled).HasColumnName("sms_enabled").IsRequired();

        builder.Property(p => p.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(p => p.CreatedBy).HasColumnName("created_by");
        builder.Property(p => p.UpdatedAt).HasColumnName("updated_at");
        builder.Property(p => p.UpdatedBy).HasColumnName("updated_by");
        builder.Property(p => p.IsDeleted).HasColumnName("is_deleted").IsRequired().HasDefaultValue(false);
        builder.Property(p => p.DeletedAt).HasColumnName("deleted_at");
        builder.Property(p => p.DeletedBy).HasColumnName("deleted_by");

        builder.Property<uint>("xmin").HasColumnName("xmin").IsRowVersion();

        builder.HasQueryFilter(p => !p.IsDeleted);

        builder.HasIndex(p => new { p.UserId, p.Category })
            .IsUnique()
            .HasDatabaseName("ux_notification_preferences_user_category")
            .HasFilter("is_deleted = false");
    }
}
