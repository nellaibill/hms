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

    public string FontFamily { get; init; } = string.Empty;
    public string FontSizeScale { get; init; } = string.Empty;
    public string IconSizeScale { get; init; } = string.Empty;

    public IReadOnlyDictionary<string, string> TokensLight { get; init; } = new Dictionary<string, string>();
    public IReadOnlyDictionary<string, string> TokensDark { get; init; } = new Dictionary<string, string>();
}
