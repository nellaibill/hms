using HMS.Shared.Kernel;

namespace HMS.Modules.Masters.Domain;

/// <summary>Lookup reason for manual stock adjustments (docs/03_Masters_ERD, "Inventory Lookup"), e.g. Damage, Expiry, Recount.</summary>
internal class StockAdjustmentReason : Entity
{
    public string ReasonCode { get; private set; } = null!;
    public string ReasonName { get; private set; } = null!;
    public bool AffectsValuation { get; private set; }
    public string? Description { get; private set; }

    public bool IsActive { get; private set; } = true;

    private StockAdjustmentReason()
    {
    }

    private StockAdjustmentReason(Guid id, string reasonCode, string reasonName, bool affectsValuation, string? description, bool isActive, Guid? createdBy)
        : base(id, createdBy)
    {
        ReasonCode = reasonCode;
        ReasonName = reasonName;
        AffectsValuation = affectsValuation;
        Description = description;
        IsActive = isActive;
    }

    public static StockAdjustmentReason Create(string reasonCode, string reasonName, bool affectsValuation, string? description, bool isActive, Guid? createdBy)
    {
        Guard.AgainstNullOrWhiteSpace(reasonCode, nameof(reasonCode));
        Guard.AgainstNullOrWhiteSpace(reasonName, nameof(reasonName));

        return new StockAdjustmentReason(Guid.CreateVersion7(), reasonCode.Trim().ToUpperInvariant(), reasonName.Trim(), affectsValuation, description?.Trim(), isActive, createdBy);
    }

    public void Update(string reasonName, bool affectsValuation, string? description, bool isActive, Guid? updatedBy)
    {
        Guard.AgainstNullOrWhiteSpace(reasonName, nameof(reasonName));

        ReasonName = reasonName.Trim();
        AffectsValuation = affectsValuation;
        Description = description?.Trim();
        IsActive = isActive;
        MarkUpdated(updatedBy);
    }
}
