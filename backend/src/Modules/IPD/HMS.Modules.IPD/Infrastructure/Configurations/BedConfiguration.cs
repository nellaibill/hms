using HMS.Modules.IPD.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HMS.Modules.IPD.Infrastructure.Configurations;

internal class BedConfiguration : IEntityTypeConfiguration<Bed>
{
    public void Configure(EntityTypeBuilder<Bed> builder)
    {
        builder.ToTable("beds");

        builder.HasKey(b => b.Id).HasName("pk_beds");
        builder.Property(b => b.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(b => b.WardId).HasColumnName("ward_id").IsRequired();
        builder.Property(b => b.BedNumber).HasColumnName("bed_number").HasMaxLength(30).IsRequired();
        builder.Property(b => b.BedType).HasColumnName("bed_type").HasMaxLength(50).IsRequired();
        builder.Property(b => b.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(b => b.IsActive).HasColumnName("is_active").IsRequired().HasDefaultValue(true);

        builder.Property(b => b.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(b => b.CreatedBy).HasColumnName("created_by");
        builder.Property(b => b.UpdatedAt).HasColumnName("updated_at");
        builder.Property(b => b.UpdatedBy).HasColumnName("updated_by");
        builder.Property(b => b.IsDeleted).HasColumnName("is_deleted").IsRequired().HasDefaultValue(false);
        builder.Property(b => b.DeletedAt).HasColumnName("deleted_at");
        builder.Property(b => b.DeletedBy).HasColumnName("deleted_by");

        builder.Property<uint>("xmin").HasColumnName("xmin").IsRowVersion();

        builder.HasQueryFilter(b => !b.IsDeleted);

        // Bed number is only unique within its own ward, not globally.
        builder.HasIndex(b => new { b.WardId, b.BedNumber })
            .IsUnique()
            .HasDatabaseName("ux_beds_ward_bed_number")
            .HasFilter("is_deleted = false");
    }
}
