using HMS.Modules.Pharmacy.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HMS.Modules.Pharmacy.Infrastructure.Configurations;

internal class PharmacySaleConfiguration : IEntityTypeConfiguration<PharmacySale>
{
    public void Configure(EntityTypeBuilder<PharmacySale> builder)
    {
        builder.Ignore(s => s.BalanceDue);
        builder.MapDocument("pharmacy_sales");

        builder.HasIndex(s => s.BillNo).IsUnique().HasDatabaseName("ux_pharmacy_sales_bill_no");
        builder.HasIndex(s => s.SaleDate).HasDatabaseName("ix_pharmacy_sales_sale_date");
        builder.HasIndex(s => s.PatientId).HasDatabaseName("ix_pharmacy_sales_patient_id");
        builder.HasIndex(s => s.AdmissionId).HasDatabaseName("ix_pharmacy_sales_admission_id");

        builder.HasOne<PharmacyStore>().WithMany().HasForeignKey(s => s.StoreId)
            .HasConstraintName("fk_pharmacy_sales_pharmacy_stores_store_id").OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(s => s.Lines).WithOne().HasForeignKey(l => l.SaleId)
            .HasConstraintName("fk_pharmacy_sale_lines_sale_id").OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(s => s.Lines).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(s => s.Payments).WithOne().HasForeignKey(p => p.SaleId)
            .HasConstraintName("fk_pharmacy_sale_payments_sale_id").OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(s => s.Payments).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

internal class PharmacySaleLineConfiguration : IEntityTypeConfiguration<PharmacySaleLine>
{
    public void Configure(EntityTypeBuilder<PharmacySaleLine> builder)
    {
        builder.Ignore(l => l.LineTotal);
        builder.Ignore(l => l.ReturnableQuantity);
        builder.MapDocument("pharmacy_sale_lines");

        builder.HasIndex(l => l.SaleId).HasDatabaseName("ix_pharmacy_sale_lines_sale_id");
        builder.HasIndex(l => l.ProductId).HasDatabaseName("ix_pharmacy_sale_lines_product_id");

        // The statutory registers list every sale of an H1/X/narcotic drug.
        builder.HasIndex(l => l.Schedule).HasDatabaseName("ix_pharmacy_sale_lines_schedule");
    }
}

internal class PharmacySalePaymentConfiguration : IEntityTypeConfiguration<PharmacySalePayment>
{
    public void Configure(EntityTypeBuilder<PharmacySalePayment> builder)
    {
        builder.MapDocument("pharmacy_sale_payments");

        builder.HasIndex(p => p.SaleId).HasDatabaseName("ix_pharmacy_sale_payments_sale_id");
        builder.HasIndex(p => p.PaidAt).HasDatabaseName("ix_pharmacy_sale_payments_paid_at");
    }
}

internal class PharmacySaleReturnConfiguration : IEntityTypeConfiguration<PharmacySaleReturn>
{
    public void Configure(EntityTypeBuilder<PharmacySaleReturn> builder)
    {
        builder.Ignore(r => r.LinesTotal);
        builder.MapDocument("pharmacy_sale_returns");

        builder.HasIndex(r => r.ReturnNo).IsUnique().HasDatabaseName("ux_pharmacy_sale_returns_return_no");
        builder.HasIndex(r => r.SaleId).HasDatabaseName("ix_pharmacy_sale_returns_sale_id");
        builder.HasIndex(r => r.ReturnDate).HasDatabaseName("ix_pharmacy_sale_returns_return_date");

        builder.HasOne<PharmacySale>().WithMany().HasForeignKey(r => r.SaleId)
            .HasConstraintName("fk_pharmacy_sale_returns_sale_id").OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(r => r.Lines).WithOne().HasForeignKey(l => l.SaleReturnId)
            .HasConstraintName("fk_pharmacy_sale_return_lines_sale_return_id").OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(r => r.Lines).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

internal class PharmacySaleReturnLineConfiguration : IEntityTypeConfiguration<PharmacySaleReturnLine>
{
    public void Configure(EntityTypeBuilder<PharmacySaleReturnLine> builder)
    {
        builder.Ignore(l => l.LineTotal);
        builder.MapDocument("pharmacy_sale_return_lines");

        builder.HasIndex(l => l.SaleReturnId).HasDatabaseName("ix_pharmacy_sale_return_lines_sale_return_id");
    }
}

internal class PharmacyHeldBillConfiguration : IEntityTypeConfiguration<PharmacyHeldBill>
{
    public void Configure(EntityTypeBuilder<PharmacyHeldBill> builder)
    {
        builder.MapDocument("pharmacy_held_bills");
        builder.Property(h => h.Payload).HasColumnType("jsonb");

        builder.HasIndex(h => h.StoreId).HasDatabaseName("ix_pharmacy_held_bills_store_id");
    }
}
