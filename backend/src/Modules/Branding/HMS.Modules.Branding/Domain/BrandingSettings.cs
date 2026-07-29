using HMS.Shared.Kernel;

namespace HMS.Modules.Branding.Domain;

/// <summary>
/// The hospital's current theme/branding configuration. Single-tenant-per-deployment
/// (see the Theme &amp; Branding plan) — this table holds exactly one row, identified by
/// <see cref="SingletonId"/>, rather than one row per hospital/tenant.
/// </summary>
internal class BrandingSettings : Entity
{
    /// <summary>
    /// Fixed, well-known id for the single row this table ever holds. Not a per-insert
    /// <c>Guid.CreateVersion7()</c> like every other module's entities — there is
    /// intentionally only ever one row, so <see cref="Application.BrandingService"/>
    /// always queries/creates by this exact id (see its GetOrCreateAsync).
    /// </summary>
    public static readonly Guid SingletonId = Guid.Parse("00000000-0000-0000-0000-000000000001");

    public string HospitalName { get; private set; } = null!;
    public string AppTitle { get; private set; } = null!;
    public string? LogoPath { get; private set; }
    public string FontFamily { get; private set; } = null!;
    public string FontSizeScale { get; private set; } = null!;

    /// <summary>Flat "--css-var-name": "H S% L%" map, serialized as JSON — see docs on the token-map storage decision.</summary>
    public string TokensLightJson { get; private set; } = null!;

    /// <summary>Same shape as <see cref="TokensLightJson"/>, applied when the UI is in dark mode.</summary>
    public string TokensDarkJson { get; private set; } = null!;

    // Required by EF Core materialization.
    private BrandingSettings()
    {
    }

    private BrandingSettings(
        string hospitalName,
        string appTitle,
        string fontFamily,
        string fontSizeScale,
        string tokensLightJson,
        string tokensDarkJson)
        : base(SingletonId, createdBy: null)
    {
        HospitalName = hospitalName;
        AppTitle = appTitle;
        FontFamily = fontFamily;
        FontSizeScale = fontSizeScale;
        TokensLightJson = tokensLightJson;
        TokensDarkJson = tokensDarkJson;
    }

    /// <summary>
    /// Creates the one-and-only row, seeded from <see cref="Application.BrandingDefaults"/>
    /// so the app looks byte-identical to its pre-feature static config until an admin
    /// actually edits the theme.
    /// </summary>
    public static BrandingSettings CreateDefault(
        string hospitalName,
        string appTitle,
        string fontFamily,
        string fontSizeScale,
        string tokensLightJson,
        string tokensDarkJson)
    {
        Guard.AgainstNullOrWhiteSpace(hospitalName, nameof(hospitalName));
        Guard.AgainstNullOrWhiteSpace(appTitle, nameof(appTitle));
        Guard.AgainstNullOrWhiteSpace(fontFamily, nameof(fontFamily));
        Guard.AgainstNullOrWhiteSpace(fontSizeScale, nameof(fontSizeScale));
        Guard.AgainstNullOrWhiteSpace(tokensLightJson, nameof(tokensLightJson));
        Guard.AgainstNullOrWhiteSpace(tokensDarkJson, nameof(tokensDarkJson));

        return new BrandingSettings(hospitalName.Trim(), appTitle.Trim(), fontFamily, fontSizeScale, tokensLightJson, tokensDarkJson);
    }

    public void UpdateIdentity(string hospitalName, string appTitle, Guid? updatedBy)
    {
        Guard.AgainstNullOrWhiteSpace(hospitalName, nameof(hospitalName));
        Guard.AgainstNullOrWhiteSpace(appTitle, nameof(appTitle));

        HospitalName = hospitalName.Trim();
        AppTitle = appTitle.Trim();
        MarkUpdated(updatedBy);
    }

    public void UpdateTypography(string fontFamily, string fontSizeScale, Guid? updatedBy)
    {
        Guard.AgainstNullOrWhiteSpace(fontFamily, nameof(fontFamily));
        Guard.AgainstNullOrWhiteSpace(fontSizeScale, nameof(fontSizeScale));

        FontFamily = fontFamily;
        FontSizeScale = fontSizeScale;
        MarkUpdated(updatedBy);
    }

    public void UpdateTokens(string tokensLightJson, string tokensDarkJson, Guid? updatedBy)
    {
        Guard.AgainstNullOrWhiteSpace(tokensLightJson, nameof(tokensLightJson));
        Guard.AgainstNullOrWhiteSpace(tokensDarkJson, nameof(tokensDarkJson));

        TokensLightJson = tokensLightJson;
        TokensDarkJson = tokensDarkJson;
        MarkUpdated(updatedBy);
    }

    public void UpdateLogo(string? logoPath, Guid? updatedBy)
    {
        LogoPath = logoPath;
        MarkUpdated(updatedBy);
    }
}
