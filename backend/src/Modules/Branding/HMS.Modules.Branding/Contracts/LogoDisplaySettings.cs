namespace HMS.Modules.Branding.Contracts;

/// <summary>How one logo slot renders — its box height in CSS px and its object-fit mode.
/// Fit is only ever a non-cropping mode ("contain" or "scale-down"), so no uploaded logo is
/// ever stretched or cut off regardless of its shape.</summary>
public record LogoSlotDisplay
{
    public int Height { get; init; }
    public string Fit { get; init; } = "contain";
}

/// <summary>
/// Per-slot display options for the configured logos (keyed by <see cref="BrandingLogoSlots"/>
/// values), plus whether the Primary logo stands in for any slot that has no logo of its own.
/// Stored as one jsonb column rather than a column per option, same reasoning as the token maps.
/// </summary>
public record LogoDisplaySettings
{
    public bool UsePrimaryAsFallback { get; init; } = true;
    public Dictionary<string, LogoSlotDisplay> Slots { get; init; } = new();
}
