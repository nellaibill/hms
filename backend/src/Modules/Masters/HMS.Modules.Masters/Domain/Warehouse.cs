using HMS.Shared.Kernel;

namespace HMS.Modules.Masters.Domain;

/// <summary>
/// Physical storage facility used across Inventory &amp; ERP (docs/03_Masters_ERD, "Warehouse
/// &amp; Location"). <see cref="Country"/>/<see cref="State"/> are free text, not foreign keys
/// — no Country/State master exists yet (matches the UI's free-text decision).
/// </summary>
internal class Warehouse : Entity
{
    public string WarehouseCode { get; private set; } = null!;
    public string WarehouseName { get; private set; } = null!;
    public string? Address { get; private set; }
    public string? Country { get; private set; }
    public string? State { get; private set; }

    public bool IsActive { get; private set; } = true;

    private Warehouse()
    {
    }

    private Warehouse(Guid id, string warehouseCode, string warehouseName, string? address, string? country, string? state, bool isActive, Guid? createdBy)
        : base(id, createdBy)
    {
        WarehouseCode = warehouseCode;
        WarehouseName = warehouseName;
        Address = address;
        Country = country;
        State = state;
        IsActive = isActive;
    }

    public static Warehouse Create(string warehouseCode, string warehouseName, string? address, string? country, string? state, bool isActive, Guid? createdBy)
    {
        Guard.AgainstNullOrWhiteSpace(warehouseCode, nameof(warehouseCode));
        Guard.AgainstNullOrWhiteSpace(warehouseName, nameof(warehouseName));

        return new Warehouse(Guid.CreateVersion7(), warehouseCode.Trim().ToUpperInvariant(), warehouseName.Trim(), address?.Trim(), country?.Trim(), state?.Trim(), isActive, createdBy);
    }

    public void Update(string warehouseName, string? address, string? country, string? state, bool isActive, Guid? updatedBy)
    {
        Guard.AgainstNullOrWhiteSpace(warehouseName, nameof(warehouseName));

        WarehouseName = warehouseName.Trim();
        Address = address?.Trim();
        Country = country?.Trim();
        State = state?.Trim();
        IsActive = isActive;
        MarkUpdated(updatedBy);
    }
}
