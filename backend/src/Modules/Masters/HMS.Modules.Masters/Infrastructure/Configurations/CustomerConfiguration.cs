using HMS.Modules.Masters.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HMS.Modules.Masters.Infrastructure.Configurations;

internal class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("customers");

        builder.HasKey(c => c.Id).HasName("pk_customers");
        builder.Property(c => c.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(c => c.CustomerCode).HasColumnName("customer_code").HasMaxLength(30).IsRequired();
        builder.Property(c => c.CustomerName).HasColumnName("customer_name").HasMaxLength(150).IsRequired();
        builder.Property(c => c.ContactPerson).HasColumnName("contact_person").HasMaxLength(150);
        builder.Property(c => c.Phone).HasColumnName("phone").HasMaxLength(30);
        builder.Property(c => c.Email).HasColumnName("email").HasMaxLength(150);
        builder.Property(c => c.Country).HasColumnName("country").HasMaxLength(100);
        builder.Property(c => c.PaymentTermId).HasColumnName("payment_term_id");
        builder.Property(c => c.IsActive).HasColumnName("is_active").IsRequired().HasDefaultValue(true);

        builder.Property(c => c.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(c => c.CreatedBy).HasColumnName("created_by");
        builder.Property(c => c.UpdatedAt).HasColumnName("updated_at");
        builder.Property(c => c.UpdatedBy).HasColumnName("updated_by");
        builder.Property(c => c.IsDeleted).HasColumnName("is_deleted").IsRequired().HasDefaultValue(false);
        builder.Property(c => c.DeletedAt).HasColumnName("deleted_at");
        builder.Property(c => c.DeletedBy).HasColumnName("deleted_by");

        builder.Property<uint>("xmin").HasColumnName("xmin").IsRowVersion();

        builder.HasQueryFilter(c => !c.IsDeleted);

        builder.HasIndex(c => c.CustomerCode).IsUnique().HasDatabaseName("ux_customers_customer_code").HasFilter("is_deleted = false");
        builder.HasIndex(c => c.PaymentTermId).HasDatabaseName("ix_customers_payment_term_id");

        builder.HasOne<PaymentTerm>()
            .WithMany()
            .HasForeignKey(c => c.PaymentTermId)
            .HasConstraintName("fk_customers_payment_term_id")
            .OnDelete(DeleteBehavior.Restrict);
    }
}
