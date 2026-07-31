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
}
