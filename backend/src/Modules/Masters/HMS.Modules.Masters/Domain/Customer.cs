using HMS.Shared.Kernel;

namespace HMS.Modules.Masters.Domain;

/// <summary>
/// Institutional/bulk customer master for sales and billing (docs/03_Masters_ERD,
/// "Business Partners") — distinct from individual patients (HMS.Modules.Patients).
/// <see cref="Country"/> is free text (no Country master yet); <see cref="PaymentTermId"/>
/// optionally references a <see cref="PaymentTerm"/> as a plain FK (no navigation property)
/// — see ProductCategory's XML comment for why.
/// </summary>
internal class Customer : Entity
{
    public string CustomerCode { get; private set; } = null!;
    public string CustomerName { get; private set; } = null!;
    public string? ContactPerson { get; private set; }
    public string? Phone { get; private set; }
    public string? Email { get; private set; }
    public string? Country { get; private set; }
    public Guid? PaymentTermId { get; private set; }

    public bool IsActive { get; private set; } = true;

    private Customer()
    {
    }

    private Customer(Guid id, string customerCode, string customerName, string? contactPerson, string? phone, string? email, string? country, Guid? paymentTermId, bool isActive, Guid? createdBy)
        : base(id, createdBy)
    {
        CustomerCode = customerCode;
        CustomerName = customerName;
        ContactPerson = contactPerson;
        Phone = phone;
        Email = email;
        Country = country;
        PaymentTermId = paymentTermId;
        IsActive = isActive;
    }

    public static Customer Create(string customerCode, string customerName, string? contactPerson, string? phone, string? email, string? country, Guid? paymentTermId, bool isActive, Guid? createdBy)
    {
        Guard.AgainstNullOrWhiteSpace(customerCode, nameof(customerCode));
        Guard.AgainstNullOrWhiteSpace(customerName, nameof(customerName));

        return new Customer(
            Guid.CreateVersion7(),
            customerCode.Trim().ToUpperInvariant(),
            customerName.Trim(),
            contactPerson?.Trim(),
            phone?.Trim(),
            email?.Trim().ToLowerInvariant(),
            country?.Trim(),
            paymentTermId,
            isActive,
            createdBy);
    }

    public void Update(string customerName, string? contactPerson, string? phone, string? email, string? country, Guid? paymentTermId, bool isActive, Guid? updatedBy)
    {
        Guard.AgainstNullOrWhiteSpace(customerName, nameof(customerName));

        CustomerName = customerName.Trim();
        ContactPerson = contactPerson?.Trim();
        Phone = phone?.Trim();
        Email = email?.Trim().ToLowerInvariant();
        Country = country?.Trim();
        PaymentTermId = paymentTermId;
        IsActive = isActive;
        MarkUpdated(updatedBy);
    }
}
