using HMS.Modules.HR.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HMS.Modules.HR.Infrastructure.Configurations;

internal class ShiftSwapRequestConfiguration : IEntityTypeConfiguration<ShiftSwapRequest>
{
    public void Configure(EntityTypeBuilder<ShiftSwapRequest> builder)
    {
        builder.ToTable("shift_swap_requests");

        builder.HasKey(s => s.Id).HasName("pk_shift_swap_requests");
        builder.Property(s => s.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(s => s.RequestedByStaffId).HasColumnName("requested_by_staff_id").IsRequired();
        builder.Property(s => s.RequestedToStaffId).HasColumnName("requested_to_staff_id").IsRequired();
        builder.Property(s => s.CurrentShiftAssignmentId).HasColumnName("current_shift_assignment_id").IsRequired();
        builder.Property(s => s.RequestedShiftAssignmentId).HasColumnName("requested_shift_assignment_id").IsRequired();
        builder.Property(s => s.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(s => s.RequestedDate).HasColumnName("requested_date").IsRequired();
        builder.Property(s => s.ApprovedDate).HasColumnName("approved_date");
        builder.Property(s => s.ApprovedBy).HasColumnName("approved_by");
        builder.Property(s => s.Remarks).HasColumnName("remarks");

        builder.Property(s => s.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(s => s.CreatedBy).HasColumnName("created_by");
        builder.Property(s => s.UpdatedAt).HasColumnName("updated_at");
        builder.Property(s => s.UpdatedBy).HasColumnName("updated_by");
        builder.Property(s => s.IsDeleted).HasColumnName("is_deleted").IsRequired().HasDefaultValue(false);
        builder.Property(s => s.DeletedAt).HasColumnName("deleted_at");
        builder.Property(s => s.DeletedBy).HasColumnName("deleted_by");

        builder.Property<uint>("xmin").HasColumnName("xmin").IsRowVersion();

        builder.HasQueryFilter(s => !s.IsDeleted);

        // No .HasOne() to ShiftAssignment — deliberately no database foreign key.
        // CurrentShiftAssignmentId/RequestedShiftAssignmentId existence is checked in
        // ShiftSwapRequestService (application-layer referential validation only); no new
        // FK relationship was requested for this phase.
        builder.HasIndex(s => s.RequestedByStaffId).HasDatabaseName("ix_shift_swap_requests_requested_by_staff_id");
        builder.HasIndex(s => s.CurrentShiftAssignmentId).HasDatabaseName("ix_shift_swap_requests_current_shift_assignment_id");
        builder.HasIndex(s => s.RequestedShiftAssignmentId).HasDatabaseName("ix_shift_swap_requests_requested_shift_assignment_id");
    }
}
