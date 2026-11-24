using HMS.Modules.Platform.Application.Abstractions;
using HMS.Modules.Platform.Contracts;
using HMS.Modules.Platform.Domain;
using HMS.Shared.Kernel;
using Microsoft.Extensions.Logging;

namespace HMS.Modules.Platform.Application;

/// <summary>
/// Owns platform.tenant_features — the schema-level "which modules does this hospital have"
/// store, separate from Tenant.EnabledModules (RBAC). See FeatureCatalog's own doc comment
/// for the distinction.
/// </summary>
internal sealed class TenantFeatureService : ITenantFeatureService
{
    private readonly ITenantRepository _tenantRepository;
    private readonly ITenantFeatureRepository _featureRepository;
    private readonly ITenantDirectory _tenantDirectory;
    private readonly ITenantMigrationService _migrationService;
    private readonly ILogger<TenantFeatureService> _logger;

    public TenantFeatureService(
        ITenantRepository tenantRepository,
        ITenantFeatureRepository featureRepository,
        ITenantDirectory tenantDirectory,
        ITenantMigrationService migrationService,
        ILogger<TenantFeatureService> logger)
    {
        _tenantRepository = tenantRepository;
        _featureRepository = featureRepository;
        _tenantDirectory = tenantDirectory;
        _migrationService = migrationService;
        _logger = logger;
    }

    public async Task<Result<TenantFeaturesResponse>> GetAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        var tenant = await _tenantRepository.GetByIdAsync(tenantId, cancellationToken);
        if (tenant is null)
        {
            return Result<TenantFeaturesResponse>.Failure(PlatformErrorCodes.NotFound, "No hospital was found for the given id.");
        }

        var existing = await _featureRepository.GetByTenantIdAsync(tenantId, cancellationToken);

        // A tenant with no rows yet (provisioned before this table existed, or never
        // toggled) is backward-compatibly treated as having every feature enabled — the
        // same "default to everything" posture Tenant.EnabledModules already uses — and a
        // sub-feature with no row is on (ADR-145). See FeatureCatalog.ResolveSelected*.
        return Result<TenantFeaturesResponse>.Success(ToResponse(tenant.Id, existing));
    }

    public async Task<Result<TenantFeaturesResponse>> UpdateAsync(Guid tenantId, UpdateTenantFeaturesRequest request, Guid? actorId, CancellationToken cancellationToken)
    {
        var tenant = await _tenantRepository.GetByIdAsync(tenantId, cancellationToken);
        if (tenant is null)
        {
            return Result<TenantFeaturesResponse>.Failure(PlatformErrorCodes.NotFound, "No hospital was found for the given id.");
        }

        var existing = await _featureRepository.GetByTenantIdAsync(tenantId, cancellationToken);
        var existingByKey = existing.ToDictionary(f => f.FeatureKey);

        var desiredByKey = FeatureCatalog.All.ToDictionary(
            key => key,
            // Mandatory keys are forced on regardless of what the caller sent — the
            // validator already rejects a request that omits one, but this is the
            // defensive backstop so no caller can ever provision/migrate without them,
            // even via a bug elsewhere (see FeatureCatalog's own doc comment).
            key => FeatureCatalog.Mandatory.Contains(key) || request.EnabledFeatures.Contains(key));

        // A key is "newly enabled" if it's becoming enabled and wasn't already (covers both
        // an existing disabled row being flipped on, and a key with no row yet — e.g. a
        // tenant provisioned before this table existed).
        // An OnWithoutRow key with no row is already on (FeatureCatalog.ResolveSelectedModules),
        // so saving it as on for the first time isn't "newly enabled".
        bool IsOnNow(string key) => existingByKey.TryGetValue(key, out var f)
            ? f.IsEnabled
            : existing.Count > 0 && FeatureCatalog.OnWithoutRow.Contains(key);
        var newlyEnabled = desiredByKey
            .Where(kv => kv.Value && !IsOnNow(kv.Key))
            .Select(kv => kv.Key)
            .ToList();

        if (newlyEnabled.Count > 0)
        {
            // Migrate BEFORE persisting the flag flip: if this fails, nothing is saved and
            // the tenant's existing features are left exactly as they were — avoids ever
            // marking a feature "enabled" whose schema was never actually created (Tenant
            // Feature/Module Management's "auto-migrates missing schema when enabling").
            var tenantInfo = await _tenantDirectory.FindByIdAsync(tenantId, cancellationToken)
                ?? throw new InvalidOperationException($"Tenant '{tenantId}' exists in the repository but ITenantDirectory could not resolve it.");

            try
            {
                await _migrationService.MigrateAsync(tenantInfo.ConnectionString, desiredByKey.Where(kv => kv.Value).Select(kv => kv.Key).ToList(), cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Migration failed while enabling feature(s) [{Features}] for tenant '{HospitalCode}'", string.Join(", ", newlyEnabled), tenant.HospitalCode);
                return Result<TenantFeaturesResponse>.Failure(
                    PlatformErrorCodes.MigrationFailed,
                    $"Failed to provision the schema for the newly enabled feature(s). No feature flags were changed; see server logs for details.");
            }
        }

        // Sub-features (ADR-145) have no schema, so they never need the migration above. Null
        // means the caller didn't send any (e.g. an older client): leave them exactly as they are.
        if (request.EnabledSubFeatures is not null)
        {
            foreach (var key in FeatureCatalog.SubFeatureKeys)
            {
                desiredByKey[key] = request.EnabledSubFeatures.Contains(key);
            }
        }

        var toCreate = new List<TenantFeature>();
        foreach (var (key, desiredEnabled) in desiredByKey)
        {
            if (existingByKey.TryGetValue(key, out var feature))
            {
                feature.SetEnabled(desiredEnabled, actorId);
            }
            else
            {
                toCreate.Add(TenantFeature.Create(tenantId, key, desiredEnabled, actorId));
            }
        }

        if (toCreate.Count > 0)
        {
            await _featureRepository.AddRangeAsync(toCreate, cancellationToken);
        }

        await _featureRepository.SaveChangesAsync(cancellationToken);

        var response = ToResponse(tenant.Id, [.. existingByKey.Values, .. toCreate]);

        _logger.LogInformation(
            "Updated features for tenant '{HospitalCode}': {FeatureCount} module(s) and {SubFeatureCount} sub-feature(s) enabled",
            tenant.HospitalCode, response.EnabledFeatures.Count, response.EnabledSubFeatures.Count);

        return Result<TenantFeaturesResponse>.Success(response);
    }

    private static TenantFeaturesResponse ToResponse(Guid tenantId, IReadOnlyCollection<TenantFeature> rows)
    {
        var rowStates = rows.Select(f => (f.FeatureKey, f.IsEnabled)).ToList();
        return new TenantFeaturesResponse
        {
            Id = tenantId,
            EnabledFeatures = FeatureCatalog.ResolveSelectedModules(rowStates),
            AllFeatures = FeatureCatalog.All,
            MandatoryFeatures = FeatureCatalog.Mandatory,
            Dependencies = FeatureCatalog.Dependencies,
            SubFeatures = [.. FeatureCatalog.SubFeatures.Select(sub => new SubFeatureResponse { Key = sub.Key, Parent = sub.Parent })],
            EnabledSubFeatures = FeatureCatalog.ResolveSelectedSubFeatures(rowStates),
            SubFeatureDependencies = FeatureCatalog.SubFeatureDependencies,
        };
    }
}
