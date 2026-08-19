using HMS.Modules.Platform.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HMS.Modules.Platform.Infrastructure.Configurations;

internal class ProvisioningAlertConfiguration : IEntityTypeConfiguration<ProvisioningAlert>
{
    public void Configure(EntityTypeBuilder<ProvisioningAlert> builder)
    {
        builder.ToTable("provisioning_alerts");

        builder.HasKey(a => a.Id).HasName("pk_provisioning_alerts");
        builder.Property(a => a.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(a => a.DatabaseName).HasColumnName("database_name").HasMaxLength(63).IsRequired();
        builder.Property(a => a.Message).HasColumnName("message").IsRequired();
        builder.Property(a => a.CreatedAt).HasColumnName("created_at").IsRequired();
    }
}
