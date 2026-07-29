namespace HMS.Modules.Branding.Contracts;

public record BrandingResponse
{
    public string HospitalName { get; init; } = string.Empty;
    public string AppTitle { get; init; } = string.Empty;

    /// <summary>Relative static-file URL (served via app.UseStaticFiles()) — null when no custom logo has been uploaded.</summary>
    public string? LogoUrl { get; init; }

    public string FontFamily { get; init; } = string.Empty;
    public string FontSizeScale { get; init; } = string.Empty;

    public IReadOnlyDictionary<string, string> TokensLight { get; init; } = new Dictionary<string, string>();
    public IReadOnlyDictionary<string, string> TokensDark { get; init; } = new Dictionary<string, string>();
}
