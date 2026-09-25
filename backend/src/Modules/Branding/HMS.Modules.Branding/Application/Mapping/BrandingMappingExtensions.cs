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
        Address = settings.Address,
        PhoneNumber = settings.PhoneNumber,
        LogoUrl = settings.LogoPath,
        CompactLogoUrl = settings.CompactLogoPath,
        LoginLogoUrl = settings.LoginLogoPath,
        PrintLogoUrl = settings.PrintLogoPath,
        FaviconUrl = settings.FaviconPath,
        LogoDisplay = string.IsNullOrWhiteSpace(settings.LogoDisplayJson)
            ? new LogoDisplaySettings()
            : JsonSerializer.Deserialize<LogoDisplaySettings>(settings.LogoDisplayJson) ?? new LogoDisplaySettings(),
        FontFamily = settings.FontFamily,
        FontSizeScale = settings.FontSizeScale,
        IconSizeScale = settings.IconSizeScale,
        TokensLight = JsonSerializer.Deserialize<Dictionary<string, string>>(settings.TokensLightJson) ?? new Dictionary<string, string>(),
        TokensDark = JsonSerializer.Deserialize<Dictionary<string, string>>(settings.TokensDarkJson) ?? new Dictionary<string, string>(),
    };
}
