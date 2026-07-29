using System.Text.Json;
using HMS.Modules.Branding.Contracts;
using HMS.Modules.Branding.Domain;

namespace HMS.Modules.Branding.Application.Mapping;

internal static class BrandingMappingExtensions
{
    public static BrandingResponse ToResponse(this BrandingSettings settings) => new()
    {
        HospitalName = settings.HospitalName,
        AppTitle = settings.AppTitle,
        LogoUrl = settings.LogoPath,
        FontFamily = settings.FontFamily,
        FontSizeScale = settings.FontSizeScale,
        TokensLight = JsonSerializer.Deserialize<Dictionary<string, string>>(settings.TokensLightJson) ?? new Dictionary<string, string>(),
        TokensDark = JsonSerializer.Deserialize<Dictionary<string, string>>(settings.TokensDarkJson) ?? new Dictionary<string, string>(),
    };
}
