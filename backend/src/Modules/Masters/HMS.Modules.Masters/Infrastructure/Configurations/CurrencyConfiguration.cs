using HMS.Modules.Masters.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HMS.Modules.Masters.Infrastructure.Configurations;

/// <summary>
/// Maps <see cref="Currency"/> to masters.currencies, following the naming/PK/audit-column/
/// soft-delete standards in docs/DatabaseArchitecture.md.
/// </summary>
internal class CurrencyConfiguration : IEntityTypeConfiguration<Currency>
{
    public void Configure(EntityTypeBuilder<Currency> builder)
    {
        builder.ToTable("currencies");

        builder.HasKey(c => c.Id).HasName("pk_currencies");
        builder.Property(c => c.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(c => c.CurrencyCode).HasColumnName("currency_code").HasMaxLength(3).IsRequired();
        builder.Property(c => c.CurrencyName).HasColumnName("currency_name").HasMaxLength(100).IsRequired();
        builder.Property(c => c.Symbol).HasColumnName("symbol").HasMaxLength(10).IsRequired();
        builder.Property(c => c.DecimalPlaces).HasColumnName("decimal_places").IsRequired().HasDefaultValue(2);
        builder.Property(c => c.IsActive).HasColumnName("is_active").IsRequired().HasDefaultValue(true);

        // Standard audit columns (docs/DatabaseArchitecture.md §5).
        builder.Property(c => c.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(c => c.CreatedBy).HasColumnName("created_by");
        builder.Property(c => c.UpdatedAt).HasColumnName("updated_at");
        builder.Property(c => c.UpdatedBy).HasColumnName("updated_by");
        builder.Property(c => c.IsDeleted).HasColumnName("is_deleted").IsRequired().HasDefaultValue(false);
        builder.Property(c => c.DeletedAt).HasColumnName("deleted_at");
        builder.Property(c => c.DeletedBy).HasColumnName("deleted_by");

        builder.Property<uint>("xmin").HasColumnName("xmin").IsRowVersion();

        builder.HasQueryFilter(c => !c.IsDeleted);

        builder.HasIndex(c => c.CurrencyCode).IsUnique().HasDatabaseName("ux_currencies_currency_code").HasFilter("is_deleted = false");
    }
}
