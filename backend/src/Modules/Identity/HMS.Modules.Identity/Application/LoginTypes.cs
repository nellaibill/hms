namespace HMS.Modules.Identity.Application;

/// <summary>
/// The web login page's fixed "Sign In As" dropdown values
/// (frontend/web/src/features/auth/mockUsers.ts's roleDefinitions) — a closed set that is
/// NOT the same thing as the Roles module's freeform, admin-creatable Role names (no roles
/// are seeded by default; an admin creates them by hand). Business rule #4 ("the user's
/// assigned role matches the selected Login Type") needs some translation between the two,
/// so this maps each dropdown value to the Role display name a Super Admin is expected to
/// create for it — a small, explicit, in-code table rather than a new schema field, per the
/// task's own "for now" scoping. Compared case-insensitively against Role.Name.
/// </summary>
internal static class LoginTypes
{
    public static readonly IReadOnlyDictionary<string, string> RoleNameByLoginType =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["superAdmin"] = "Super Admin",
            ["admin"] = "Hospital Administrator",
            ["receptionist"] = "Receptionist",
            ["doctor"] = "Doctor / Consultant",
            ["nurse"] = "Nurse",
            ["labTechnician"] = "Lab Technician",
            ["radiologist"] = "Radiologist",
            ["pharmacist"] = "Pharmacist",
            ["hr"] = "HR Officer",
            ["accounts"] = "Accounts Officer",
        };

    public static bool IsKnown(string? loginType) =>
        !string.IsNullOrWhiteSpace(loginType) && RoleNameByLoginType.ContainsKey(loginType);

    public static bool RoleMatches(string loginType, string roleName) =>
        RoleNameByLoginType.TryGetValue(loginType, out var expectedRoleName) &&
        string.Equals(expectedRoleName, roleName, StringComparison.OrdinalIgnoreCase);
}
