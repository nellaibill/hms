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
    /// <summary>Shown alongside HospitalName on printed/exported clinical documents (e.g. the
    /// OPD Consultation report) — optional, unlike HospitalName/AppTitle.</summary>
    public string? Address { get; private set; }
    public string? PhoneNumber { get; private set; }
    /// <summary>The Primary logo (BrandingLogoSlots.Primary) — the original single hospital logo.</summary>
    public string? LogoPath { get; private set; }
    public string? CompactLogoPath { get; private set; }
    public string? LoginLogoPath { get; private set; }
    public string? PrintLogoPath { get; private set; }
    public string? FaviconPath { get; private set; }

    /// <summary>Serialized Contracts.LogoDisplaySettings; null until an admin first saves one
    /// (the mapping then returns the defaults).</summary>
    public string? LogoDisplayJson { get; private set; }
    public string FontFamily { get; private set; } = null!;
    public string FontSizeScale { get; private set; } = null!;
    public string IconSizeScale { get; private set; } = null!;

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
        string iconSizeScale,
        string tokensLightJson,
        string tokensDarkJson)
        : base(SingletonId, createdBy: null)
    {
        HospitalName = hospitalName;
        AppTitle = appTitle;
        FontFamily = fontFamily;
        FontSizeScale = fontSizeScale;
        IconSizeScale = iconSizeScale;
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
        string iconSizeScale,
        string tokensLightJson,
        string tokensDarkJson)
    {
        Guard.AgainstNullOrWhiteSpace(hospitalName, nameof(hospitalName));
        Guard.AgainstNullOrWhiteSpace(appTitle, nameof(appTitle));
        Guard.AgainstNullOrWhiteSpace(fontFamily, nameof(fontFamily));
        Guard.AgainstNullOrWhiteSpace(fontSizeScale, nameof(fontSizeScale));
        Guard.AgainstNullOrWhiteSpace(iconSizeScale, nameof(iconSizeScale));
        Guard.AgainstNullOrWhiteSpace(tokensLightJson, nameof(tokensLightJson));
        Guard.AgainstNullOrWhiteSpace(tokensDarkJson, nameof(tokensDarkJson));

        return new BrandingSettings(hospitalName.Trim(), appTitle.Trim(), fontFamily, fontSizeScale, iconSizeScale, tokensLightJson, tokensDarkJson);
    }

    public void UpdateIdentity(string hospitalName, string appTitle, string? address, string? phoneNumber, Guid? updatedBy)
    {
        Guard.AgainstNullOrWhiteSpace(hospitalName, nameof(hospitalName));
        Guard.AgainstNullOrWhiteSpace(appTitle, nameof(appTitle));

        HospitalName = hospitalName.Trim();
        AppTitle = appTitle.Trim();
        Address = string.IsNullOrWhiteSpace(address) ? null : address.Trim();
        PhoneNumber = string.IsNullOrWhiteSpace(phoneNumber) ? null : phoneNumber.Trim();
        MarkUpdated(updatedBy);
    }

    public void UpdateTypography(string fontFamily, string fontSizeScale, string iconSizeScale, Guid? updatedBy)
    {
        Guard.AgainstNullOrWhiteSpace(fontFamily, nameof(fontFamily));
        Guard.AgainstNullOrWhiteSpace(fontSizeScale, nameof(fontSizeScale));
        Guard.AgainstNullOrWhiteSpace(iconSizeScale, nameof(iconSizeScale));

        FontFamily = fontFamily;
        FontSizeScale = fontSizeScale;
        IconSizeScale = iconSizeScale;
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

    /// <summary>The stored path for one logo slot. <paramref name="slot"/> must already be
    /// validated against Contracts.BrandingLogoSlots.</summary>
    public string? GetLogoPath(string slot) => slot switch
    {
        Contracts.BrandingLogoSlots.Primary => LogoPath,
        Contracts.BrandingLogoSlots.Compact => CompactLogoPath,
        Contracts.BrandingLogoSlots.Login => LoginLogoPath,
        Contracts.BrandingLogoSlots.Print => PrintLogoPath,
        Contracts.BrandingLogoSlots.Favicon => FaviconPath,
        _ => throw new ArgumentOutOfRangeException(nameof(slot), slot, "Unknown logo slot."),
    };

    /// <summary>Sets (or, with a null path, clears) one logo slot. <paramref name="slot"/> must
    /// already be validated against Contracts.BrandingLogoSlots.</summary>
    public void UpdateLogo(string slot, string? logoPath, Guid? updatedBy)
    {
        switch (slot)
        {
            case Contracts.BrandingLogoSlots.Primary: LogoPath = logoPath; break;
            case Contracts.BrandingLogoSlots.Compact: CompactLogoPath = logoPath; break;
            case Contracts.BrandingLogoSlots.Login: LoginLogoPath = logoPath; break;
            case Contracts.BrandingLogoSlots.Print: PrintLogoPath = logoPath; break;
            case Contracts.BrandingLogoSlots.Favicon: FaviconPath = logoPath; break;
            default: throw new ArgumentOutOfRangeException(nameof(slot), slot, "Unknown logo slot.");
        }

        MarkUpdated(updatedBy);
    }

    public void UpdateLogoDisplay(string logoDisplayJson, Guid? updatedBy)
    {
        Guard.AgainstNullOrWhiteSpace(logoDisplayJson, nameof(logoDisplayJson));

        LogoDisplayJson = logoDisplayJson;
        MarkUpdated(updatedBy);
    }
}
