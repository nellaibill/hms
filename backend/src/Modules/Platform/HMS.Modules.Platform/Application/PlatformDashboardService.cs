using HMS.Modules.Platform.Application.Abstractions;
using HMS.Modules.Platform.Contracts;
using HMS.Modules.Platform.Domain;
using HMS.Shared.Kernel;
using Microsoft.Extensions.Logging;

namespace HMS.Modules.Platform.Application;

internal sealed class PlatformDashboardService : IPlatformDashboardService
{
    private readonly ITenantRepository _tenantRepository;
    private readonly ITenantDirectory _tenantDirectory;
    private readonly ITenantMigrationService _migrationService;
    private readonly IProvisioningAlertStore _provisioningAlertStore;
    private readonly ILogger<PlatformDashboardService> _logger;

    public PlatformDashboardService(
        ITenantRepository tenantRepository,
        ITenantDirectory tenantDirectory,
        ITenantMigrationService migrationService,
        IProvisioningAlertStore provisioningAlertStore,
        ILogger<PlatformDashboardService> logger)
    {
        _tenantRepository = tenantRepository;
        _tenantDirectory = tenantDirectory;
        _migrationService = migrationService;
        _provisioningAlertStore = provisioningAlertStore;
        _logger = logger;
    }

    public async Task<PagedResult<TenantListItemResponse>> GetHospitalsAsync(TenantListQuery query, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _tenantRepository.GetPagedAsync(query, cancellationToken);
        return new PagedResult<TenantListItemResponse>(items.Select(ToResponse).ToList(), query.Page, query.PageSize, totalCount);
    }

    public async Task<TenantDashboardStatsResponse> GetStatsAsync(CancellationToken cancellationToken)
    {
        var (total, active, inactive) = await _tenantRepository.GetCountsAsync(cancellationToken);
        var provisioningAlertCount = await _provisioningAlertStore.GetCountAsync(cancellationToken);
        return new TenantDashboardStatsResponse
        {
            Total = total,
            Active = active,
            Inactive = inactive,
            ProvisioningAlertCount = provisioningAlertCount,
        };
    }

    public async Task<Result<TenantListItemResponse>> UpdateStatusAsync(Guid id, string status, Guid? actorId, CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<TenantStatus>(status, ignoreCase: true, out var parsedStatus))
        {
            return Result<TenantListItemResponse>.Failure(PlatformErrorCodes.InvalidStatus, $"'{status}' is not a valid status.");
        }

        var tenant = await _tenantRepository.GetByIdAsync(id, cancellationToken);
        if (tenant is null)
        {
            return Result<TenantListItemResponse>.Failure(PlatformErrorCodes.NotFound, "No hospital was found for the given id.");
        }

        tenant.SetStatus(parsedStatus, updatedBy: actorId);
        await _tenantRepository.SaveChangesAsync(cancellationToken);

