namespace HMS.Modules.Identity.Contracts;

/// <summary>
/// User-facing login failure messages. The login page shows the API's message as-is, so these
/// are the words a receptionist or doctor actually reads. Public because HMS.Api's
/// TenantResolutionMiddleware rejects an unknown hospital code before the Identity module runs,
/// and must say exactly what a wrong username or password says.
/// </summary>
public static class LoginMessages
{
    public const int MaxFailedAttempts = 5;
    public const int LockoutMinutes = 15;

    /// <summary>Wrong hospital code, unknown username or wrong password: one message for all
    /// three, so the response never confirms which hospital codes or usernames exist. It
    /// states the lockout rule up front, so a lockout never comes as a surprise.</summary>
    public const string InvalidCredentials =
        "The hospital code, username or password is incorrect. After 5 wrong attempts in a row the account is locked for 15 minutes.";
}
