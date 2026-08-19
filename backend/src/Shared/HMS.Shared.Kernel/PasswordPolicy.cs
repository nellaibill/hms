using System.Text.RegularExpressions;

namespace HMS.Shared.Kernel;

/// <summary>
/// The one password-strength rule every credential-setting path in HMS must apply —
/// hospital Super Admin creation (Platform's CreateHospitalRequestValidator), admin
/// password reset (Identity's SetPasswordRequestValidator), and self-service password
/// change (Identity's ChangePasswordRequestValidator). Centralized here (Kernel has no
/// FluentValidation dependency, so this is a plain regex/const, not a validator) so the
/// three call sites can't drift out of sync with each other.
/// </summary>
public static class PasswordPolicy
{
    public const int MinimumLength = 10;

    public static readonly Regex ComplexityRegex = new(
        @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^a-zA-Z0-9]).+$",
        RegexOptions.Compiled);

    public const string ComplexityMessage =
        "Password must contain at least one uppercase letter, one lowercase letter, one digit, and one special character.";
}
