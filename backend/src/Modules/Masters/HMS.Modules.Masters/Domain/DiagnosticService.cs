using HMS.Modules.Masters.Contracts;
using HMS.Shared.Kernel;

namespace HMS.Modules.Masters.Domain;

/// <summary>
/// A billable laboratory or radiology test (e.g. "Complete Blood Count", "Digital X-ray") with
/// its standard patient-facing price — the normalized replacement for the Laboratory/Radiology
/// half of the old flat DiagnosticTest entity. Procedure-type billing stays on DiagnosticTest;
/// <see cref="ServiceType"/> here is only ever Laboratory or Radiology (enforced by
/// CreateDiagnosticServiceRequestValidator/UpdateDiagnosticServiceRequestValidator, not by this
/// entity itself, which reuses DiagnosticTestServiceType rather than a third near-identical
/// enum). CategoryId/ProviderId are app-level references into DiagnosticCategory/
/// DiagnosticProvider — validated by DiagnosticServiceService, not enforced by a database
/// foreign key, same convention as DiagnosticTest's siblings elsewhere in this module.
/// </summary>
internal class DiagnosticService : Entity
{
    public string Code { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public Guid CategoryId { get; private set; }
    public DiagnosticTestServiceType ServiceType { get; private set; }
    public bool IsOutsourced { get; private set; }
    public Guid? ProviderId { get; private set; }
    public decimal Price { get; private set; }
    public bool IsActive { get; private set; } = true;

    // Required by EF Core materialization.
    private DiagnosticService()
    {
    }

    private DiagnosticService(
        Guid id,
        string code,
        string name,
        Guid categoryId,
        DiagnosticTestServiceType serviceType,
        bool isOutsourced,
        Guid? providerId,
        decimal price,
        bool isActive,
        Guid? createdBy)
        : base(id, createdBy)
    {
        Code = code;
        Name = name;
        CategoryId = categoryId;
        ServiceType = serviceType;
        IsOutsourced = isOutsourced;
        ProviderId = providerId;
        Price = price;
        IsActive = isActive;
    }

    public static DiagnosticService Create(
        string code,
        string name,
        Guid categoryId,
        DiagnosticTestServiceType serviceType,
        bool isOutsourced,
        Guid? providerId,
        decimal price,
        bool isActive,
        Guid? createdBy)
    {
        Guard.AgainstNullOrWhiteSpace(code, nameof(code));
        Guard.AgainstNullOrWhiteSpace(name, nameof(name));
        if (price < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(price), "Price cannot be negative.");
        }

        return new DiagnosticService(
            Guid.CreateVersion7(),
            code.Trim(),
            name.Trim(),
            categoryId,
            serviceType,
            isOutsourced,
            providerId,
            price,
            isActive,
            createdBy);
    }

    public void Update(
        string code,
        string name,
        Guid categoryId,
        DiagnosticTestServiceType serviceType,
        bool isOutsourced,
        Guid? providerId,
        decimal price,
        bool isActive,
        Guid? updatedBy)
    {
        Guard.AgainstNullOrWhiteSpace(code, nameof(code));
        Guard.AgainstNullOrWhiteSpace(name, nameof(name));
        if (price < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(price), "Price cannot be negative.");
        }

        Code = code.Trim();
        Name = name.Trim();
        CategoryId = categoryId;
        ServiceType = serviceType;
        IsOutsourced = isOutsourced;
        ProviderId = providerId;
        Price = price;
        IsActive = isActive;
        MarkUpdated(updatedBy);
    }
}
