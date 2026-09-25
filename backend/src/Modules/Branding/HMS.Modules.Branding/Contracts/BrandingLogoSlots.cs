namespace HMS.Modules.Branding.Contracts;

/// <summary>
/// The logo slots a hospital can configure on Theme &amp; Branding → Identity → Logo
/// Configuration. <see cref="Primary"/> is the original single hospital logo (the
/// <c>logo_path</c> column); the rest are optional per-surface overrides the frontend falls
/// back from to Primary (when <see cref="LogoDisplaySettings.UsePrimaryAsFallback"/> is on),
/// then to the bundled default artwork. Values are the wire format of the <c>slot</c> query
/// parameter on <c>POST/DELETE api/v1/branding/logo</c> — mirror
/// frontend/web/src/features/branding/types.ts's LOGO_SLOTS.
/// </summary>
public static class BrandingLogoSlots
{
    public const string Primary = "primary";
    public const string Compact = "compact";
    public const string Login = "login";
    public const string Print = "print";
    public const string Favicon = "favicon";

    public static readonly IReadOnlyList<string> All = [Primary, Compact, Login, Print, Favicon];

    public static bool IsValid(string? slot) => slot is not null && All.Contains(slot);
}
