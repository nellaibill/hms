namespace HMS.Modules.Identity.Contracts;

/// <summary>
/// LoginType is the web login page's "Sign In As" dropdown value (e.g. "doctor",
/// "superAdmin") — a fixed set unrelated to the freeform Role names created via the Roles
/// module. AuthenticationService maps it to an expected Role name; see LoginTypes.cs.
/// </summary>
public record LoginRequest
{
    public string LoginType { get; init; } = string.Empty;
    public string Username { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
}
