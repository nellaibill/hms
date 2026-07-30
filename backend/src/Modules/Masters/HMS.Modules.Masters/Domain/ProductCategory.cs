using HMS.Shared.Kernel;

namespace HMS.Modules.Masters.Domain;

/// <summary>
/// Top-level product classification (docs/03_Masters_ERD, "Product Classification"), e.g.
/// Pharmacy, Consumables, Equipment. <see cref="ParentId"/> optionally self-references
/// another <see cref="ProductCategory"/> for a shallow tree — modeled as a plain FK (no
/// navigation property) since categories are independent aggregates, not a single
/// aggregate's owned collection (contrast with Patient/PatientRegistration).
/// </summary>
internal class ProductCategory : Entity
{
    public string CategoryCode { get; private set; } = null!;
    public string CategoryName { get; private set; } = null!;
    public Guid? ParentId { get; private set; }
    public int SortOrder { get; private set; }
    public string? Description { get; private set; }

    public bool IsActive { get; private set; } = true;

    private ProductCategory()
    {
    }

    private ProductCategory(Guid id, string categoryCode, string categoryName, Guid? parentId, int sortOrder, string? description, bool isActive, Guid? createdBy)
        : base(id, createdBy)
    {
        CategoryCode = categoryCode;
        CategoryName = categoryName;
        ParentId = parentId;
        SortOrder = sortOrder;
        Description = description;
        IsActive = isActive;
    }

    public static ProductCategory Create(string categoryCode, string categoryName, Guid? parentId, int sortOrder, string? description, bool isActive, Guid? createdBy)
    {
        Guard.AgainstNullOrWhiteSpace(categoryCode, nameof(categoryCode));
        Guard.AgainstNullOrWhiteSpace(categoryName, nameof(categoryName));

        return new ProductCategory(Guid.CreateVersion7(), categoryCode.Trim().ToUpperInvariant(), categoryName.Trim(), parentId, sortOrder, description?.Trim(), isActive, createdBy);
    }

    public void Update(string categoryName, Guid? parentId, int sortOrder, string? description, bool isActive, Guid? updatedBy)
    {
        Guard.AgainstNullOrWhiteSpace(categoryName, nameof(categoryName));

        CategoryName = categoryName.Trim();
        ParentId = parentId;
        SortOrder = sortOrder;
        Description = description?.Trim();
        IsActive = isActive;
        MarkUpdated(updatedBy);
    }
}
