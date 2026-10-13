using HMS.Modules.Pharmacy.Contracts;
using HMS.Shared.Kernel;

namespace HMS.Modules.Pharmacy.Domain;

/// <summary>
/// A place the pharmacy holds stock — the main store, an OP or IP dispensing counter, a ward
/// or theatre sub-store. Every balance and every ledger row belongs to exactly one store, so
/// the same batch held in two stores is two separate balances.
/// <para>
/// Owned by Pharmacy rather than reusing Masters' Warehouse: a store carries pharmacy meaning
/// (its type, which one stock defaults to) that a generic warehouse address record doesn't,
/// and being in this schema lets balances and the ledger hold a real foreign key to it.
/// </para>
/// </summary>
internal class PharmacyStore : Entity
{
    public string Code { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public StoreType StoreType { get; private set; }

    /// <summary>The store used when a request doesn't name one. Exactly one store is the
    /// default at any time — StoreService moves the flag rather than ever leaving none.</summary>
    public bool IsDefault { get; private set; }

    public bool IsActive { get; private set; } = true;

    // Required by EF Core materialization.
    private PharmacyStore()
    {
    }

    private PharmacyStore(Guid id, string code, string name, StoreType storeType, Guid? createdBy)
        : base(id, createdBy)
    {
        Code = code;
        Name = name;
        StoreType = storeType;
    }

    public static PharmacyStore Create(string code, string name, StoreType storeType, Guid? createdBy)
    {
        Guard.AgainstNullOrWhiteSpace(code, nameof(code));
        Guard.AgainstNullOrWhiteSpace(name, nameof(name));

        return new PharmacyStore(Guid.CreateVersion7(), code.Trim().ToUpperInvariant(), name.Trim(), storeType, createdBy);
    }

    public void Update(string name, StoreType storeType, bool isActive, Guid? updatedBy)
    {
        Guard.AgainstNullOrWhiteSpace(name, nameof(name));
        if (IsDefault && !isActive)
        {
            throw new InvalidOperationException("The default store cannot be deactivated.");
        }

        Name = name.Trim();
        StoreType = storeType;
        IsActive = isActive;
        MarkUpdated(updatedBy);
    }

    public void MakeDefault(Guid? updatedBy)
    {
        if (!IsActive)
        {
            throw new InvalidOperationException("An inactive store cannot be the default.");
        }

        IsDefault = true;
        MarkUpdated(updatedBy);
    }

    public void ClearDefault(Guid? updatedBy)
    {
        IsDefault = false;
        MarkUpdated(updatedBy);
    }
}
