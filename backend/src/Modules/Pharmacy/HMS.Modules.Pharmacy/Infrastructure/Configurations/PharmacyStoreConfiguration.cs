using HMS.Modules.Pharmacy.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HMS.Modules.Pharmacy.Infrastructure.Configurations;

internal class PharmacyStoreConfiguration : IEntityTypeConfiguration<PharmacyStore>
{
    public void Configure(EntityTypeBuilder<PharmacyStore> builder)
    {
        builder.ToTable("pharmacy_stores");

        builder.HasKey(s => s.Id).HasName("pk_pharmacy_stores");
        builder.Property(s => s.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(s => s.Code).HasColumnName("code").HasMaxLength(30).IsRequired();
        builder.Property(s => s.Name).HasColumnName("name").HasMaxLength(100).IsRequired();
        builder.Property(s => s.StoreType).HasColumnName("store_type").HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(s => s.IsDefault).HasColumnName("is_default").IsRequired().HasDefaultValue(false);
        builder.Property(s => s.IsActive).HasColumnName("is_active").IsRequired().HasDefaultValue(true);

        builder.Property(s => s.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(s => s.CreatedBy).HasColumnName("created_by");
        builder.Property(s => s.UpdatedAt).HasColumnName("updated_at");
        builder.Property(s => s.UpdatedBy).HasColumnName("updated_by");
        builder.Property(s => s.IsDeleted).HasColumnName("is_deleted").IsRequired().HasDefaultValue(false);
        builder.Property(s => s.DeletedAt).HasColumnName("deleted_at");
        builder.Property(s => s.DeletedBy).HasColumnName("deleted_by");

        builder.Property<uint>("xmin").HasColumnName("xmin").IsRowVersion();

        builder.HasQueryFilter(s => !s.IsDeleted);

        builder.HasIndex(s => s.Code).IsUnique().HasDatabaseName("ux_pharmacy_stores_code").HasFilter("is_deleted = false");

        // "Exactly one default" is kept by StoreService, which moves the flag in one save. It
        // is deliberately not a unique index on is_default: EF orders the two UPDATEs of a
        // swap by the index's values and can't see that a filter would exempt the false rows,
        // so it reads a swap as a circular dependency.
    }
}
