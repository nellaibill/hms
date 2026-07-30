using HMS.Modules.Masters.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HMS.Modules.Masters.Infrastructure.Configurations;

internal class ProductSubCategoryConfiguration : IEntityTypeConfiguration<ProductSubCategory>
{
    public void Configure(EntityTypeBuilder<ProductSubCategory> builder)
    {
        builder.ToTable("product_sub_categories");

        builder.HasKey(s => s.Id).HasName("pk_product_sub_categories");
        builder.Property(s => s.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(s => s.SubCategoryCode).HasColumnName("sub_category_code").HasMaxLength(30).IsRequired();
        builder.Property(s => s.SubCategoryName).HasColumnName("sub_category_name").HasMaxLength(150).IsRequired();
        builder.Property(s => s.CategoryId).HasColumnName("category_id").IsRequired();
        builder.Property(s => s.SortOrder).HasColumnName("sort_order").IsRequired().HasDefaultValue(0);
        builder.Property(s => s.Description).HasColumnName("description");
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

        builder.HasIndex(s => s.SubCategoryCode).IsUnique().HasDatabaseName("ux_product_sub_categories_sub_category_code").HasFilter("is_deleted = false");
        builder.HasIndex(s => s.CategoryId).HasDatabaseName("ix_product_sub_categories_category_id");

        builder.HasOne<ProductCategory>()
            .WithMany()
            .HasForeignKey(s => s.CategoryId)
            .HasConstraintName("fk_product_sub_categories_category_id")
            .OnDelete(DeleteBehavior.Restrict);
    }
}
