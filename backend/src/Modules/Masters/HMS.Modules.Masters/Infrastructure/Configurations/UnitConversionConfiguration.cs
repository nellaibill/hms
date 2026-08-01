using HMS.Modules.Masters.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HMS.Modules.Masters.Infrastructure.Configurations;

internal class UnitConversionConfiguration : IEntityTypeConfiguration<UnitConversion>
{
    public void Configure(EntityTypeBuilder<UnitConversion> builder)
    {
        builder.ToTable("unit_conversions");

        builder.HasKey(u => u.Id).HasName("pk_unit_conversions");
        builder.Property(u => u.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(u => u.FromUomId).HasColumnName("from_uom_id").IsRequired();
        builder.Property(u => u.ToUomId).HasColumnName("to_uom_id").IsRequired();
        builder.Property(u => u.ConversionFactor).HasColumnName("conversion_factor").HasColumnType("numeric(18,6)").IsRequired();
        builder.Property(u => u.IsActive).HasColumnName("is_active").IsRequired().HasDefaultValue(true);

        builder.Property(u => u.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(u => u.CreatedBy).HasColumnName("created_by");
        builder.Property(u => u.UpdatedAt).HasColumnName("updated_at");
        builder.Property(u => u.UpdatedBy).HasColumnName("updated_by");
        builder.Property(u => u.IsDeleted).HasColumnName("is_deleted").IsRequired().HasDefaultValue(false);
        builder.Property(u => u.DeletedAt).HasColumnName("deleted_at");
        builder.Property(u => u.DeletedBy).HasColumnName("deleted_by");

        builder.Property<uint>("xmin").HasColumnName("xmin").IsRowVersion();

        builder.HasQueryFilter(u => !u.IsDeleted);

        builder.HasIndex(u => new { u.FromUomId, u.ToUomId }).IsUnique().HasDatabaseName("ux_unit_conversions_from_to").HasFilter("is_deleted = false");
        builder.HasIndex(u => u.ToUomId).HasDatabaseName("ix_unit_conversions_to_uom_id");

        builder.HasOne<UnitOfMeasure>()
            .WithMany()
            .HasForeignKey(u => u.FromUomId)
            .HasConstraintName("fk_unit_conversions_from_uom_id")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<UnitOfMeasure>()
            .WithMany()
            .HasForeignKey(u => u.ToUomId)
            .HasConstraintName("fk_unit_conversions_to_uom_id")
            .OnDelete(DeleteBehavior.Restrict);
    }
}
