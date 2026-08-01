using HMS.Modules.Masters.Contracts;
using HMS.Modules.Masters.Domain;

namespace HMS.Modules.Masters.Application.Mapping;

internal static class TaxMappingExtensions
{
    public static TaxResponse ToResponse(this Tax tax) => new()
    {
        Id = tax.Id,
        TaxCode = tax.TaxCode,
        TaxName = tax.TaxName,
        TaxType = tax.TaxType,
        RatePercent = tax.RatePercent,
        IsInclusive = tax.IsInclusive,
        IsActive = tax.IsActive,
        CreatedAt = tax.CreatedAt,
        UpdatedAt = tax.UpdatedAt,
    };
}
