namespace HMS.Modules.Masters.Application;

/// <summary>
/// Stable, machine-readable error codes for expected Masters-module failures, per
/// docs/ApiStandards.md §5 — the UI branches on these, not on the message text. One shared
/// class for all ~16 entities (rather than a per-entity {Entity}ErrorCodes class like
/// Patients/Branding) since every entity only ever fails for the same two reasons: not
/// found, or a duplicate business code/name.
/// </summary>
internal static class MastersErrorCodes
{
    public const string NotFound = "MASTERS.NOT_FOUND";
    public const string DuplicateCode = "MASTERS.DUPLICATE_CODE";
    public const string InvalidReference = "MASTERS.INVALID_REFERENCE";

    /// <summary>DiagnosticService.CategoryId doesn't reference an existing DiagnosticCategory.</summary>
    public const string InvalidCategory = "MASTERS.INVALID_CATEGORY";

    /// <summary>DiagnosticService.ProviderId doesn't reference an existing DiagnosticProvider
    /// (only checked when IsOutsourced is true).</summary>
    public const string InvalidProvider = "MASTERS.INVALID_PROVIDER";

    /// <summary>A DiagnosticPackageItem's ServiceId doesn't reference an existing
    /// DiagnosticService.</summary>
    public const string InvalidPackageItemService = "MASTERS.INVALID_PACKAGE_ITEM_SERVICE";
}
