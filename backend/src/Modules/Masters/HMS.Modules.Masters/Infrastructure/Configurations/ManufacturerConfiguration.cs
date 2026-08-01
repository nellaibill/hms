using HMS.Modules.Masters.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HMS.Modules.Masters.Infrastructure.Configurations;

internal class ManufacturerConfiguration : IEntityTypeConfiguration<Manufacturer>
{
    public void Configure(EntityTypeBuilder<Manufacturer> builder)
    {
        builder.ToTable("manufacturers");

        builder.HasKey(m => m.Id).HasName("pk_manufacturers");
        builder.Property(m => m.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(m => m.ManufacturerCode).HasColumnName("manufacturer_code").HasMaxLength(30).IsRequired();
        builder.Property(m => m.ManufacturerName).HasColumnName("manufacturer_name").HasMaxLength(150).IsRequired();
        builder.Property(m => m.ContactPerson).HasColumnName("contact_person").HasMaxLength(150);
        builder.Property(m => m.Phone).HasColumnName("phone").HasMaxLength(30);
        builder.Property(m => m.Email).HasColumnName("email").HasMaxLength(150);
        builder.Property(m => m.Country).HasColumnName("country").HasMaxLength(100);
        builder.Property(m => m.IsActive).HasColumnName("is_active").IsRequired().HasDefaultValue(true);

        builder.Property(m => m.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(m => m.CreatedBy).HasColumnName("created_by");
        builder.Property(m => m.UpdatedAt).HasColumnName("updated_at");
        builder.Property(m => m.UpdatedBy).HasColumnName("updated_by");
        builder.Property(m => m.IsDeleted).HasColumnName("is_deleted").IsRequired().HasDefaultValue(false);
        builder.Property(m => m.DeletedAt).HasColumnName("deleted_at");
        builder.Property(m => m.DeletedBy).HasColumnName("deleted_by");

        builder.Property<uint>("xmin").HasColumnName("xmin").IsRowVersion();

        builder.HasQueryFilter(m => !m.IsDeleted);

        builder.HasIndex(m => m.ManufacturerCode).IsUnique().HasDatabaseName("ux_manufacturers_manufacturer_code").HasFilter("is_deleted = false");
    }
}
