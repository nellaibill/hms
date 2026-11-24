namespace HMS.Modules.Platform.Contracts;

/// <summary>
/// Which schema-level modules (FeatureCatalog keys) this hospital tenant has — governs
/// whether a module's database schema is provisioned/reachable at all. Distinct from
/// TenantConfigurationResponse (RBAC EnabledModules), which governs what a user can *do*
/// inside modules the tenant already has.
/// </summary>
public record TenantFeaturesResponse
{
    public Guid Id { get; init; }
    public IReadOnlyList<string> EnabledFeatures { get; init; } = [];

    /// <summary>The full feature catalog (FeatureCatalog.All), so the frontend can render a
    /// toggle per feature without hardcoding its own copy of the list.</summary>
    public IReadOnlyList<string> AllFeatures { get; init; } = [];

    /// <summary>Always enabled, never toggleable — the frontend shows these checked and
    /// disabled rather than omitting them entirely.</summary>
    public IReadOnlyList<string> MandatoryFeatures { get; init; } = [];

    /// <summary>FeatureCatalog.Dependencies — feature key to the Optional keys it requires — so
    /// the frontend can auto-enable/disable them instead of hardcoding (and drifting from) its
    /// own copy; the validators still enforce it server-side.</summary>
    public IReadOnlyDictionary<string, IReadOnlyList<string>> Dependencies { get; init; } = new Dictionary<string, IReadOnlyList<string>>();

    /// <summary>The sub-feature catalog (FeatureCatalog.SubFeatures, ADR-145), parent before
    /// child, so the frontend can nest each part under its module without its own copy.</summary>
    public IReadOnlyList<SubFeatureResponse> SubFeatures { get; init; } = [];

    /// <summary>The sub-features this hospital has left on, regardless of whether their parent is
    /// on — so switching a module back on shows its parts exactly as they were.</summary>
    public IReadOnlyList<string> EnabledSubFeatures { get; init; } = [];

    /// <summary>FeatureCatalog.SubFeatureDependencies — sub-feature to the sub-features it needs.</summary>
    public IReadOnlyDictionary<string, IReadOnlyList<string>> SubFeatureDependencies { get; init; } = new Dictionary<string, IReadOnlyList<string>>();
}

public record SubFeatureResponse
{
    public string Key { get; init; } = string.Empty;
    public string Parent { get; init; } = string.Empty;
}

public record UpdateTenantFeaturesRequest
{
    public IReadOnlyList<string> EnabledFeatures { get; init; } = [];

    /// <summary>The sub-features to leave on (ADR-145); every other sub-feature is switched off.
    /// Null leaves the hospital's sub-features exactly as they are (e.g. an older client).</summary>
    public IReadOnlyList<string>? EnabledSubFeatures { get; init; }
}
