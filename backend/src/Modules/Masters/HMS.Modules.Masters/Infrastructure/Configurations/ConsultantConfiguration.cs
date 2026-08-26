using HMS.Modules.Masters.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HMS.Modules.Masters.Infrastructure.Configurations;

internal class ConsultantConfiguration : IEntityTypeConfiguration<Consultant>
{
    public void Configure(EntityTypeBuilder<Consultant> builder)
    {
        builder.ToTable("consultants");

        builder.HasKey(c => c.Id).HasName("pk_consultants");
        builder.Property(c => c.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(c => c.Name).HasColumnName("name").HasMaxLength(150).IsRequired();
        builder.Property(c => c.DepartmentId).HasColumnName("department_id");
        builder.Property(c => c.Specialization).HasColumnName("specialization").HasMaxLength(150);
        builder.Property(c => c.IsActive).HasColumnName("is_active").IsRequired().HasDefaultValue(true);

        builder.Property(c => c.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(c => c.CreatedBy).HasColumnName("created_by");
        builder.Property(c => c.UpdatedAt).HasColumnName("updated_at");
        builder.Property(c => c.UpdatedBy).HasColumnName("updated_by");
        builder.Property(c => c.IsDeleted).HasColumnName("is_deleted").IsRequired().HasDefaultValue(false);
        builder.Property(c => c.DeletedAt).HasColumnName("deleted_at");
        builder.Property(c => c.DeletedBy).HasColumnName("deleted_by");

        builder.Property<uint>("xmin").HasColumnName("xmin").IsRowVersion();

        builder.HasQueryFilter(c => !c.IsDeleted);

        // No uniqueness constraint on Name — unlike AppointmentType/ConsultationType, two
        // consultants legitimately can share a display name (e.g. two "Dr. Sharma"s); Code
        // used to be the disambiguator, and removing it doesn't make duplicate names an
        // error, just something the UI tells apart via Specialization instead (see
        // ConsultantSelect's own comment).
        builder.HasIndex(c => c.DepartmentId).HasDatabaseName("ix_consultants_department_id");

        // Same-module reference — a real DB-level FK is fine here (unlike the Patients/HR
        // cross-module references, which stay app-level-only per docs/Architecture.md §4).
        builder.HasOne<Department>()
            .WithMany()
            .HasForeignKey(c => c.DepartmentId)
            .HasConstraintName("fk_consultants_department_id")
            .OnDelete(DeleteBehavior.SetNull);
    }
}
