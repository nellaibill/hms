using HMS.Modules.Masters.Domain;
using Microsoft.EntityFrameworkCore;

namespace HMS.Modules.Masters.Infrastructure;

/// <summary>
/// Owns the "masters" PostgreSQL schema — reference/lookup data for Inventory &amp; ERP
/// (see docs/03_Masters_ERD). Per docs/DatabaseArchitecture.md §1, only this module's own
/// code constructs/migrates this context — no other module references it.
/// </summary>
public class MastersDbContext : DbContext
{
    public const string SchemaName = "masters";

    public MastersDbContext(DbContextOptions<MastersDbContext> options) : base(options)
    {
    }

    // Internal (not public): each entity is an internal domain type, so public DbSet<T>
    // properties would be CS0053 accessibility violations. The context itself stays public
    // (HMS.Api's Program.cs resolves it by type for the startup migration call), but these
    // DbSets are only ever queried from within this module.
    internal DbSet<Currency> Currencies => Set<Currency>();
    internal DbSet<PaymentMethod> PaymentMethods => Set<PaymentMethod>();
    internal DbSet<PaymentTerm> PaymentTerms => Set<PaymentTerm>();
    internal DbSet<StockAdjustmentReason> StockAdjustmentReasons => Set<StockAdjustmentReason>();
    internal DbSet<Brand> Brands => Set<Brand>();
    internal DbSet<Manufacturer> Manufacturers => Set<Manufacturer>();
    internal DbSet<UnitOfMeasure> UnitsOfMeasure => Set<UnitOfMeasure>();
    internal DbSet<Tax> Taxes => Set<Tax>();
    internal DbSet<Warehouse> Warehouses => Set<Warehouse>();
    internal DbSet<ProductCategory> ProductCategories => Set<ProductCategory>();
    internal DbSet<ProductSubCategory> ProductSubCategories => Set<ProductSubCategory>();
    internal DbSet<ProductGroup> ProductGroups => Set<ProductGroup>();
    internal DbSet<UnitConversion> UnitConversions => Set<UnitConversion>();
    internal DbSet<StorageLocation> StorageLocations => Set<StorageLocation>();
    internal DbSet<Supplier> Suppliers => Set<Supplier>();
    internal DbSet<Customer> Customers => Set<Customer>();
    internal DbSet<Department> Departments => Set<Department>();
    internal DbSet<Consultant> Consultants => Set<Consultant>();
    internal DbSet<AppointmentType> AppointmentTypes => Set<AppointmentType>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(SchemaName);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(MastersDbContext).Assembly);
    }
}
