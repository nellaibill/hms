using HMS.Modules.Masters.Contracts;
using HMS.Shared.Kernel;

namespace HMS.Modules.Masters.Domain;

/// <summary>
/// A billable laboratory or radiology test/package (e.g. "Complete Blood Count", "Digital
/// X-ray", "LFT Package") with its standard patient-facing price — shared reference data for
/// Billing's Laboratory/Radiology sections, replacing the frontend's former hardcoded catalog.
/// ServiceType picks which billing section offers the test. IsOutsourced/ReferenceLab record
/// that an in-house lab routes the sample to an external lab (e.g. Q-LAB) rather than running
/// it itself — informational for now, not used in any billing calculation.
/// </summary>
internal class DiagnosticTest : Entity
{
    public string Name { get; private set; } = null!;
    public DiagnosticTestServiceType ServiceType { get; private set; }
    public string? Category { get; private set; }
    public decimal Price { get; private set; }
    public bool IsOutsourced { get; private set; }
    public string? ReferenceLab { get; private set; }
    public bool IsActive { get; private set; } = true;

    // Required by EF Core materialization.
    private DiagnosticTest()
    {
    }

    private DiagnosticTest(
        Guid id,
        string name,
        DiagnosticTestServiceType serviceType,
        string? category,
        decimal price,
        bool isOutsourced,
        string? referenceLab,
        bool isActive,
        Guid? createdBy)
        : base(id, createdBy)
    {
        Name = name;
        ServiceType = serviceType;
        Category = category;
        Price = price;
        IsOutsourced = isOutsourced;
        ReferenceLab = referenceLab;
        IsActive = isActive;
    }

    public static DiagnosticTest Create(
        string name,
        DiagnosticTestServiceType serviceType,
        string? category,
        decimal price,
        bool isOutsourced,
        string? referenceLab,
        bool isActive,
        Guid? createdBy)
    {
        Guard.AgainstNullOrWhiteSpace(name, nameof(name));
        if (price < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(price), "Price cannot be negative.");
        }

        return new DiagnosticTest(
            Guid.CreateVersion7(),
            name.Trim(),
            serviceType,
            category?.Trim(),
            price,
            isOutsourced,
            referenceLab?.Trim(),
            isActive,
            createdBy);
    }

    public void Update(
        string name,
        DiagnosticTestServiceType serviceType,
        string? category,
        decimal price,
        bool isOutsourced,
        string? referenceLab,
        bool isActive,
        Guid? updatedBy)
    {
        Guard.AgainstNullOrWhiteSpace(name, nameof(name));
        if (price < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(price), "Price cannot be negative.");
        }

        Name = name.Trim();
        ServiceType = serviceType;
        Category = category?.Trim();
        Price = price;
        IsOutsourced = isOutsourced;
        ReferenceLab = referenceLab?.Trim();
        IsActive = isActive;
        MarkUpdated(updatedBy);
    }
}
