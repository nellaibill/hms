using HMS.Modules.Calendar.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HMS.Modules.Calendar.Infrastructure.Configurations;

internal class EventConfiguration : IEntityTypeConfiguration<Event>
{
    public void Configure(EntityTypeBuilder<Event> builder)
    {
        builder.ToTable("events");

        builder.HasKey(e => e.Id).HasName("pk_events");
        builder.Property(e => e.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(e => e.Title).HasColumnName("title").HasMaxLength(200).IsRequired();
        builder.Property(e => e.Description).HasColumnName("description").HasMaxLength(2000);

        // Stored as the enum member's own name, not its ordinal — same convention as
        // AssignmentStatus/SwapRequestStatus elsewhere in HR, so the raw table is
        // self-describing and resilient to enum member reordering.
        builder.Property(e => e.EventType).HasColumnName("event_type").HasConversion<string>().HasMaxLength(30).IsRequired();

        builder.Property(e => e.StartDate).HasColumnName("start_date").IsRequired();
        builder.Property(e => e.EndDate).HasColumnName("end_date").IsRequired();
        builder.Property(e => e.IsAllDay).HasColumnName("is_all_day").IsRequired().HasDefaultValue(false);
        builder.Property(e => e.DepartmentId).HasColumnName("department_id");

        builder.Property(e => e.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(e => e.CreatedBy).HasColumnName("created_by");
        builder.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        builder.Property(e => e.UpdatedBy).HasColumnName("updated_by");
        builder.Property(e => e.IsDeleted).HasColumnName("is_deleted").IsRequired().HasDefaultValue(false);
        builder.Property(e => e.DeletedAt).HasColumnName("deleted_at");
        builder.Property(e => e.DeletedBy).HasColumnName("deleted_by");

        builder.Property<uint>("xmin").HasColumnName("xmin").IsRowVersion();

        builder.HasQueryFilter(e => !e.IsDeleted);

        builder.HasIndex(e => e.EventType).HasDatabaseName("ix_events_event_type");
        builder.HasIndex(e => e.DepartmentId).HasDatabaseName("ix_events_department_id");

        // Two distinct indexes over the same StartDate column, so both must be named
        // via the (expression, name) overload at the point of definition — calling
        // HasIndex(e => e.StartDate) a second time without a name up front makes EF
        // Core treat it as re-configuring the *same* index (matched by property list),
        // silently merging the two instead of creating a second one.
        builder.HasIndex(e => e.StartDate, "ix_events_start_date");

        // "Holiday dates must be unique" — enforced at the DB level (not just in
        // EventService), so it holds even under concurrent requests. Only applies to
        // Holiday-type, non-deleted rows — a Meeting and a Holiday can share a date,
        // and a soft-deleted Holiday doesn't block recreating one on the same date.
        builder.HasIndex(e => e.StartDate, "ux_events_holiday_start_date")
            .IsUnique()
            .HasFilter("event_type = 'Holiday' AND is_deleted = false");
    }
}
