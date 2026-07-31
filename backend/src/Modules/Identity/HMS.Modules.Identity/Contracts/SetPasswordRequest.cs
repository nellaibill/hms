namespace HMS.Modules.Identity.Contracts;

/// <summary>
/// Admin-sets/resets a user's password. Not part of CreateUserRequest — user creation has
/// no credential step today, so this is the only way a PasswordHash ever gets set,
/// making login actually usable.
/// </summary>
public record SetPasswordRequest
{
    public string Password { get; init; } = string.Empty;
}
