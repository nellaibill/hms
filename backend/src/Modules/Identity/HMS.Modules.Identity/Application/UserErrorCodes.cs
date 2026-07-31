namespace HMS.Modules.Identity.Application;

/// <summary>
/// Stable, machine-readable error codes for expected Users-module failures, per
/// docs/ApiStandards.md §5 — the UI branches on these, not on the message text.
/// </summary>
internal static class UserErrorCodes
{
    public const string NotFound = "IDENTITY.USER_NOT_FOUND";
    public const string DuplicateEmail = "IDENTITY.USER_EMAIL_DUPLICATE";
    public const string DuplicateUsername = "IDENTITY.USER_USERNAME_DUPLICATE";
    public const string InvalidRole = "IDENTITY.USER_INVALID_ROLE";
    public const string InvalidFile = "IDENTITY.USER_INVALID_FILE";
}
