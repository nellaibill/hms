using HMS.Modules.Masters.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HMS.Modules.Masters.Infrastructure.Configurations;

internal class DiagnosticServiceConfiguration : IEntityTypeConfiguration<DiagnosticService>
{
    public void Configure(EntityTypeBuilder<DiagnosticService> builder)
    {
        builder.ToTable("diagnostic_services");

        builder.HasKey(d => d.Id).HasName("pk_diagnostic_services");
        builder.Property(d => d.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(d => d.Code).HasColumnName("code").HasMaxLength(20).IsRequired();
        builder.Property(d => d.Name).HasColumnName("name").HasMaxLength(150).IsRequired();
        // App-level reference into DiagnosticCategory — no DB FK, validated in
        // DiagnosticServiceService (see Domain/DiagnosticService.cs).
        builder.Property(d => d.CategoryId).HasColumnName("category_id").IsRequired();
        builder.Property(d => d.ServiceType).HasColumnName("service_type").HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(d => d.IsOutsourced).HasColumnName("is_outsourced").IsRequired().HasDefaultValue(false);
        // App-level reference into DiagnosticProvider — no DB FK, same convention as CategoryId.
        builder.Property(d => d.ProviderId).HasColumnName("provider_id");
        builder.Property(d => d.Price).HasColumnName("price").HasColumnType("numeric(10,2)").IsRequired();
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

        builder.HasIndex(d => d.Code).IsUnique().HasDatabaseName("ux_diagnostic_services_code").HasFilter("is_deleted = false");
        builder.HasIndex(d => d.CategoryId).HasDatabaseName("ix_diagnostic_services_category_id");
    }
}
