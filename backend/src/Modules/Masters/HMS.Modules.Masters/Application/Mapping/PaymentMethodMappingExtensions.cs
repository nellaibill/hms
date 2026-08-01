using HMS.Modules.Masters.Contracts;
using HMS.Modules.Masters.Domain;

namespace HMS.Modules.Masters.Application.Mapping;

internal static class PaymentMethodMappingExtensions
{
    public static PaymentMethodResponse ToResponse(this PaymentMethod method) => new()
    {
        Id = method.Id,
        MethodCode = method.MethodCode,
        MethodName = method.MethodName,
        Description = method.Description,
        IsActive = method.IsActive,
        CreatedAt = method.CreatedAt,
        UpdatedAt = method.UpdatedAt,
    };
}
