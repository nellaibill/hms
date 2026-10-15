using HMS.Modules.Appointments.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HMS.Modules.Appointments.Infrastructure.Configurations;

internal class AppointmentConfiguration : IEntityTypeConfiguration<Appointment>
{
    /// <summary>The unique index that makes double-booking a seat impossible, even when two
    /// desks save at the same moment. AppointmentRepository.TrySaveChangesAsync recognises it.</summary>
    public const string SeatIndexName = "ux_appointments_consultant_date_start_seat";

    public void Configure(EntityTypeBuilder<Appointment> builder)
    {
        builder.ToTable("appointments");

        builder.HasKey(a => a.Id).HasName("pk_appointments");
        builder.Property(a => a.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(a => a.AppointmentNumber).HasColumnName("appointment_number").HasMaxLength(30).IsRequired();

        builder.Property(a => a.PatientId).HasColumnName("patient_id");
        builder.Property(a => a.PatientName).HasColumnName("patient_name").HasMaxLength(Appointment.MaxNameLength).IsRequired();
        builder.Property(a => a.PhoneNumber).HasColumnName("phone_number").HasMaxLength(20).IsRequired();
        builder.Property(a => a.Uhid).HasColumnName("uhid").HasMaxLength(30);

        builder.Property(a => a.DepartmentId).HasColumnName("department_id").IsRequired();
        builder.Property(a => a.ConsultantId).HasColumnName("consultant_id").IsRequired();
        builder.Property(a => a.AppointmentTypeId).HasColumnName("appointment_type_id");

        builder.Property(a => a.AppointmentDate).HasColumnName("appointment_date").IsRequired();
        builder.Property(a => a.StartTime).HasColumnName("start_time").IsRequired();
        builder.Property(a => a.EndTime).HasColumnName("end_time").IsRequired();
        builder.Property(a => a.Session).HasColumnName("session").IsRequired();
        builder.Property(a => a.SeatNumber).HasColumnName("seat_number").IsRequired();
        builder.Property(a => a.TokenNumber).HasColumnName("token_number").IsRequired();

        builder.Property(a => a.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(a => a.Source).HasColumnName("source").HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(a => a.Notes).HasColumnName("notes").HasMaxLength(Appointment.MaxNotesLength);

        builder.Property(a => a.CancelReason).HasColumnName("cancel_reason").HasMaxLength(Appointment.MaxReasonLength);
        builder.Property(a => a.CancelledAt).HasColumnName("cancelled_at");
        builder.Property(a => a.CancelledBy).HasColumnName("cancelled_by");
        builder.Property(a => a.VisitId).HasColumnName("visit_id");
        builder.Property(a => a.ArrivedAt).HasColumnName("arrived_at");
        builder.Property(a => a.ArrivedBy).HasColumnName("arrived_by");
        builder.Property(a => a.NoShowAt).HasColumnName("no_show_at");
        builder.Property(a => a.RescheduleCount).HasColumnName("reschedule_count").IsRequired().HasDefaultValue(0);

        builder.Property(a => a.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(a => a.CreatedBy).HasColumnName("created_by");
        builder.Property(a => a.UpdatedAt).HasColumnName("updated_at");
        builder.Property(a => a.UpdatedBy).HasColumnName("updated_by");
        builder.Property(a => a.IsDeleted).HasColumnName("is_deleted").IsRequired().HasDefaultValue(false);
        builder.Property(a => a.DeletedAt).HasColumnName("deleted_at");
        builder.Property(a => a.DeletedBy).HasColumnName("deleted_by");

        builder.Property<uint>("xmin").HasColumnName("xmin").IsRowVersion();

        builder.HasQueryFilter(a => !a.IsDeleted);

        builder.HasIndex(a => a.AppointmentNumber, "ux_appointments_appointment_number").IsUnique();

        // The day list and slot grid both read one consultant's (or everyone's) day.
        builder.HasIndex(a => new { a.AppointmentDate, a.ConsultantId }, "ix_appointments_date_consultant");
        builder.HasIndex(a => a.PatientId, "ix_appointments_patient_id");
        builder.HasIndex(a => a.PhoneNumber, "ix_appointments_phone_number");

        // Only rows that hold a seat take part: a cancelled or no-show booking gives its seat back.
        builder.HasIndex(a => new { a.ConsultantId, a.AppointmentDate, a.StartTime, a.SeatNumber }, SeatIndexName)
            .IsUnique()
            .HasFilter("status IN ('Booked', 'Arrived') AND is_deleted = false");
    }
}
