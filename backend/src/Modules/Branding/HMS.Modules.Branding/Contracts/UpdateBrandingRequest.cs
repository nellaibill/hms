namespace HMS.Modules.Branding.Contracts;

public record UpdateBrandingRequest
{
    public string HospitalName { get; init; } = string.Empty;
    public string AppTitle { get; init; } = string.Empty;
    public string FontFamily { get; init; } = string.Empty;
    public string FontSizeScale { get; init; } = string.Empty;
    public Dictionary<string, string> TokensLight { get; init; } = new();
    public Dictionary<string, string> TokensDark { get; init; } = new();
}
