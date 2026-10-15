namespace HMS.Modules.Identity.Application;

/// <summary>
/// Stable, machine-readable error codes for expected Authentication-module failures, per
/// docs/ApiStandards.md §5. InvalidLogin covers every failure that happens before the password
/// is proven correct (unknown user, wrong password, no password set yet), so the API never
/// confirms which usernames exist. AccountLocked is the one exception, see ADR-099. A failure
/// after a correct password (inactive account, wrong "Sign in as") gets its own code and an
/// exact message: whoever sees it already knows the password, so nothing is given away.
/// </summary>
internal static class AuthenticationErrorCodes
{
    public const string InvalidLogin = "IDENTITY.INVALID_LOGIN";

    /// <summary>Five wrong passwords in a row locked the account; the message says for how long.</summary>
    public const string AccountLocked = "IDENTITY.ACCOUNT_LOCKED";

    /// <summary>Correct password, but an administrator deactivated the account.</summary>
    public const string AccountInactive = "IDENTITY.ACCOUNT_INACTIVE";

    /// <summary>Correct password, but "Sign in as" doesn't match the account's role.</summary>
    public const string LoginTypeMismatch = "IDENTITY.LOGIN_TYPE_MISMATCH";

    /// <summary>ChangePasswordAsync's one failure reason — unlike login, revealing this is
    /// safe: the caller is already authenticated, so there's no username-enumeration risk.</summary>
    public const string InvalidCurrentPassword = "IDENTITY.INVALID_CURRENT_PASSWORD";
}
