using HMS.Modules.Masters.Contracts;
using HMS.Modules.Masters.Domain;

namespace HMS.Modules.Masters.Application.Mapping;

internal static class CurrencyMappingExtensions
{
    public static CurrencyResponse ToResponse(this Currency currency) => new()
    {
        Id = currency.Id,
        CurrencyCode = currency.CurrencyCode,
        CurrencyName = currency.CurrencyName,
        Symbol = currency.Symbol,
        DecimalPlaces = currency.DecimalPlaces,
        IsActive = currency.IsActive,
        CreatedAt = currency.CreatedAt,
        UpdatedAt = currency.UpdatedAt,
    };
}
