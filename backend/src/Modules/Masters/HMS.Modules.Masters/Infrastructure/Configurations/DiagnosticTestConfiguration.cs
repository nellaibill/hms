using HMS.Modules.Masters.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HMS.Modules.Masters.Infrastructure.Configurations;

internal class DiagnosticTestConfiguration : IEntityTypeConfiguration<DiagnosticTest>
{
    public void Configure(EntityTypeBuilder<DiagnosticTest> builder)
    {
        builder.ToTable("diagnostic_tests");

        builder.HasKey(d => d.Id).HasName("pk_diagnostic_tests");
        builder.Property(d => d.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(d => d.Name).HasColumnName("name").HasMaxLength(150).IsRequired();
        builder.Property(d => d.ServiceType).HasColumnName("service_type").HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(d => d.Category).HasColumnName("category").HasMaxLength(100);
        builder.Property(d => d.Price).HasColumnName("price").HasColumnType("numeric(10,2)").IsRequired();
        builder.Property(d => d.IsOutsourced).HasColumnName("is_outsourced").IsRequired().HasDefaultValue(false);
        builder.Property(d => d.ReferenceLab).HasColumnName("reference_lab").HasMaxLength(100);
        builder.Property(d => d.IsActive).HasColumnName("is_active").IsRequired().HasDefaultValue(true);

        builder.Property(d => d.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(d => d.CreatedBy).HasColumnName("created_by");
        builder.Property(d => d.UpdatedAt).HasColumnName("updated_at");
        builder.Property(d => d.UpdatedBy).HasColumnName("updated_by");
        builder.Property(d => d.IsDeleted).HasColumnName("is_deleted").IsRequired().HasDefaultValue(false);
        builder.Property(d => d.DeletedAt).HasColumnName("deleted_at");
        builder.Property(d => d.DeletedBy).HasColumnName("deleted_by");

        builder.Property<uint>("xmin").HasColumnName("xmin").IsRowVersion();

        builder.HasQueryFilter(d => !d.IsDeleted);

        // A test name can legitimately repeat across service types and in-house/outsourced
        // variants (e.g. the same test priced differently in-house vs. via a reference lab).
        builder.HasIndex(d => new { d.Name, d.ServiceType, d.IsOutsourced }).IsUnique().HasDatabaseName("ux_diagnostic_tests_name_type_outsourced").HasFilter("is_deleted = false");
    }
}
