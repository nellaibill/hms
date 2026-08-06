namespace HMS.Modules.Documents.Application;

/// <summary>
/// Stable, machine-readable error codes for expected Documents-module failures, per
/// docs/ApiStandards.md §5 — the caller branches on these, not on the message text.
/// </summary>
internal static class DocumentErrorCodes
{
    public const string NotFound = "DOCUMENTS.DOCUMENT_NOT_FOUND";
    public const string InvalidFile = "DOCUMENTS.INVALID_FILE";
    public const string OwnerNotFound = "DOCUMENTS.OWNER_NOT_FOUND";
    public const string Forbidden = "DOCUMENTS.FORBIDDEN";
    public const string NotAvailable = "DOCUMENTS.CONTENT_NOT_AVAILABLE";
}
