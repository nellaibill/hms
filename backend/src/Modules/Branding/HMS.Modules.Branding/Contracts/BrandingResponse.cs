namespace HMS.Modules.Branding.Contracts;

public record BrandingResponse
{
    public string HospitalName { get; init; } = string.Empty;
    public string AppTitle { get; init; } = string.Empty;
    /// <summary>Shown alongside HospitalName on printed/exported clinical documents — optional.</summary>
    public string? Address { get; init; }
    public string? PhoneNumber { get; init; }

    /// <summary>Relative static-file URL (served via app.UseStaticFiles()) — null when no custom logo has been uploaded.</summary>
    public string? LogoUrl { get; init; }

    /// <summary>Optional per-surface logos (see BrandingLogoSlots) — null when that slot has no
    /// logo of its own; the frontend then falls back to <see cref="LogoUrl"/> per
    /// <see cref="LogoDisplay"/>'s UsePrimaryAsFallback.</summary>
    public string? CompactLogoUrl { get; init; }
    public string? LoginLogoUrl { get; init; }
    public string? PrintLogoUrl { get; init; }
    public string? FaviconUrl { get; init; }
    public LogoDisplaySettings LogoDisplay { get; init; } = new();

    public string FontFamily { get; init; } = string.Empty;
    public string FontSizeScale { get; init; } = string.Empty;
    public string IconSizeScale { get; init; } = string.Empty;

    public IReadOnlyDictionary<string, string> TokensLight { get; init; } = new Dictionary<string, string>();
    public IReadOnlyDictionary<string, string> TokensDark { get; init; } = new Dictionary<string, string>();
}
