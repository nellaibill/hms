using HMS.Modules.Masters.Contracts;
using HMS.Modules.Masters.Domain;

namespace HMS.Modules.Masters.Application.Mapping;

internal static class StockAdjustmentReasonMappingExtensions
{
    public static StockAdjustmentReasonResponse ToResponse(this StockAdjustmentReason reason) => new()
    {
        Id = reason.Id,
        ReasonCode = reason.ReasonCode,
        ReasonName = reason.ReasonName,
        AffectsValuation = reason.AffectsValuation,
        Description = reason.Description,
        IsActive = reason.IsActive,
        CreatedAt = reason.CreatedAt,
        UpdatedAt = reason.UpdatedAt,
    };
}
