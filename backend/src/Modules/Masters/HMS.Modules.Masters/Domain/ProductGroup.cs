using HMS.Shared.Kernel;

namespace HMS.Modules.Masters.Domain;

/// <summary>
/// Third-level product classification, belonging to one <see cref="ProductSubCategory"/>
/// (docs/03_Masters_ERD, "Product Classification"). <see cref="SubCategoryId"/> is a plain
/// FK (no navigation property) — see ProductCategory's XML comment for why.
/// </summary>
internal class ProductGroup : Entity
{
    public string GroupCode { get; private set; } = null!;
    public string GroupName { get; private set; } = null!;
    public Guid SubCategoryId { get; private set; }
    public int SortOrder { get; private set; }
    public string? Description { get; private set; }

    public bool IsActive { get; private set; } = true;

    private ProductGroup()
    {
    }

    private ProductGroup(Guid id, string groupCode, string groupName, Guid subCategoryId, int sortOrder, string? description, bool isActive, Guid? createdBy)
        : base(id, createdBy)
    {
        GroupCode = groupCode;
        GroupName = groupName;
        SubCategoryId = subCategoryId;
        SortOrder = sortOrder;
        Description = description;
        IsActive = isActive;
    }

    public static ProductGroup Create(string groupCode, string groupName, Guid subCategoryId, int sortOrder, string? description, bool isActive, Guid? createdBy)
    {
        Guard.AgainstNullOrWhiteSpace(groupCode, nameof(groupCode));
        Guard.AgainstNullOrWhiteSpace(groupName, nameof(groupName));

        return new ProductGroup(Guid.CreateVersion7(), groupCode.Trim().ToUpperInvariant(), groupName.Trim(), subCategoryId, sortOrder, description?.Trim(), isActive, createdBy);
    }

    public void Update(string groupName, Guid subCategoryId, int sortOrder, string? description, bool isActive, Guid? updatedBy)
    {
        Guard.AgainstNullOrWhiteSpace(groupName, nameof(groupName));

        GroupName = groupName.Trim();
        SubCategoryId = subCategoryId;
        SortOrder = sortOrder;
        Description = description?.Trim();
        IsActive = isActive;
        MarkUpdated(updatedBy);
    }
}
