using HMS.Modules.Notifications.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HMS.Modules.Notifications.Infrastructure.Configurations;

internal class NotificationRecipientConfiguration : IEntityTypeConfiguration<NotificationRecipient>
{
    public void Configure(EntityTypeBuilder<NotificationRecipient> builder)
    {
        builder.ToTable("notification_recipients");

        builder.HasKey(r => r.Id).HasName("pk_notification_recipients");
        builder.Property(r => r.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(r => r.NotificationId).HasColumnName("notification_id").IsRequired();

        // No FK constraint to identity.users — see NotificationPreferenceConfiguration's
        // identical reasoning.
        builder.Property(r => r.UserId).HasColumnName("user_id").IsRequired();

        builder.Property(r => r.IsRead).HasColumnName("is_read").IsRequired().HasDefaultValue(false);
        builder.Property(r => r.ReadAt).HasColumnName("read_at");

        builder.Property(r => r.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(r => r.CreatedBy).HasColumnName("created_by");
        builder.Property(r => r.UpdatedAt).HasColumnName("updated_at");
        builder.Property(r => r.UpdatedBy).HasColumnName("updated_by");
        builder.Property(r => r.IsDeleted).HasColumnName("is_deleted").IsRequired().HasDefaultValue(false);
        builder.Property(r => r.DeletedAt).HasColumnName("deleted_at");
        builder.Property(r => r.DeletedBy).HasColumnName("deleted_by");

        builder.Property<uint>("xmin").HasColumnName("xmin").IsRowVersion();

        builder.HasQueryFilter(r => !r.IsDeleted);

        // Restrict: a Notification's recipient rows must never disappear as a side effect of
        // the Notification itself being (soft-)deleted — mirrors Payment's identical
        // reasoning against Invoice in HMS.Modules.Billing.
        builder.HasOne<Notification>()
            .WithMany()
            .HasForeignKey(r => r.NotificationId)
            .HasConstraintName("fk_notification_recipients_notifications_notification_id")
            .OnDelete(DeleteBehavior.Restrict);

        // The hot path for the notification bell — "my unread notifications, newest first".
        builder.HasIndex(r => new { r.UserId, r.IsRead }).HasDatabaseName("ix_notification_recipients_user_is_read");
        builder.HasIndex(r => r.NotificationId).HasDatabaseName("ix_notification_recipients_notification_id");
    }
}
