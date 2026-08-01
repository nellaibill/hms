namespace HMS.Modules.Products.Application;

/// <summary>
/// Stable, machine-readable error codes for expected Products-module failures, per
/// docs/ApiStandards.md §5 — the UI branches on these, not on the message text. One shared
/// class for all 8 entities (mirrors HMS.Modules.Masters.MastersErrorCodes) since every
/// entity only ever fails for the same handful of reasons: not found, a duplicate business
/// key, or a reference (own-schema or into masters.*) that doesn't exist.
/// </summary>
internal static class ProductsErrorCodes
{
    public const string NotFound = "PRODUCTS.NOT_FOUND";
    public const string DuplicateCode = "PRODUCTS.DUPLICATE_CODE";
    public const string InvalidReference = "PRODUCTS.INVALID_REFERENCE";
}
