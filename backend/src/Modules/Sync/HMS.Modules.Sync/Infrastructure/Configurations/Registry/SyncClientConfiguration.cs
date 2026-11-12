using HMS.Modules.Sync.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HMS.Modules.Sync.Infrastructure.Configurations.Registry;

internal sealed class SyncClientConfiguration : IEntityTypeConfiguration<SyncClient>
{
    public void Configure(EntityTypeBuilder<SyncClient> builder)
    {
        builder.ToTable("sync_clients");

        builder.HasKey(c => c.Id).HasName("pk_sync_clients");
        builder.Property(c => c.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(c => c.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(c => c.HospitalCode).HasColumnName("hospital_code").HasMaxLength(50).IsRequired();
        builder.Property(c => c.KeyPrefix).HasColumnName("key_prefix").HasMaxLength(20).IsRequired();
        builder.Property(c => c.KeyHash).HasColumnName("key_hash").HasMaxLength(64).IsRequired();
        builder.Property(c => c.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(c => c.CreatedBy).HasColumnName("created_by").HasMaxLength(100);
        builder.Property(c => c.RevokedAt).HasColumnName("revoked_at");
        builder.Property(c => c.LastUsedAt).HasColumnName("last_used_at");

        builder.HasIndex(c => c.KeyHash).IsUnique().HasDatabaseName("ux_sync_clients_key_hash");
        builder.HasIndex(c => c.TenantId).HasDatabaseName("ix_sync_clients_tenant_id");
    }
}

internal sealed class TenantSyncSettingsConfiguration : IEntityTypeConfiguration<TenantSyncSettings>
{
    public void Configure(EntityTypeBuilder<TenantSyncSettings> builder)
    {
        builder.ToTable("tenant_settings");

        builder.HasKey(s => s.TenantId).HasName("pk_tenant_settings");
        builder.Property(s => s.TenantId).HasColumnName("tenant_id").ValueGeneratedNever();
        builder.Property(s => s.HospitalCode).HasColumnName("hospital_code").HasMaxLength(50).IsRequired();
        builder.Property(s => s.PatientsOwnedByEdge).HasColumnName("patients_owned_by_edge").IsRequired().HasDefaultValue(false);
        builder.Property(s => s.UpdatedAt).HasColumnName("updated_at").IsRequired();
        builder.Property(s => s.UpdatedBy).HasColumnName("updated_by").HasMaxLength(100);
    }
}
