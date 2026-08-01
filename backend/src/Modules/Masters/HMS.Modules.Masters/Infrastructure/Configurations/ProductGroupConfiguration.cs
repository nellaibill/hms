using HMS.Modules.Masters.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HMS.Modules.Masters.Infrastructure.Configurations;

internal class ProductGroupConfiguration : IEntityTypeConfiguration<ProductGroup>
{
    public void Configure(EntityTypeBuilder<ProductGroup> builder)
    {
        builder.ToTable("product_groups");

        builder.HasKey(g => g.Id).HasName("pk_product_groups");
        builder.Property(g => g.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(g => g.GroupCode).HasColumnName("group_code").HasMaxLength(30).IsRequired();
        builder.Property(g => g.GroupName).HasColumnName("group_name").HasMaxLength(150).IsRequired();
        builder.Property(g => g.SubCategoryId).HasColumnName("sub_category_id").IsRequired();
        builder.Property(g => g.SortOrder).HasColumnName("sort_order").IsRequired().HasDefaultValue(0);
        builder.Property(g => g.Description).HasColumnName("description");
        builder.Property(g => g.IsActive).HasColumnName("is_active").IsRequired().HasDefaultValue(true);

        builder.Property(g => g.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(g => g.CreatedBy).HasColumnName("created_by");
        builder.Property(g => g.UpdatedAt).HasColumnName("updated_at");
        builder.Property(g => g.UpdatedBy).HasColumnName("updated_by");
        builder.Property(g => g.IsDeleted).HasColumnName("is_deleted").IsRequired().HasDefaultValue(false);
        builder.Property(g => g.DeletedAt).HasColumnName("deleted_at");
        builder.Property(g => g.DeletedBy).HasColumnName("deleted_by");

        builder.Property<uint>("xmin").HasColumnName("xmin").IsRowVersion();

        builder.HasQueryFilter(g => !g.IsDeleted);

        builder.HasIndex(g => g.GroupCode).IsUnique().HasDatabaseName("ux_product_groups_group_code").HasFilter("is_deleted = false");
        builder.HasIndex(g => g.SubCategoryId).HasDatabaseName("ix_product_groups_sub_category_id");

        builder.HasOne<ProductSubCategory>()
            .WithMany()
            .HasForeignKey(g => g.SubCategoryId)
            .HasConstraintName("fk_product_groups_sub_category_id")
            .OnDelete(DeleteBehavior.Restrict);
    }
}
