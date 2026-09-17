namespace HMS.Modules.Identity.Contracts;

public record LoginUserResponse
{
    public Guid Id { get; init; }
    public string Username { get; init; } = string.Empty;
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public Guid RoleId { get; init; }
    public string RoleName { get; init; } = string.Empty;

    /// <summary>Optional link to a Masters.Consultant record, independent of RoleName —
    /// see Identity.User.ConsultantId's own doc comment. The frontend uses this to lock the
    /// Department/Consultant filters on clinical list screens to this user's own consultant.</summary>
    public Guid? ConsultantId { get; init; }

    public string LoginType { get; init; } = string.Empty;
    public string? ProfilePhotoUrl { get; init; }
    public IReadOnlyList<string> PermissionKeys { get; init; } = [];

    /// <summary>The FeatureCatalog keys this tenant has enabled (Tenant Feature/Module
    /// Management) — UI/nav-gating convenience only, a login-time snapshot. NEVER the
    /// source of truth for backend authorization: FeatureAuthorizationHandler always checks
    /// live tenant state instead, so a feature disabled mid-session is rejected on the very
    /// next API call regardless of what this list still says.</summary>
    public IReadOnlyList<string> FeatureKeys { get; init; } = [];

    /// <summary>True when this user's current password was set by someone else (an admin
    /// reset, or the initial password Platform Admin chose during hospital registration) —
    /// see User.MustChangePassword's own doc comment. The frontend forces a change-password
    /// screen before letting the user reach the app when this is true.</summary>
    public bool MustChangePassword { get; init; }
}
