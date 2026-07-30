using HMS.Modules.Products.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HMS.Modules.Products.Infrastructure.Configurations;

internal class ProductAttributeConfiguration : IEntityTypeConfiguration<ProductAttribute>
{
    public void Configure(EntityTypeBuilder<ProductAttribute> builder)
    {
        builder.ToTable("product_attributes");

        builder.HasKey(a => a.Id).HasName("pk_product_attributes");
        builder.Property(a => a.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(a => a.AttributeCode).HasColumnName("attribute_code").HasMaxLength(50).IsRequired();
        builder.Property(a => a.AttributeName).HasColumnName("attribute_name").HasMaxLength(150).IsRequired();
        builder.Property(a => a.DataType).HasColumnName("data_type").HasMaxLength(20).IsRequired();
        builder.Property(a => a.IsMandatory).HasColumnName("is_mandatory").IsRequired().HasDefaultValue(false);
        builder.Property(a => a.IsActive).HasColumnName("is_active").IsRequired().HasDefaultValue(true);

        builder.Property(a => a.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(a => a.CreatedBy).HasColumnName("created_by");
        builder.Property(a => a.UpdatedAt).HasColumnName("updated_at");
        builder.Property(a => a.UpdatedBy).HasColumnName("updated_by");
        builder.Property(a => a.IsDeleted).HasColumnName("is_deleted").IsRequired().HasDefaultValue(false);
        builder.Property(a => a.DeletedAt).HasColumnName("deleted_at");
        builder.Property(a => a.DeletedBy).HasColumnName("deleted_by");

        builder.Property<uint>("xmin").HasColumnName("xmin").IsRowVersion();

        builder.HasQueryFilter(a => !a.IsDeleted);

        builder.HasIndex(a => a.AttributeCode).IsUnique().HasDatabaseName("ux_product_attributes_attribute_code").HasFilter("is_deleted = false");
    }
}
