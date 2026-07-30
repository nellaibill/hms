using HMS.Modules.Masters.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HMS.Modules.Masters.Infrastructure.Configurations;

internal class StockAdjustmentReasonConfiguration : IEntityTypeConfiguration<StockAdjustmentReason>
{
    public void Configure(EntityTypeBuilder<StockAdjustmentReason> builder)
    {
        builder.ToTable("stock_adjustment_reasons");

        builder.HasKey(s => s.Id).HasName("pk_stock_adjustment_reasons");
        builder.Property(s => s.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(s => s.ReasonCode).HasColumnName("reason_code").HasMaxLength(30).IsRequired();
        builder.Property(s => s.ReasonName).HasColumnName("reason_name").HasMaxLength(150).IsRequired();
        builder.Property(s => s.AffectsValuation).HasColumnName("affects_valuation").IsRequired().HasDefaultValue(false);
        builder.Property(s => s.Description).HasColumnName("description");
        builder.Property(s => s.IsActive).HasColumnName("is_active").IsRequired().HasDefaultValue(true);

        builder.Property(s => s.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(s => s.CreatedBy).HasColumnName("created_by");
        builder.Property(s => s.UpdatedAt).HasColumnName("updated_at");
        builder.Property(s => s.UpdatedBy).HasColumnName("updated_by");
        builder.Property(s => s.IsDeleted).HasColumnName("is_deleted").IsRequired().HasDefaultValue(false);
        builder.Property(s => s.DeletedAt).HasColumnName("deleted_at");
        builder.Property(s => s.DeletedBy).HasColumnName("deleted_by");

        builder.Property<uint>("xmin").HasColumnName("xmin").IsRowVersion();

        builder.HasQueryFilter(s => !s.IsDeleted);

        builder.HasIndex(s => s.ReasonCode).IsUnique().HasDatabaseName("ux_stock_adjustment_reasons_reason_code").HasFilter("is_deleted = false");
    }
}
