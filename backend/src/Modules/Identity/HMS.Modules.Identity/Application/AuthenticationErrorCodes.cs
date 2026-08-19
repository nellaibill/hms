namespace HMS.Modules.Identity.Application;

/// <summary>
/// Stable, machine-readable error codes for expected Authentication-module failures, per
/// docs/ApiStandards.md §5. InvalidLogin is deliberately the single code used for every
/// login-rejection reason (user not found, wrong password, wrong login type, inactive
/// user) — never revealing which specific check failed is a standard login-security
/// practice, not an oversight.
/// </summary>
internal static class AuthenticationErrorCodes
{
    public const string InvalidLogin = "IDENTITY.INVALID_LOGIN";

    /// <summary>ChangePasswordAsync's one failure reason — unlike login, revealing this is
    /// safe: the caller is already authenticated, so there's no username-enumeration risk.</summary>
    public const string InvalidCurrentPassword = "IDENTITY.INVALID_CURRENT_PASSWORD";
}
