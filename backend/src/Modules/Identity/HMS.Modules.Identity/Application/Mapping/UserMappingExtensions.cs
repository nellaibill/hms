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
    //
    // roleName is passed in explicitly rather than read from user.Role — that navigation
    // is only ever populated by EF's own fixup, which is unreliable right after ChangeRole
    // (the old Role object stays attached until reloaded) and never populated at all for a
    // User built directly via User.Create (e.g. in unit tests). UserService always has the
    // authoritative Role in hand already, so it passes the name straight through.
    public static UserResponse ToResponse(this User user, string roleName) => new()
    {
        Id = user.Id,
        Username = user.Username,
        FirstName = user.FirstName,
        LastName = user.LastName,
        Email = user.Email,
        PhoneNumber = user.PhoneNumber,
        ProfilePhotoUrl = user.ProfilePhotoUrl,
        RoleId = user.RoleId,
        RoleName = roleName,
        EmailVerified = user.EmailVerified,
        LastLoginAt = user.LastLoginAt,
        IsActive = user.IsActive,
        CreatedAt = user.CreatedAt,
        UpdatedAt = user.UpdatedAt,
    };
}
