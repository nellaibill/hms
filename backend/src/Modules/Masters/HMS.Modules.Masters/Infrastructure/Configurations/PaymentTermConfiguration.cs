using HMS.Modules.Masters.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HMS.Modules.Masters.Infrastructure.Configurations;

internal class PaymentTermConfiguration : IEntityTypeConfiguration<PaymentTerm>
{
    public void Configure(EntityTypeBuilder<PaymentTerm> builder)
    {
        builder.ToTable("payment_terms");

        builder.HasKey(p => p.Id).HasName("pk_payment_terms");
        builder.Property(p => p.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(p => p.TermName).HasColumnName("term_name").HasMaxLength(100).IsRequired();
        builder.Property(p => p.Days).HasColumnName("days").IsRequired();
        builder.Property(p => p.Description).HasColumnName("description");
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

        builder.HasIndex(p => p.TermName).IsUnique().HasDatabaseName("ux_payment_terms_term_name").HasFilter("is_deleted = false");
    }
}