        return Result<TenantListItemResponse>.Success(ToResponse(tenant));
    }

    public async Task<Result<TenantListItemResponse>> MigrateAsync(Guid id, CancellationToken cancellationToken)
    {
        var tenant = await _tenantRepository.GetByIdAsync(id, cancellationToken);
        if (tenant is null)
        {
            return Result<TenantListItemResponse>.Failure(PlatformErrorCodes.NotFound, "No hospital was found for the given id.");
        }

        // Resolved via ITenantDirectory (not rebuilt here) so the connection string always
        // comes from the same trusted source runtime tenant resolution uses — never a
        // database name taken directly off the Tenant row without going through it.
        var tenantInfo = await _tenantDirectory.FindByIdAsync(id, cancellationToken)
            ?? throw new InvalidOperationException($"Tenant '{id}' exists in the repository but ITenantDirectory could not resolve it.");

        try
        {
            // Passes the tenant's CURRENT enabled-feature set (not the full catalog) so a
            // re-migrate never provisions a schema for a feature this tenant doesn't have —
            // otherwise this operator action would silently defeat feature-based exclusion.
            await _migrationService.MigrateAsync(tenantInfo.ConnectionString, tenantInfo.EnabledFeatures, cancellationToken);
        }
        catch (Exception ex)
        {
            // The tenant's existing database is left exactly as it was — MigrateAsync only
            // ever applies migrations, it never drops anything, so there is nothing to roll
            // back here (see ITenantMigrationService's own doc comment and Phase C's "never
            // automatically drop an existing tenant database on migration failure"
            // requirement).
            _logger.LogError(ex, "Migration failed for tenant '{HospitalCode}' (database '{DatabaseName}')", tenant.HospitalCode, tenant.DatabaseName);
            return Result<TenantListItemResponse>.Failure(
                PlatformErrorCodes.MigrationFailed,
                $"Migration failed for hospital '{tenant.HospitalCode}'. The existing database was not changed further; see server logs for details.");
        }

        _logger.LogInformation("Applied pending migrations for tenant '{HospitalCode}' (database '{DatabaseName}')", tenant.HospitalCode, tenant.DatabaseName);

        return Result<TenantListItemResponse>.Success(ToResponse(tenant));
    }

    public async Task<Result<TenantDeletePreviewResponse>> GetDeletePreviewAsync(Guid id, CancellationToken cancellationToken)
    {
        var tenant = await _tenantRepository.GetByIdAsync(id, cancellationToken);
        if (tenant is null)
        {
            return Result<TenantDeletePreviewResponse>.Failure(PlatformErrorCodes.NotFound, "No hospital was found for the given id.");
        }

        return Result<TenantDeletePreviewResponse>.Success(new TenantDeletePreviewResponse
        {
            Id = tenant.Id,
            HospitalName = tenant.HospitalName,
            HospitalCode = tenant.HospitalCode,
            Status = tenant.Status.ToString(),
            CreatedAt = tenant.CreatedAt,
        });
    }

    public async Task<Result> DeleteHospitalAsync(Guid id, string confirmHospitalCode, Guid? actorId, CancellationToken cancellationToken)
    {
        var tenant = await _tenantRepository.GetByIdAsync(id, cancellationToken);
        if (tenant is null)
        {
            return Result.Failure(PlatformErrorCodes.NotFound, "No hospital was found for the given id.");
        }

        if (!string.Equals(tenant.HospitalCode, confirmHospitalCode.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            return Result.Failure(PlatformErrorCodes.ConfirmationMismatch, "The hospital code you entered does not match.");
        }

        tenant.SoftDelete(actorId);
        await _tenantRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Soft-deleted tenant '{HospitalCode}' (its database was not touched)", tenant.HospitalCode);

        return Result.Success();
    }

    public async Task<Result<TenantListItemResponse>> RestoreHospitalAsync(Guid id, Guid? actorId, CancellationToken cancellationToken)
    {
        var tenant = await _tenantRepository.GetByIdIncludingDeletedAsync(id, cancellationToken);
        if (tenant is null || !tenant.IsDeleted)
        {
            return Result<TenantListItemResponse>.Failure(PlatformErrorCodes.NotDeleted, "No soft-deleted hospital was found for the given id.");
        }

        tenant.Restore(actorId);
        await _tenantRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Restored soft-deleted tenant '{HospitalCode}'", tenant.HospitalCode);

        return Result<TenantListItemResponse>.Success(ToResponse(tenant));
    }

    public async Task<PagedResult<DeletedTenantListItemResponse>> GetDeletedHospitalsAsync(TenantListQuery query, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _tenantRepository.GetDeletedPagedAsync(query, cancellationToken);
        return new PagedResult<DeletedTenantListItemResponse>(items.Select(ToDeletedResponse).ToList(), query.Page, query.PageSize, totalCount);
    }

    public async Task<Result<TenantConfigurationResponse>> GetConfigurationAsync(Guid id, CancellationToken cancellationToken)
    {
        var tenant = await _tenantRepository.GetByIdAsync(id, cancellationToken);
        if (tenant is null)
        {
            return Result<TenantConfigurationResponse>.Failure(PlatformErrorCodes.NotFound, "No hospital was found for the given id.");
        }

        return Result<TenantConfigurationResponse>.Success(ToConfigurationResponse(tenant));
    }

    public async Task<Result<TenantConfigurationResponse>> UpdateConfigurationAsync(Guid id, UpdateTenantConfigurationRequest request, Guid? actorId, CancellationToken cancellationToken)
    {
        var tenant = await _tenantRepository.GetByIdAsync(id, cancellationToken);
        if (tenant is null)
        {
            return Result<TenantConfigurationResponse>.Failure(PlatformErrorCodes.NotFound, "No hospital was found for the given id.");
        }

        tenant.UpdateConfiguration(request.EnabledModules, request.SubscriptionTier, actorId);
        await _tenantRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Updated configuration for tenant '{HospitalCode}': {ModuleCount} module(s) enabled, tier '{Tier}'",
            tenant.HospitalCode, tenant.EnabledModules.Count, tenant.SubscriptionTier);

        return Result<TenantConfigurationResponse>.Success(ToConfigurationResponse(tenant));
    }

    private static TenantConfigurationResponse ToConfigurationResponse(Tenant tenant) => new()
    {
        Id = tenant.Id,
        EnabledModules = tenant.EnabledModules,
        SubscriptionTier = tenant.SubscriptionTier,
        AllModules = ModuleCatalog.All,
    };

    private static TenantListItemResponse ToResponse(Tenant tenant) => new()
    {
        Id = tenant.Id,
        HospitalName = tenant.HospitalName,
        HospitalCode = tenant.HospitalCode,
        Status = tenant.Status.ToString(),
        CreatedAt = tenant.CreatedAt,
        SubscriptionTier = tenant.SubscriptionTier,
        ImportedPatientCapacity = tenant.ImportedPatientCapacity,
    };

    private static DeletedTenantListItemResponse ToDeletedResponse(Tenant tenant) => new()
    {
        Id = tenant.Id,
        HospitalName = tenant.HospitalName,
        HospitalCode = tenant.HospitalCode,
        Status = tenant.Status.ToString(),
        CreatedAt = tenant.CreatedAt,
        DeletedAt = tenant.DeletedAt!.Value,
    };
}
