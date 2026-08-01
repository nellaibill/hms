using HMS.Modules.Masters.Contracts;
using HMS.Modules.Masters.Domain;

namespace HMS.Modules.Masters.Application.Mapping;

internal static class CustomerMappingExtensions
{
    public static CustomerResponse ToResponse(this Customer customer) => new()
    {
        Id = customer.Id,
        CustomerCode = customer.CustomerCode,
        CustomerName = customer.CustomerName,
        ContactPerson = customer.ContactPerson,
        Phone = customer.Phone,
        Email = customer.Email,
        Country = customer.Country,
        PaymentTermId = customer.PaymentTermId,
        IsActive = customer.IsActive,
        CreatedAt = customer.CreatedAt,
        UpdatedAt = customer.UpdatedAt,
    };
}
