namespace HMS.Modules.Documents.Application;

/// <summary>
/// The authenticated caller, as read from the validated JWT's "UserId"/"LoginType" claims
/// (HMS.Modules.Identity.Infrastructure.JwtTokenGenerator). "LoginType" — not the freeform,
/// admin-creatable "RoleName" claim — is used here because it's the closed, stable set
/// (superAdmin/admin/receptionist/doctor/nurse/labTechnician/radiologist/pharmacist/hr/
/// accounts, see HMS.Modules.Identity.Application.LoginTypes) that
/// Application.Security.DocumentAccessPolicy's role-to-owner-type map is keyed against;
/// "RoleName" can be any hospital-specific string an admin typed in and can't drive a
/// hardcoded policy table safely.
///
/// Documents is the first module to derive a real actor from claims rather than passing
/// <c>actorId: null</c> the way every other module's controllers currently do — see
/// docs/modules/Documents/DocumentManagement.md for why RBAC couldn't be deferred here.
/// </summary>
public record DocumentActor(Guid? UserId, string? LoginType)
{
    public static readonly DocumentActor Anonymous = new(null, null);
}
