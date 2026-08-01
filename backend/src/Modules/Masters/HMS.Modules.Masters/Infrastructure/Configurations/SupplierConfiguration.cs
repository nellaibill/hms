using HMS.Modules.Masters.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HMS.Modules.Masters.Infrastructure.Configurations;

internal class SupplierConfiguration : IEntityTypeConfiguration<Supplier>
{
    public void Configure(EntityTypeBuilder<Supplier> builder)
    {
        builder.ToTable("suppliers");

        builder.HasKey(s => s.Id).HasName("pk_suppliers");
        builder.Property(s => s.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(s => s.SupplierCode).HasColumnName("supplier_code").HasMaxLength(30).IsRequired();
        builder.Property(s => s.SupplierName).HasColumnName("supplier_name").HasMaxLength(150).IsRequired();
        builder.Property(s => s.ContactPerson).HasColumnName("contact_person").HasMaxLength(150);
        builder.Property(s => s.Phone).HasColumnName("phone").HasMaxLength(30);
        builder.Property(s => s.Email).HasColumnName("email").HasMaxLength(150);
        builder.Property(s => s.TaxId).HasColumnName("tax_id").HasMaxLength(50);
        builder.Property(s => s.Country).HasColumnName("country").HasMaxLength(100);
        builder.Property(s => s.PaymentTermId).HasColumnName("payment_term_id");
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

        builder.HasIndex(s => s.SupplierCode).IsUnique().HasDatabaseName("ux_suppliers_supplier_code").HasFilter("is_deleted = false");
        builder.HasIndex(s => s.PaymentTermId).HasDatabaseName("ix_suppliers_payment_term_id");

        builder.HasOne<PaymentTerm>()
            .WithMany()
            .HasForeignKey(s => s.PaymentTermId)
            .HasConstraintName("fk_suppliers_payment_term_id")
            .OnDelete(DeleteBehavior.Restrict);
    }
}
