using HMS.Modules.Laboratory.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HMS.Modules.Laboratory.Infrastructure.Configurations;

internal class LabOrderItemEventConfiguration : IEntityTypeConfiguration<LabOrderItemEvent>
{
    public void Configure(EntityTypeBuilder<LabOrderItemEvent> builder)
    {
        builder.ToTable("lab_order_item_events");

        builder.HasKey(e => e.Id).HasName("pk_lab_order_item_events");
        builder.Property(e => e.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(e => e.LabOrderItemId).HasColumnName("lab_order_item_id").IsRequired();
        builder.Property(e => e.EventType).HasColumnName("event_type").HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(e => e.ActorId).HasColumnName("actor_id");
        builder.Property(e => e.OccurredAt).HasColumnName("occurred_at").IsRequired();
        builder.Property(e => e.Remarks).HasColumnName("remarks").HasMaxLength(1000);

        builder.Property(e => e.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(e => e.CreatedBy).HasColumnName("created_by");
        builder.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        builder.Property(e => e.UpdatedBy).HasColumnName("updated_by");
        builder.Property(e => e.IsDeleted).HasColumnName("is_deleted").IsRequired().HasDefaultValue(false);
        builder.Property(e => e.DeletedAt).HasColumnName("deleted_at");
        builder.Property(e => e.DeletedBy).HasColumnName("deleted_by");

        builder.Property<uint>("xmin").HasColumnName("xmin").IsRowVersion();

        builder.HasQueryFilter(e => !e.IsDeleted);

        builder.HasIndex(e => e.LabOrderItemId).HasDatabaseName("ix_lab_order_item_events_lab_order_item_id");
        builder.HasIndex(e => e.OccurredAt).HasDatabaseName("ix_lab_order_item_events_occurred_at");
    }
}
