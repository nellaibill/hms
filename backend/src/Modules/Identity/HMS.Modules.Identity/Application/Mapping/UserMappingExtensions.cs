using HMS.Modules.Identity.Contracts;
using HMS.Modules.Identity.Domain;

namespace HMS.Modules.Identity.Application.Mapping;

/// <summary>
/// Manual entity-to-DTO mapping. A single entity doesn't justify a mapping library
/// (Mapster/AutoMapper) at MVP scale — see docs/DecisionLog.md.
/// </summary>
internal static class UserMappingExtensions
{
    public static UserResponse ToResponse(this User user) => new()
    {
        Id = user.Id,
        FirstName = user.FirstName,
        LastName = user.LastName,
        Email = user.Email,
        PhoneNumber = user.PhoneNumber,
        IsActive = user.IsActive,
        CreatedAt = user.CreatedAt,
        UpdatedAt = user.UpdatedAt,
    };
}
