using HMS.Modules.Masters.Contracts;
using HMS.Modules.Masters.Domain;

namespace HMS.Modules.Masters.Application.Mapping;

internal static class SupplierMappingExtensions
{
    public static SupplierResponse ToResponse(this Supplier supplier) => new()
    {
        Id = supplier.Id,
        SupplierCode = supplier.SupplierCode,
        SupplierName = supplier.SupplierName,
        ContactPerson = supplier.ContactPerson,
        Phone = supplier.Phone,
        Email = supplier.Email,
        TaxId = supplier.TaxId,
        Country = supplier.Country,
        PaymentTermId = supplier.PaymentTermId,
        IsActive = supplier.IsActive,
        CreatedAt = supplier.CreatedAt,
        UpdatedAt = supplier.UpdatedAt,
    };
}
