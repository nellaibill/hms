using HMS.Modules.Masters.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HMS.Modules.Masters.Infrastructure.Configurations;

internal class ProductCategoryConfiguration : IEntityTypeConfiguration<ProductCategory>
{
    public void Configure(EntityTypeBuilder<ProductCategory> builder)
    {
        builder.ToTable("product_categories");

        builder.HasKey(c => c.Id).HasName("pk_product_categories");
        builder.Property(c => c.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(c => c.CategoryCode).HasColumnName("category_code").HasMaxLength(20).IsRequired();
        builder.Property(c => c.CategoryName).HasColumnName("category_name").HasMaxLength(150).IsRequired();
        builder.Property(c => c.ParentId).HasColumnName("parent_id");
        builder.Property(c => c.SortOrder).HasColumnName("sort_order").IsRequired().HasDefaultValue(0);
        builder.Property(c => c.Description).HasColumnName("description");
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

        builder.HasIndex(c => c.CategoryCode).IsUnique().HasDatabaseName("ux_product_categories_category_code").HasFilter("is_deleted = false");
        builder.HasIndex(c => c.ParentId).HasDatabaseName("ix_product_categories_parent_id");

        // Self-referencing, FK-only (no navigation property) — see the Domain type's XML comment.
        builder.HasOne<ProductCategory>()
            .WithMany()
            .HasForeignKey(c => c.ParentId)
            .HasConstraintName("fk_product_categories_parent_id")
            .OnDelete(DeleteBehavior.Restrict);
    }
}
