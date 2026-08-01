using HMS.Shared.Kernel;

namespace HMS.Modules.Masters.Domain;

/// <summary>
/// Vendor master for purchasing goods and services (docs/03_Masters_ERD, "Business
/// Partners"). <see cref="Country"/> is free text (no Country master yet);
/// <see cref="PaymentTermId"/> optionally references a <see cref="PaymentTerm"/> as a plain
/// FK (no navigation property) — see ProductCategory's XML comment for why.
/// </summary>
internal class Supplier : Entity
{
    public string SupplierCode { get; private set; } = null!;
    public string SupplierName { get; private set; } = null!;
    public string? ContactPerson { get; private set; }
    public string? Phone { get; private set; }
    public string? Email { get; private set; }
    public string? TaxId { get; private set; }
    public string? Country { get; private set; }
    public Guid? PaymentTermId { get; private set; }

    public bool IsActive { get; private set; } = true;

    private Supplier()
    {
    }

    private Supplier(Guid id, string supplierCode, string supplierName, string? contactPerson, string? phone, string? email, string? taxId, string? country, Guid? paymentTermId, bool isActive, Guid? createdBy)
        : base(id, createdBy)
    {
        SupplierCode = supplierCode;
        SupplierName = supplierName;
        ContactPerson = contactPerson;
        Phone = phone;
        Email = email;
        TaxId = taxId;
        Country = country;
        PaymentTermId = paymentTermId;
        IsActive = isActive;
    }

    public static Supplier Create(string supplierCode, string supplierName, string? contactPerson, string? phone, string? email, string? taxId, string? country, Guid? paymentTermId, bool isActive, Guid? createdBy)
    {
        Guard.AgainstNullOrWhiteSpace(supplierCode, nameof(supplierCode));
        Guard.AgainstNullOrWhiteSpace(supplierName, nameof(supplierName));

        return new Supplier(
            Guid.CreateVersion7(),
            supplierCode.Trim().ToUpperInvariant(),
            supplierName.Trim(),
            contactPerson?.Trim(),
            phone?.Trim(),
            email?.Trim().ToLowerInvariant(),
            taxId?.Trim(),
            country?.Trim(),
            paymentTermId,
            isActive,
            createdBy);
    }

    public void Update(string supplierName, string? contactPerson, string? phone, string? email, string? taxId, string? country, Guid? paymentTermId, bool isActive, Guid? updatedBy)
    {
        Guard.AgainstNullOrWhiteSpace(supplierName, nameof(supplierName));

        SupplierName = supplierName.Trim();
        ContactPerson = contactPerson?.Trim();
        Phone = phone?.Trim();
        Email = email?.Trim().ToLowerInvariant();
        TaxId = taxId?.Trim();
        Country = country?.Trim();
        PaymentTermId = paymentTermId;
        IsActive = isActive;
        MarkUpdated(updatedBy);
    }
}
