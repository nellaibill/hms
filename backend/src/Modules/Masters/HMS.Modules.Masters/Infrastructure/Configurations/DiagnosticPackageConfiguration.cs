using HMS.Modules.Masters.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HMS.Modules.Masters.Infrastructure.Configurations;

/// <summary>Maps <see cref="DiagnosticPackage"/> and its child <see cref="DiagnosticPackageItem"/>
/// — mirrors PatientVisitConfiguration's aggregate-root + child shape, including the same
/// HasMany(...).OnDelete(DeleteBehavior.Cascade) for the child table.</summary>
internal class DiagnosticPackageConfiguration : IEntityTypeConfiguration<DiagnosticPackage>
{
    public void Configure(EntityTypeBuilder<DiagnosticPackage> builder)
    {
        builder.ToTable("diagnostic_packages");

        builder.HasKey(p => p.Id).HasName("pk_diagnostic_packages");
        builder.Property(p => p.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(p => p.Code).HasColumnName("code").HasMaxLength(20).IsRequired();
        builder.Property(p => p.Name).HasColumnName("name").HasMaxLength(150).IsRequired();
        builder.Property(p => p.Description).HasColumnName("description").HasMaxLength(500);
        builder.Property(p => p.TotalPrice).HasColumnName("total_price").HasColumnType("numeric(10,2)").IsRequired();
        builder.Property(p => p.IsActive).HasColumnName("is_active").IsRequired().HasDefaultValue(true);

        builder.Property(p => p.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(p => p.CreatedBy).HasColumnName("created_by");
        builder.Property(p => p.UpdatedAt).HasColumnName("updated_at");
        builder.Property(p => p.UpdatedBy).HasColumnName("updated_by");
        builder.Property(p => p.IsDeleted).HasColumnName("is_deleted").IsRequired().HasDefaultValue(false);
        builder.Property(p => p.DeletedAt).HasColumnName("deleted_at");
        builder.Property(p => p.DeletedBy).HasColumnName("deleted_by");

        builder.Property<uint>("xmin").HasColumnName("xmin").IsRowVersion();

        builder.HasQueryFilter(p => !p.IsDeleted);

        builder.HasIndex(p => p.Code).IsUnique().HasDatabaseName("ux_diagnostic_packages_code").HasFilter("is_deleted = false");

        builder.HasMany(p => p.Items)
            .WithOne()
            .HasForeignKey(i => i.PackageId)
            .HasConstraintName("fk_diagnostic_package_items_package_id")
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(p => p.Items).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

internal class DiagnosticPackageItemConfiguration : IEntityTypeConfiguration<DiagnosticPackageItem>
{
    public void Configure(EntityTypeBuilder<DiagnosticPackageItem> builder)
    {
        builder.ToTable("diagnostic_package_items");

        builder.HasKey(i => i.Id).HasName("pk_diagnostic_package_items");
        builder.Property(i => i.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(i => i.PackageId).HasColumnName("package_id").IsRequired();
        // App-level reference into DiagnosticService — no DB FK, validated in
        // DiagnosticPackageService (see Domain/DiagnosticPackageItem.cs).
        builder.Property(i => i.ServiceId).HasColumnName("service_id").IsRequired();

        builder.HasIndex(i => i.PackageId).HasDatabaseName("ix_diagnostic_package_items_package_id");
        builder.HasIndex(i => i.ServiceId).HasDatabaseName("ix_diagnostic_package_items_service_id");
    }
}
