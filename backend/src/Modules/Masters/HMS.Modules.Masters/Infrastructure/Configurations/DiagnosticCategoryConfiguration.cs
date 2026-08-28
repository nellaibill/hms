using HMS.Modules.Masters.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HMS.Modules.Masters.Infrastructure.Configurations;

internal class DiagnosticCategoryConfiguration : IEntityTypeConfiguration<DiagnosticCategory>
{
    public void Configure(EntityTypeBuilder<DiagnosticCategory> builder)
    {
        builder.ToTable("diagnostic_categories");

        builder.HasKey(c => c.Id).HasName("pk_diagnostic_categories");
        builder.Property(c => c.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(c => c.Code).HasColumnName("code").HasMaxLength(20).IsRequired();
        builder.Property(c => c.Name).HasColumnName("name").HasMaxLength(150).IsRequired();
        builder.Property(c => c.Description).HasColumnName("description").HasMaxLength(500);
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

        builder.HasIndex(c => c.Code).IsUnique().HasDatabaseName("ux_diagnostic_categories_code").HasFilter("is_deleted = false");
    }
}
