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
    private readonly ILogger<PlatformDashboardService> _logger;

    public PlatformDashboardService(
        ITenantRepository tenantRepository,
        ITenantDirectory tenantDirectory,
        ITenantMigrationService migrationService,
        ILogger<PlatformDashboardService> logger)
    {
        _tenantRepository = tenantRepository;
        _tenantDirectory = tenantDirectory;
        _migrationService = migrationService;
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
        return new TenantDashboardStatsResponse { Total = total, Active = active, Inactive = inactive };
    }

    public async Task<Result<TenantListItemResponse>> UpdateStatusAsync(Guid id, string status, CancellationToken cancellationToken)
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

        tenant.SetStatus(parsedStatus, updatedBy: null);
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
            await _migrationService.MigrateAsync(tenantInfo.ConnectionString, cancellationToken);
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

    private static TenantListItemResponse ToResponse(Tenant tenant) => new()
    {
        Id = tenant.Id,
        HospitalName = tenant.HospitalName,
        HospitalCode = tenant.HospitalCode,
        DatabaseName = tenant.DatabaseName,
        Status = tenant.Status.ToString(),
        CreatedAt = tenant.CreatedAt,
    };
}
