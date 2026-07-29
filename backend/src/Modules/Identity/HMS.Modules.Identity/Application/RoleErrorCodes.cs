namespace HMS.Modules.Identity.Application;

internal static class RoleErrorCodes
{
    public const string NotFound =
        "ROLES.ROLE_NOT_FOUND";

    public const string DuplicateName =
        "ROLES.ROLE_NAME_DUPLICATE";

    public const string SystemRoleDeletionForbidden =
        "ROLES.SYSTEM_ROLE_DELETE_FORBIDDEN";

    public const string InvalidPermission = 
        "ROLES.INVALID_PERMISSION";
}