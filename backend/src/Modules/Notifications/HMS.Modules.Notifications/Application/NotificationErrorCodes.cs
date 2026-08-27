namespace HMS.Modules.Notifications.Application;

/// <summary>
/// Stable, machine-readable error codes for expected Notifications-module failures, per
/// docs/ApiStandards.md §5 — the UI branches on these, not on the message text.
/// </summary>
internal static class NotificationErrorCodes
{
    /// <summary>Covers both "no such NotificationRecipient row" and "the row exists but
    /// belongs to someone else" — deliberately not distinguished (no separate Forbidden
    /// code) so a caller can't use this endpoint to probe whether a given id belongs to
    /// another user, same reasoning as AuthenticationService's generic login-failure
    /// message.</summary>
    public const string NotFound = "NOTIFICATIONS.NOTIFICATION_NOT_FOUND";

    public const string NoRecipients = "NOTIFICATIONS.NO_RECIPIENTS";

    /// <summary>NotifyAsync's Body was omitted but no active InApp template exists for the
    /// given TemplateKey — see NotifyRequest.Body's own doc comment.</summary>
    public const string TemplateNotFound = "NOTIFICATIONS.TEMPLATE_NOT_FOUND";

    public const string DuplicateTemplate = "NOTIFICATIONS.TEMPLATE_DUPLICATE";
    public const string EmailTemplateRequiresSubject = "NOTIFICATIONS.TEMPLATE_EMAIL_REQUIRES_SUBJECT";
}
