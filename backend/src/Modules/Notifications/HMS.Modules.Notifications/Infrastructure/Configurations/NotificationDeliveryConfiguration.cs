using HMS.Modules.Notifications.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HMS.Modules.Notifications.Infrastructure.Configurations;

internal class NotificationDeliveryConfiguration : IEntityTypeConfiguration<NotificationDelivery>
{
    public void Configure(EntityTypeBuilder<NotificationDelivery> builder)
    {
        builder.ToTable("notification_deliveries");

        builder.HasKey(d => d.Id).HasName("pk_notification_deliveries");
        builder.Property(d => d.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(d => d.NotificationRecipientId).HasColumnName("notification_recipient_id").IsRequired();
        builder.Property(d => d.Channel).HasColumnName("channel").HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(d => d.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(d => d.Attempts).HasColumnName("attempts").IsRequired().HasDefaultValue(0);
        builder.Property(d => d.LastError).HasColumnName("last_error").HasMaxLength(1000);
        builder.Property(d => d.SentAt).HasColumnName("sent_at");

        builder.Property(d => d.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(d => d.CreatedBy).HasColumnName("created_by");
        builder.Property(d => d.UpdatedAt).HasColumnName("updated_at");
        builder.Property(d => d.UpdatedBy).HasColumnName("updated_by");
        builder.Property(d => d.IsDeleted).HasColumnName("is_deleted").IsRequired().HasDefaultValue(false);
        builder.Property(d => d.DeletedAt).HasColumnName("deleted_at");
        builder.Property(d => d.DeletedBy).HasColumnName("deleted_by");

        builder.Property<uint>("xmin").HasColumnName("xmin").IsRowVersion();

        builder.HasQueryFilter(d => !d.IsDeleted);

        builder.HasOne<NotificationRecipient>()
            .WithMany()
            .HasForeignKey(d => d.NotificationRecipientId)
            .HasConstraintName("fk_notification_deliveries_notification_recipients_recipient_id")
            .OnDelete(DeleteBehavior.Restrict);

        // The background delivery worker's pending-work query (added in a later phase).
        builder.HasIndex(d => d.Status).HasDatabaseName("ix_notification_deliveries_status");
        builder.HasIndex(d => d.NotificationRecipientId).HasDatabaseName("ix_notification_deliveries_recipient_id");
    }
}
