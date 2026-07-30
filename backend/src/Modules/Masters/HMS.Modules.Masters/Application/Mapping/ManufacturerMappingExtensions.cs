using HMS.Modules.Masters.Contracts;
using HMS.Modules.Masters.Domain;

namespace HMS.Modules.Masters.Application.Mapping;

internal static class ManufacturerMappingExtensions
{
    public static ManufacturerResponse ToResponse(this Manufacturer manufacturer) => new()
    {
        Id = manufacturer.Id,
        ManufacturerCode = manufacturer.ManufacturerCode,
        ManufacturerName = manufacturer.ManufacturerName,
        ContactPerson = manufacturer.ContactPerson,
        Phone = manufacturer.Phone,
        Email = manufacturer.Email,
        Country = manufacturer.Country,
        IsActive = manufacturer.IsActive,
        CreatedAt = manufacturer.CreatedAt,
        UpdatedAt = manufacturer.UpdatedAt,
    };
}
