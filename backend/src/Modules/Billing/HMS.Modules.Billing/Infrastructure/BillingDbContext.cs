using HMS.Modules.Billing.Domain;
using Microsoft.EntityFrameworkCore;

namespace HMS.Modules.Billing.Infrastructure;

public class BillingDbContext : DbContext
{
    public const string SchemaName = "billing";
    public const string InvoiceNumberSequenceName = "invoice_no_seq";

    public BillingDbContext(DbContextOptions<BillingDbContext> options) : base(options)
    {
    }

    // Internal (not public): domain types are internal, so DbSet<T> properties stay internal too.
    internal DbSet<Invoice> Invoices => Set<Invoice>();
    internal DbSet<InvoiceLineItem> InvoiceLineItems => Set<InvoiceLineItem>();
    internal DbSet<Payment> Payments => Set<Payment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(SchemaName);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BillingDbContext).Assembly);

        modelBuilder.HasSequence<long>(InvoiceNumberSequenceName, SchemaName).StartsAt(1);
    }
}
