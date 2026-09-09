using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HMS.Modules.DischargeSummary.Infrastructure.Configurations;

/// <summary>
/// Maps <see cref="Domain.DischargeMedication"/> to discharge_summary.discharge_medications.
/// Internal (not public) for the same CS0051 reason as DischargeSummaryConfiguration — the
/// entity is an internal domain type.
/// </summary>
internal class DischargeMedicationConfiguration : IEntityTypeConfiguration<Domain.DischargeMedication>
{
    public void Configure(EntityTypeBuilder<Domain.DischargeMedication> builder)
    {
        builder.ToTable("discharge_medications");

        builder.HasKey(x => x.Id).HasName("pk_discharge_medications");
        builder.Property(x => x.Id)
            .HasColumnName("id")
            .ValueGeneratedNever(); // Generated in the domain (DischargeMedication.Create), not by the database.

        builder.Property(x => x.DischargeSummaryId).HasColumnName("discharge_summary_id").IsRequired();
        builder.Property(x => x.SortOrder).HasColumnName("sort_order").IsRequired();

        builder.Property(x => x.DrugName).HasColumnName("drug_name").HasMaxLength(200).IsRequired();
        builder.Property(x => x.Dose).HasColumnName("dose").HasMaxLength(100).IsRequired();
        builder.Property(x => x.Route).HasColumnName("route").HasMaxLength(100).IsRequired();

        builder.Property(x => x.MorningQty).HasColumnName("morning_qty").HasColumnType("numeric(6,2)").IsRequired();
        builder.Property(x => x.NoonQty).HasColumnName("noon_qty").HasColumnType("numeric(6,2)").IsRequired();
        builder.Property(x => x.EveningQty).HasColumnName("evening_qty").HasColumnType("numeric(6,2)").IsRequired();
        builder.Property(x => x.NightQty).HasColumnName("night_qty").HasColumnType("numeric(6,2)").IsRequired();
        builder.Property(x => x.DurationDays).HasColumnName("duration_days").IsRequired();

        builder.Property(x => x.FoodInstruction).HasColumnName("food_instruction").HasConversion<string>().HasMaxLength(20).IsRequired();

        // Standard audit columns (docs/DatabaseArchitecture.md §5).
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(x => x.CreatedBy).HasColumnName("created_by");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        builder.Property(x => x.UpdatedBy).HasColumnName("updated_by");
        builder.Property(x => x.IsDeleted).HasColumnName("is_deleted").IsRequired().HasDefaultValue(false);
        builder.Property(x => x.DeletedAt).HasColumnName("deleted_at");
        builder.Property(x => x.DeletedBy).HasColumnName("deleted_by");

        builder.Property<uint>("xmin")
            .HasColumnName("xmin")
            .IsRowVersion();

        builder.HasQueryFilter(x => !x.IsDeleted);

        builder.HasIndex(x => x.DischargeSummaryId).HasDatabaseName("ix_discharge_medications_discharge_summary_id");
    }
}
