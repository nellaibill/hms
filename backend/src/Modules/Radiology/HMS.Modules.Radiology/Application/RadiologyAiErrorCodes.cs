namespace HMS.Modules.Radiology.Application;

/// <summary>Stable, machine-readable error codes for expected Radiology-AI failures
/// (docs/ApiStandards.md §5). Document lookup/permission failures are passed through with the
/// Documents module's own codes.</summary>
internal static class RadiologyAiErrorCodes
{
    public const string NotConfigured = "RADIOLOGY.AI_NOT_CONFIGURED";
    public const string UnsupportedImage = "RADIOLOGY.UNSUPPORTED_IMAGE";
    public const string ImageTooLarge = "RADIOLOGY.IMAGE_TOO_LARGE";
    public const string RequestFailed = "RADIOLOGY.AI_REQUEST_FAILED";

    // Mirrors HMS.Modules.Documents.Application.DocumentErrorCodes (internal there).
    public const string DocumentNotFound = "DOCUMENTS.DOCUMENT_NOT_FOUND";
    public const string DocumentNotAvailable = "DOCUMENTS.CONTENT_NOT_AVAILABLE";
}
