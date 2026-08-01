using HMS.Modules.Masters.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HMS.Modules.Masters.Infrastructure.Configurations;

internal class TaxConfiguration : IEntityTypeConfiguration<Tax>
{
    public void Configure(EntityTypeBuilder<Tax> builder)
    {
        builder.ToTable("taxes");

        builder.HasKey(t => t.Id).HasName("pk_taxes");
        builder.Property(t => t.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(t => t.TaxCode).HasColumnName("tax_code").HasMaxLength(20).IsRequired();
        builder.Property(t => t.TaxName).HasColumnName("tax_name").HasMaxLength(150).IsRequired();
        builder.Property(t => t.TaxType).HasColumnName("tax_type").HasMaxLength(20);
        builder.Property(t => t.RatePercent).HasColumnName("rate_percent").HasColumnType("numeric(6,3)").IsRequired();
        builder.Property(t => t.IsInclusive).HasColumnName("is_inclusive").IsRequired().HasDefaultValue(false);
        builder.Property(t => t.IsActive).HasColumnName("is_active").IsRequired().HasDefaultValue(true);

        builder.Property(t => t.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(t => t.CreatedBy).HasColumnName("created_by");
        builder.Property(t => t.UpdatedAt).HasColumnName("updated_at");
        builder.Property(t => t.UpdatedBy).HasColumnName("updated_by");
        builder.Property(t => t.IsDeleted).HasColumnName("is_deleted").IsRequired().HasDefaultValue(false);
        builder.Property(t => t.DeletedAt).HasColumnName("deleted_at");
        builder.Property(t => t.DeletedBy).HasColumnName("deleted_by");

        builder.Property<uint>("xmin").HasColumnName("xmin").IsRowVersion();

        builder.HasQueryFilter(t => !t.IsDeleted);

        builder.HasIndex(t => t.TaxCode).IsUnique().HasDatabaseName("ux_taxes_tax_code").HasFilter("is_deleted = false");
    }
}
