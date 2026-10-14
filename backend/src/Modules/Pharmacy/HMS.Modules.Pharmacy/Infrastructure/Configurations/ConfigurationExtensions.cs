using System.Text.RegularExpressions;
using HMS.Shared.Kernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HMS.Modules.Pharmacy.Infrastructure.Configurations;

/// <summary>
/// What every document table in this module needs, written once: the primary key, the
/// standard audit columns, the xmin concurrency token, the soft-delete filter, and
/// snake_case column names for everything else. A configuration calls
/// <see cref="MapDocument{T}"/> and then states only what is particular to its table.
/// </summary>
internal static partial class ConfigurationExtensions
{
    public static EntityTypeBuilder<T> MapDocument<T>(this EntityTypeBuilder<T> builder, string tableName)
        where T : Entity
    {
        builder.ToTable(tableName);
        builder.HasKey(e => e.Id).HasName($"pk_{tableName}");
        builder.Property(e => e.Id).ValueGeneratedNever();
        builder.Property(e => e.IsDeleted).HasDefaultValue(false);

        // UseXminAsConcurrencyToken() doesn't exist in the pinned Npgsql EF Core provider
        // version — manual mapping instead (docs/DeveloperHandbook.md §20).
        builder.Property<uint>("xmin").HasColumnName("xmin").IsRowVersion();
        builder.HasQueryFilter(e => !e.IsDeleted);

        foreach (var property in builder.Metadata.GetProperties())
        {
            if (property.Name == "xmin")
            {
                continue;
            }

            builder.Property(property.Name).HasColumnName(ToSnakeCase(property.Name));

            if (property.ClrType == typeof(decimal) || property.ClrType == typeof(decimal?))
            {
                builder.Property(property.Name).HasPrecision(18, 4);
            }
            else if ((Nullable.GetUnderlyingType(property.ClrType) ?? property.ClrType).IsEnum)
            {
                builder.Property(property.Name).HasConversion<string>().HasMaxLength(30);
            }
        }

        return builder;
    }

    public static string ToSnakeCase(string name) => SnakeCaseBoundary().Replace(name, "$1_$2").ToLowerInvariant();

    [GeneratedRegex("([a-z0-9])([A-Z])")]
    private static partial Regex SnakeCaseBoundary();
}
