using HMS.Modules.Appointments.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HMS.Modules.Appointments.Infrastructure.Configurations;

internal class ConsultantBlockConfiguration : IEntityTypeConfiguration<ConsultantBlock>
{
    public void Configure(EntityTypeBuilder<ConsultantBlock> builder)
    {
        builder.ToTable("consultant_blocks");

        builder.HasKey(b => b.Id).HasName("pk_consultant_blocks");
        builder.Property(b => b.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(b => b.ConsultantId).HasColumnName("consultant_id").IsRequired();
        builder.Property(b => b.FromDate).HasColumnName("from_date").IsRequired();
        builder.Property(b => b.ToDate).HasColumnName("to_date").IsRequired();
        builder.Property(b => b.Session).HasColumnName("session");
        builder.Property(b => b.Reason).HasColumnName("reason").HasMaxLength(Appointment.MaxReasonLength).IsRequired();

        builder.Property(b => b.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(b => b.CreatedBy).HasColumnName("created_by");
        builder.Property(b => b.UpdatedAt).HasColumnName("updated_at");
        builder.Property(b => b.UpdatedBy).HasColumnName("updated_by");
        builder.Property(b => b.IsDeleted).HasColumnName("is_deleted").IsRequired().HasDefaultValue(false);
        builder.Property(b => b.DeletedAt).HasColumnName("deleted_at");
        builder.Property(b => b.DeletedBy).HasColumnName("deleted_by");

        builder.Property<uint>("xmin").HasColumnName("xmin").IsRowVersion();

        builder.HasQueryFilter(b => !b.IsDeleted);

        builder.HasIndex(b => new { b.ConsultantId, b.FromDate, b.ToDate }, "ix_consultant_blocks_consultant_dates");
    }
}
