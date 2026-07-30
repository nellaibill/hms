using HMS.Shared.Kernel;

namespace HMS.Modules.Masters.Domain;

/// <summary>
/// Second-level product classification, belonging to one <see cref="ProductCategory"/>
/// (docs/03_Masters_ERD, "Product Classification"). <see cref="CategoryId"/> is a plain FK
/// (no navigation property) — see ProductCategory's XML comment for why.
/// </summary>
internal class ProductSubCategory : Entity
{
    public string SubCategoryCode { get; private set; } = null!;
    public string SubCategoryName { get; private set; } = null!;
    public Guid CategoryId { get; private set; }
    public int SortOrder { get; private set; }
    public string? Description { get; private set; }

    public bool IsActive { get; private set; } = true;

    private ProductSubCategory()
    {
    }

    private ProductSubCategory(Guid id, string subCategoryCode, string subCategoryName, Guid categoryId, int sortOrder, string? description, bool isActive, Guid? createdBy)
        : base(id, createdBy)
    {
        SubCategoryCode = subCategoryCode;
        SubCategoryName = subCategoryName;
        CategoryId = categoryId;
        SortOrder = sortOrder;
        Description = description;
        IsActive = isActive;
    }

    public static ProductSubCategory Create(string subCategoryCode, string subCategoryName, Guid categoryId, int sortOrder, string? description, bool isActive, Guid? createdBy)
    {
        Guard.AgainstNullOrWhiteSpace(subCategoryCode, nameof(subCategoryCode));
        Guard.AgainstNullOrWhiteSpace(subCategoryName, nameof(subCategoryName));

        return new ProductSubCategory(Guid.CreateVersion7(), subCategoryCode.Trim().ToUpperInvariant(), subCategoryName.Trim(), categoryId, sortOrder, description?.Trim(), isActive, createdBy);
    }

    public void Update(string subCategoryName, Guid categoryId, int sortOrder, string? description, bool isActive, Guid? updatedBy)
    {
        Guard.AgainstNullOrWhiteSpace(subCategoryName, nameof(subCategoryName));

        SubCategoryName = subCategoryName.Trim();
        CategoryId = categoryId;
        SortOrder = sortOrder;
        Description = description?.Trim();
        IsActive = isActive;
        MarkUpdated(updatedBy);
    }
}
