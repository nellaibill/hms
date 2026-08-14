using HMS.Modules.Platform.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HMS.Modules.Platform.Infrastructure.Configurations;

internal class TenantConfiguration : IEntityTypeConfiguration<Tenant>
{
    public void Configure(EntityTypeBuilder<Tenant> builder)
    {
        builder.ToTable("tenants");

        builder.HasKey(t => t.Id).HasName("pk_tenants");
        builder.Property(t => t.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(t => t.HospitalName).HasColumnName("hospital_name").HasMaxLength(200).IsRequired();
        builder.Property(t => t.HospitalCode).HasColumnName("hospital_code").HasMaxLength(100).IsRequired();
        builder.Property(t => t.MobileNumber).HasColumnName("mobile_number").HasMaxLength(20).IsRequired();
        builder.Property(t => t.Email).HasColumnName("email").HasMaxLength(200).IsRequired();
        builder.Property(t => t.Address).HasColumnName("address").HasMaxLength(500).IsRequired();
        builder.Property(t => t.City).HasColumnName("city").HasMaxLength(100).IsRequired();
        builder.Property(t => t.State).HasColumnName("state").HasMaxLength(100).IsRequired();
        builder.Property(t => t.Pincode).HasColumnName("pincode").HasMaxLength(20).IsRequired();
        builder.Property(t => t.DatabaseName).HasColumnName("database_name").HasMaxLength(63).IsRequired();
        builder.Property(t => t.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(20).IsRequired();

        builder.Property(t => t.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(t => t.CreatedBy).HasColumnName("created_by");
        builder.Property(t => t.UpdatedAt).HasColumnName("updated_at");
        builder.Property(t => t.UpdatedBy).HasColumnName("updated_by");
        builder.Property(t => t.IsDeleted).HasColumnName("is_deleted").IsRequired().HasDefaultValue(false);
        builder.Property(t => t.DeletedAt).HasColumnName("deleted_at");
        builder.Property(t => t.DeletedBy).HasColumnName("deleted_by");

        builder.Property<uint>("xmin").HasColumnName("xmin").IsRowVersion();

        builder.HasQueryFilter(t => !t.IsDeleted);

        // Postgres database names are capped at 63 bytes, matching DatabaseName's max length.
        builder.HasIndex(t => t.HospitalCode).IsUnique().HasDatabaseName("ux_tenants_hospital_code").HasFilter("is_deleted = false");
        builder.HasIndex(t => t.DatabaseName).IsUnique().HasDatabaseName("ux_tenants_database_name").HasFilter("is_deleted = false");
        builder.HasIndex(t => t.Email).IsUnique().HasDatabaseName("ux_tenants_email").HasFilter("is_deleted = false");
    }
}
