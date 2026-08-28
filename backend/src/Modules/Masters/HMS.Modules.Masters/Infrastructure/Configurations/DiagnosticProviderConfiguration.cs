using HMS.Modules.Masters.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HMS.Modules.Masters.Infrastructure.Configurations;

internal class DiagnosticProviderConfiguration : IEntityTypeConfiguration<DiagnosticProvider>
{
    public void Configure(EntityTypeBuilder<DiagnosticProvider> builder)
    {
        builder.ToTable("diagnostic_providers");

        builder.HasKey(p => p.Id).HasName("pk_diagnostic_providers");
        builder.Property(p => p.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(p => p.Code).HasColumnName("code").HasMaxLength(20).IsRequired();
        builder.Property(p => p.Name).HasColumnName("name").HasMaxLength(150).IsRequired();
        builder.Property(p => p.ContactDetails).HasColumnName("contact_details").HasMaxLength(500);
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

        builder.HasIndex(p => p.Code).IsUnique().HasDatabaseName("ux_diagnostic_providers_code").HasFilter("is_deleted = false");
    }
}
