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
    public string LoginType { get; init; } = string.Empty;
    public string? ProfilePhotoUrl { get; init; }
    public IReadOnlyList<string> PermissionKeys { get; init; } = [];

    /// <summary>True when this user's current password was set by someone else (an admin
    /// reset, or the initial password Platform Admin chose during hospital registration) —
    /// see User.MustChangePassword's own doc comment. The frontend forces a change-password
    /// screen before letting the user reach the app when this is true.</summary>
    public bool MustChangePassword { get; init; }
}
