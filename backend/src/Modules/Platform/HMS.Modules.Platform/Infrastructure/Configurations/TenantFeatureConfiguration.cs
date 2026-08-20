using HMS.Modules.Platform.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HMS.Modules.Platform.Infrastructure.Configurations;

internal class TenantFeatureConfiguration : IEntityTypeConfiguration<TenantFeature>
{
    public void Configure(EntityTypeBuilder<TenantFeature> builder)
    {
        builder.ToTable("tenant_features");

        builder.HasKey(f => f.Id).HasName("pk_tenant_features");
        builder.Property(f => f.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(f => f.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(f => f.FeatureKey).HasColumnName("feature_key").HasMaxLength(50).IsRequired();
        builder.Property(f => f.IsEnabled).HasColumnName("is_enabled").IsRequired();

        builder.Property(f => f.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(f => f.CreatedBy).HasColumnName("created_by");
        builder.Property(f => f.UpdatedAt).HasColumnName("updated_at");
        builder.Property(f => f.UpdatedBy).HasColumnName("updated_by");
        builder.Property(f => f.IsDeleted).HasColumnName("is_deleted").IsRequired().HasDefaultValue(false);
        builder.Property(f => f.DeletedAt).HasColumnName("deleted_at");
        builder.Property(f => f.DeletedBy).HasColumnName("deleted_by");

        builder.HasQueryFilter(f => !f.IsDeleted);

        // One row per (tenant, feature) — also the natural lookup index for "all features of
        // this tenant" queries (TenantId is the leading column).
        builder.HasIndex(f => new { f.TenantId, f.FeatureKey })
            .IsUnique()
            .HasDatabaseName("ux_tenant_features_tenant_id_feature_key")
            .HasFilter("is_deleted = false");
    }
}
