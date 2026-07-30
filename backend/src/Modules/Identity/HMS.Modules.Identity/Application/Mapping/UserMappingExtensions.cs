using HMS.Modules.Identity.Contracts;
using HMS.Modules.Identity.Domain;

namespace HMS.Modules.Identity.Application.Mapping;

/// <summary>
/// Manual entity-to-DTO mapping. A single entity doesn't justify a mapping library
/// (Mapster/AutoMapper) at MVP scale — see docs/DecisionLog.md.
/// </summary>
internal static class UserMappingExtensions
{
    // PasswordHash is deliberately never mapped here — it must never appear in an API
    // response, no matter what else changes on User or UserResponse.
    public static UserResponse ToResponse(this User user) => new()
    {
        Id = user.Id,
        Username = user.Username,
        FirstName = user.FirstName,
        LastName = user.LastName,
        Email = user.Email,
        PhoneNumber = user.PhoneNumber,
        EmailVerified = user.EmailVerified,
        LastLoginAt = user.LastLoginAt,
        IsActive = user.IsActive,
        CreatedAt = user.CreatedAt,
        UpdatedAt = user.UpdatedAt,
    };
}
