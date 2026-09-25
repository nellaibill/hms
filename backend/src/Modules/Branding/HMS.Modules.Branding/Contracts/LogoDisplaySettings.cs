namespace HMS.Modules.Branding.Contracts;

/// <summary>How one logo slot renders — its box width and height in CSS px (independent, no
/// aspect lock) and its CSS object-fit mode: "contain" (never distorts or crops), "cover"
/// (fills the box, cropping overflow) or "fill" (stretches to the box).</summary>
public record LogoSlotDisplay
{
    public int Width { get; init; }
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
