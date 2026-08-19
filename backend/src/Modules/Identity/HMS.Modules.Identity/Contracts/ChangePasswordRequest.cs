namespace HMS.Modules.Identity.Contracts;

/// <summary>
/// Self-service password change — the caller changes their own password by proving they
/// know the current one. Distinct from <see cref="SetPasswordRequest"/> (an admin resetting
/// someone else's password without knowing their current one).
/// </summary>
public record ChangePasswordRequest
{
    public string CurrentPassword { get; init; } = string.Empty;
    public string NewPassword { get; init; } = string.Empty;
}
