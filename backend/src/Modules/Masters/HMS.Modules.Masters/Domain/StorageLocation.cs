using HMS.Shared.Kernel;

namespace HMS.Modules.Masters.Domain;

/// <summary>
/// Rack/Shelf/Bin hierarchy within a <see cref="Warehouse"/> (docs/03_Masters_ERD,
/// "Warehouse &amp; Location"). <see cref="ParentLocationId"/> optionally self-references
/// another location in the same warehouse (e.g. a Bin's parent is a Shelf). Both FKs are
/// plain scalars (no navigation properties) — see ProductCategory's XML comment for why.
/// <see cref="LocationCode"/> is unique per warehouse, not globally.
/// </summary>
internal class StorageLocation : Entity
{
    public Guid WarehouseId { get; private set; }
    public string LocationCode { get; private set; } = null!;
    public string LocationName { get; private set; } = null!;
    public string? LocationType { get; private set; }
    public Guid? ParentLocationId { get; private set; }

    public bool IsActive { get; private set; } = true;

    private StorageLocation()
    {
    }

    private StorageLocation(Guid id, Guid warehouseId, string locationCode, string locationName, string? locationType, Guid? parentLocationId, bool isActive, Guid? createdBy)
        : base(id, createdBy)
    {
        WarehouseId = warehouseId;
        LocationCode = locationCode;
        LocationName = locationName;
        LocationType = locationType;
        ParentLocationId = parentLocationId;
        IsActive = isActive;
    }

    public static StorageLocation Create(Guid warehouseId, string locationCode, string locationName, string? locationType, Guid? parentLocationId, bool isActive, Guid? createdBy)
    {
        Guard.AgainstNullOrWhiteSpace(locationCode, nameof(locationCode));
        Guard.AgainstNullOrWhiteSpace(locationName, nameof(locationName));

        return new StorageLocation(Guid.CreateVersion7(), warehouseId, locationCode.Trim().ToUpperInvariant(), locationName.Trim(), locationType?.Trim(), parentLocationId, isActive, createdBy);
    }

    public void Update(string locationName, string? locationType, Guid? parentLocationId, bool isActive, Guid? updatedBy)
    {
        Guard.AgainstNullOrWhiteSpace(locationName, nameof(locationName));

        LocationName = locationName.Trim();
        LocationType = locationType?.Trim();
        ParentLocationId = parentLocationId;
        IsActive = isActive;
        MarkUpdated(updatedBy);
    }
}
