namespace HMS.Modules.Branding.Application;

/// <summary>
/// Stable, machine-readable error codes for expected Branding-module failures, per
/// docs/ApiStandards.md §5 — the UI branches on these, not on the message text.
/// </summary>
internal static class BrandingErrorCodes
{
    public const string InvalidFontFamily = "BRANDING.INVALID_FONT_FAMILY";
    public const string InvalidFontSizeScale = "BRANDING.INVALID_FONT_SIZE_SCALE";
    public const string InvalidIconSizeScale = "BRANDING.INVALID_ICON_SIZE_SCALE";
    public const string InvalidFile = "BRANDING.INVALID_FILE";
    public const string InvalidLogoSlot = "BRANDING.INVALID_LOGO_SLOT";
    public const string InvalidLogoDisplay = "BRANDING.INVALID_LOGO_DISPLAY";
}
