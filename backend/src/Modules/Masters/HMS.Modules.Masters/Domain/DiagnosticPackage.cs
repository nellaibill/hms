using HMS.Shared.Kernel;

namespace HMS.Modules.Masters.Domain;

/// <summary>
/// A bundle of <see cref="DiagnosticService"/> tests sold at one discounted price (e.g. a
/// "Master Health Checkup" package) — the aggregate root for its <see cref="Items"/>, mirroring
/// PatientVisit/PatientVisitConsultation's own aggregate-root + child shape. TotalPrice is a
/// deliberate, independent bundle-discount price — never derived from summing item prices, and
/// never recomputed when items change, so a package's price stays exactly what Billing quoted
/// a patient. Items get added/removed one at a time after creation (AddItem/RemoveItem) for the
/// package detail page's "add/remove one test" workflow — RemoveItem has no PatientVisit
/// equivalent (visits only ever grow), added here because packages need it.
/// </summary>
internal class DiagnosticPackage : Entity
{
    public string Code { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public string? Description { get; private set; }
    public decimal TotalPrice { get; private set; }

    /// <summary>What it costs the hospital to run the whole package once — the package's own
    /// running cost, as distinct from <see cref="TotalPrice"/>, what the patient is billed.
    /// Like the bundle price it's a deliberate figure, not derived from the tests. Defaults to 0
    /// (not yet costed), matching DiagnosticService.CostPrice; profit reports then fall back to
    /// the sum of the tests' own running costs.</summary>
    public decimal CostPrice { get; private set; }

    public bool IsActive { get; private set; } = true;

    private readonly List<DiagnosticPackageItem> _items = [];
    public IReadOnlyCollection<DiagnosticPackageItem> Items => _items.AsReadOnly();

    // Required by EF Core materialization.
    private DiagnosticPackage()
    {
    }

    private DiagnosticPackage(
        Guid id,
        string code,
        string name,
        string? description,
        decimal totalPrice,
        decimal costPrice,
        bool isActive,
        Guid? createdBy)
        : base(id, createdBy)
    {
        Code = code;
        Name = name;
        Description = description;
        TotalPrice = totalPrice;
        CostPrice = costPrice;
        IsActive = isActive;
    }

    public static DiagnosticPackage Create(
        string code,
        string name,
        string? description,
        decimal totalPrice,
        decimal costPrice,
        bool isActive,
        IReadOnlyList<Guid> serviceIds,
        Guid? createdBy)
    {
        Guard.AgainstNullOrWhiteSpace(code, nameof(code));
        Guard.AgainstNullOrWhiteSpace(name, nameof(name));
        GuardPrices(totalPrice, costPrice);

        var package = new DiagnosticPackage(
            Guid.CreateVersion7(),
            code.Trim(),
            name.Trim(),
            description?.Trim(),
            totalPrice,
            costPrice,
            isActive,
            createdBy);

        foreach (var serviceId in serviceIds)
        {
            package._items.Add(DiagnosticPackageItem.Create(package.Id, serviceId));
        }

        return package;
    }

    public void Update(
        string code,
        string name,
        string? description,
        decimal totalPrice,
        decimal costPrice,
        bool isActive,
        Guid? updatedBy)
    {
        Guard.AgainstNullOrWhiteSpace(code, nameof(code));
        Guard.AgainstNullOrWhiteSpace(name, nameof(name));
        GuardPrices(totalPrice, costPrice);

        Code = code.Trim();
        Name = name.Trim();
        Description = description?.Trim();
        TotalPrice = totalPrice;
        CostPrice = costPrice;
        IsActive = isActive;
        MarkUpdated(updatedBy);
    }

    private static void GuardPrices(decimal totalPrice, decimal costPrice)
    {
        if (totalPrice < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(totalPrice), "Total price cannot be negative.");
        }

        if (costPrice < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(costPrice), "Cost price cannot be negative.");
        }
    }

    public DiagnosticPackageItem AddItem(Guid serviceId, Guid? updatedBy)
    {
        var item = DiagnosticPackageItem.Create(Id, serviceId);
        _items.Add(item);
        MarkUpdated(updatedBy);
        return item;
    }

    /// <returns>false if no item with that id exists on this package.</returns>
    public bool RemoveItem(Guid itemId, Guid? updatedBy)
    {
        var item = _items.FirstOrDefault(i => i.Id == itemId);
        if (item is null)
        {
            return false;
        }

        _items.Remove(item);
        MarkUpdated(updatedBy);
        return true;
    }
}
