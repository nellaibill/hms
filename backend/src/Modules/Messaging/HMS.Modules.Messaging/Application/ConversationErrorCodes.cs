namespace HMS.Modules.Messaging.Application;

/// <summary>
/// Stable, machine-readable error codes for expected Messaging-module failures, per
/// docs/ApiStandards.md §5 — the UI branches on these, not on the message text.
/// </summary>
internal static class ConversationErrorCodes
{
    /// <summary>Returned for a caller who isn't a participant, *and* for a conversation id
    /// that doesn't exist at all — a non-participant's lookup against
    /// ConversationParticipants finds no row either way, so both cases collapse into the
    /// same check and the same 403. This is a stricter privacy property than
    /// HMS.Modules.Notifications' equivalent choice: a caller can't even tell whether a
    /// given conversation id is valid, not just whether it's theirs.</summary>
    public const string NotParticipant = "MESSAGING.NOT_A_PARTICIPANT";

    public const string InvalidParticipants = "MESSAGING.INVALID_PARTICIPANTS";
}
