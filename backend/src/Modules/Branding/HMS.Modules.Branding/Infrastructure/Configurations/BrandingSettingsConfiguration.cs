using HMS.Modules.Branding.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HMS.Modules.Branding.Infrastructure.Configurations;

/// <summary>
/// Maps <see cref="BrandingSettings"/> to branding.branding_settings, following the
/// naming/PK/audit-column/soft-delete standards in docs/DatabaseArchitecture.md.
/// Internal (not public): mirrors HMS.Modules.Identity.Infrastructure.Configurations.UserConfiguration.
/// </summary>
internal class BrandingSettingsConfiguration : IEntityTypeConfiguration<BrandingSettings>
{
    public void Configure(EntityTypeBuilder<BrandingSettings> builder)
    {
        builder.ToTable("branding_settings");

        builder.HasKey(b => b.Id).HasName("pk_branding_settings");
        builder.Property(b => b.Id)
            .HasColumnName("id")
            .ValueGeneratedNever(); // Fixed singleton id assigned in the domain (BrandingSettings.SingletonId).

        builder.Property(b => b.HospitalName).HasColumnName("hospital_name").HasMaxLength(200).IsRequired();
        builder.Property(b => b.AppTitle).HasColumnName("app_title").HasMaxLength(200).IsRequired();
        builder.Property(b => b.LogoPath).HasColumnName("logo_path").HasMaxLength(500);
        builder.Property(b => b.FontFamily).HasColumnName("font_family").HasMaxLength(50).IsRequired();
        builder.Property(b => b.FontSizeScale).HasColumnName("font_size_scale").HasMaxLength(10).IsRequired();

        // Flat CSS-var-name -> value maps stored as jsonb rather than one column per token —
        // the app already has ~30 tokens and will likely grow more; this avoids a migration
        // every time a new token is introduced (see the Theme & Branding architecture notes).
        builder.Property(b => b.TokensLightJson).HasColumnName("tokens_light").HasColumnType("jsonb").IsRequired();
        builder.Property(b => b.TokensDarkJson).HasColumnName("tokens_dark").HasColumnType("jsonb").IsRequired();

        // Standard audit columns (docs/DatabaseArchitecture.md §5).
        builder.Property(b => b.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(b => b.CreatedBy).HasColumnName("created_by");
        builder.Property(b => b.UpdatedAt).HasColumnName("updated_at");
        builder.Property(b => b.UpdatedBy).HasColumnName("updated_by");
        builder.Property(b => b.IsDeleted).HasColumnName("is_deleted").IsRequired().HasDefaultValue(false);
        builder.Property(b => b.DeletedAt).HasColumnName("deleted_at");
        builder.Property(b => b.DeletedBy).HasColumnName("deleted_by");

        // Optimistic concurrency via Postgres's own system column (docs/DatabaseArchitecture.md §5).
        builder.Property<uint>("xmin")
            .HasColumnName("xmin")
            .IsRowVersion();

        builder.HasQueryFilter(b => !b.IsDeleted);
    }
}
