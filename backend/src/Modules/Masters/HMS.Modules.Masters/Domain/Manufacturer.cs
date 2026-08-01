using HMS.Shared.Kernel;

namespace HMS.Modules.Masters.Domain;

/// <summary>
/// Manufacturer master with contact details (docs/03_Masters_ERD, "Brand &amp; Manufacturer").
/// <see cref="Country"/> is free text, not a foreign key — no Country master exists yet
/// (matches the UI's free-text decision; see docs/DecisionLog.md).
/// </summary>
internal class Manufacturer : Entity
{
    public string ManufacturerCode { get; private set; } = null!;
    public string ManufacturerName { get; private set; } = null!;
    public string? ContactPerson { get; private set; }
    public string? Phone { get; private set; }
    public string? Email { get; private set; }
    public string? Country { get; private set; }

    public bool IsActive { get; private set; } = true;

    private Manufacturer()
    {
    }

    private Manufacturer(Guid id, string manufacturerCode, string manufacturerName, string? contactPerson, string? phone, string? email, string? country, bool isActive, Guid? createdBy)
        : base(id, createdBy)
    {
        ManufacturerCode = manufacturerCode;
        ManufacturerName = manufacturerName;
        ContactPerson = contactPerson;
        Phone = phone;
        Email = email;
        Country = country;
        IsActive = isActive;
    }

    public static Manufacturer Create(string manufacturerCode, string manufacturerName, string? contactPerson, string? phone, string? email, string? country, bool isActive, Guid? createdBy)
    {
        Guard.AgainstNullOrWhiteSpace(manufacturerCode, nameof(manufacturerCode));
        Guard.AgainstNullOrWhiteSpace(manufacturerName, nameof(manufacturerName));

        return new Manufacturer(
            Guid.CreateVersion7(),
            manufacturerCode.Trim().ToUpperInvariant(),
            manufacturerName.Trim(),
            contactPerson?.Trim(),
            phone?.Trim(),
            email?.Trim().ToLowerInvariant(),
            country?.Trim(),
            isActive,
            createdBy);
    }

    public void Update(string manufacturerName, string? contactPerson, string? phone, string? email, string? country, bool isActive, Guid? updatedBy)
    {
        Guard.AgainstNullOrWhiteSpace(manufacturerName, nameof(manufacturerName));

        ManufacturerName = manufacturerName.Trim();
        ContactPerson = contactPerson?.Trim();
        Phone = phone?.Trim();
        Email = email?.Trim().ToLowerInvariant();
        Country = country?.Trim();
        IsActive = isActive;
        MarkUpdated(updatedBy);
    }
}
