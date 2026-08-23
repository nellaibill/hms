using HMS.Modules.Platform.Infrastructure;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace HMS.Api.HealthChecks;

/// <summary>
/// Confirms the platform database is reachable — used by the container orchestrator
/// (Docker Compose healthcheck) to gate readiness, not application business logic. Checks
/// PlatformDbContext specifically because it's the one DbContext always resolvable outside
/// a request (no ITenantContext needed), unlike every tenant-aware hospital DbContext.
/// </summary>
public sealed class DatabaseHealthCheck(PlatformDbContext platformDb) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        return await platformDb.Database.CanConnectAsync(cancellationToken)
            ? HealthCheckResult.Healthy()
            : HealthCheckResult.Unhealthy("Cannot connect to the platform database.");
    }
}
