using HMS.Modules.Platform.Application.Abstractions;
using HMS.Modules.Platform.Contracts;
using HMS.Modules.Platform.Domain;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace HMS.Modules.Platform.Infrastructure;

/// <summary>
/// Implements <see cref="ITenantDirectory"/> against this module's own Tenant aggregate.
/// Builds each tenant's connection string from ConnectionStrings:Default (the same
/// Postgres user/host/port every hospital request already used pre-Phase-C — no new
/// credentials, per the "keep current PostgreSQL deployment" constraint) with only the
/// Database swapped to the tenant's own — the same technique
/// HMS.Api.Provisioning.TenantProvisioningService.BuildTenantConnectionString already uses
/// against ConnectionStrings:PlatformAdmin for CREATE/DROP DATABASE.
/// </summary>
internal sealed class TenantDirectory : ITenantDirectory
{
    private readonly ITenantRepository _tenantRepository;
    private readonly string _baseConnectionString;

    public TenantDirectory(ITenantRepository tenantRepository, IConfiguration configuration)
    {
        _tenantRepository = tenantRepository;
        _baseConnectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("Missing 'ConnectionStrings:Default' configuration value.");
    }

    public async Task<TenantInfo?> FindByIdAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        var tenant = await _tenantRepository.GetByIdAsync(tenantId, cancellationToken);
        return tenant is null ? null : ToTenantInfo(tenant);
    }

    public async Task<TenantInfo?> FindByHospitalCodeAsync(string hospitalCode, CancellationToken cancellationToken)
    {
        var tenant = await _tenantRepository.GetByHospitalCodeAsync(hospitalCode, cancellationToken);
        return tenant is null ? null : ToTenantInfo(tenant);
    }

    private TenantInfo ToTenantInfo(Tenant tenant)
    {
        var builder = new NpgsqlConnectionStringBuilder(_baseConnectionString) { Database = tenant.DatabaseName };
        return new TenantInfo(tenant.Id, tenant.HospitalCode, tenant.DatabaseName, builder.ConnectionString, tenant.Status == TenantStatus.Active);
    }
}
