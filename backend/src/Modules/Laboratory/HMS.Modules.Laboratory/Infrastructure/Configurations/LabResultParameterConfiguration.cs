using HMS.Modules.Laboratory.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HMS.Modules.Laboratory.Infrastructure.Configurations;

internal class LabResultParameterConfiguration : IEntityTypeConfiguration<LabResultParameter>
{
    public void Configure(EntityTypeBuilder<LabResultParameter> builder)
    {
        builder.ToTable("lab_result_parameters");

        builder.HasKey(p => p.Id).HasName("pk_lab_result_parameters");
        builder.Property(p => p.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(p => p.LabOrderItemId).HasColumnName("lab_order_item_id").IsRequired();
        builder.Property(p => p.ParameterName).HasColumnName("parameter_name").HasMaxLength(200).IsRequired();
        builder.Property(p => p.ResultValue).HasColumnName("result_value").HasMaxLength(500).IsRequired();
        builder.Property(p => p.Unit).HasColumnName("unit").HasMaxLength(50);
        builder.Property(p => p.ReferenceRange).HasColumnName("reference_range").HasMaxLength(200);
        builder.Property(p => p.Flag).HasColumnName("flag").HasConversion<string>().HasMaxLength(20);
        builder.Property(p => p.Remarks).HasColumnName("remarks").HasMaxLength(1000);

        builder.Property(p => p.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(p => p.CreatedBy).HasColumnName("created_by");
        builder.Property(p => p.UpdatedAt).HasColumnName("updated_at");
        builder.Property(p => p.UpdatedBy).HasColumnName("updated_by");
        builder.Property(p => p.IsDeleted).HasColumnName("is_deleted").IsRequired().HasDefaultValue(false);
        builder.Property(p => p.DeletedAt).HasColumnName("deleted_at");
        builder.Property(p => p.DeletedBy).HasColumnName("deleted_by");

        builder.Property<uint>("xmin").HasColumnName("xmin").IsRowVersion();

        builder.HasQueryFilter(p => !p.IsDeleted);

        builder.HasIndex(p => p.LabOrderItemId).HasDatabaseName("ix_lab_result_parameters_lab_order_item_id");
    }
}
