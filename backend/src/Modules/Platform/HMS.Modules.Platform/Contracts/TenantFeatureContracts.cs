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
}

public record UpdateTenantFeaturesRequest
{
    public IReadOnlyList<string> EnabledFeatures { get; init; } = [];
}
