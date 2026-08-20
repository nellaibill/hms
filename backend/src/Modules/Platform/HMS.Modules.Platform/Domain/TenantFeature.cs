using HMS.Shared.Kernel;

namespace HMS.Modules.Platform.Domain;

/// <summary>
/// One row per (tenant, FeatureCatalog key) — the Platform-DB source of truth for which
/// schema-level modules a hospital tenant actually has. Distinct from Tenant.EnabledModules
/// (an RBAC permission-category list governing what a user can *do*): this governs whether a
/// module's database schema is provisioned/reachable at all. Toggling a feature off never
/// deletes this row or any tenant-database schema/data — see TenantFeatureService.
/// </summary>
internal class TenantFeature : Entity
{
    public Guid TenantId { get; private set; }
    public string FeatureKey { get; private set; } = null!;
    public bool IsEnabled { get; private set; }

    // Required by EF Core materialization.
    private TenantFeature()
    {
    }

    private TenantFeature(Guid id, Guid tenantId, string featureKey, bool isEnabled, Guid? createdBy)
        : base(id, createdBy)
    {
        TenantId = tenantId;
        FeatureKey = featureKey;
        IsEnabled = isEnabled;
    }

    public static TenantFeature Create(Guid tenantId, string featureKey, bool isEnabled, Guid? createdBy)
    {
        Guard.AgainstNullOrWhiteSpace(featureKey, nameof(featureKey));
        return new TenantFeature(Guid.CreateVersion7(), tenantId, featureKey.Trim(), isEnabled, createdBy);
    }

    public void SetEnabled(bool isEnabled, Guid? updatedBy)
    {
        IsEnabled = isEnabled;
        MarkUpdated(updatedBy);
    }
}
