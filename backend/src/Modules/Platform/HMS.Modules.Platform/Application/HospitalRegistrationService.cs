using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using HMS.Modules.Platform.Application.Abstractions;
using HMS.Modules.Platform.Contracts;
using HMS.Modules.Platform.Domain;
using HMS.Shared.Kernel;
using Microsoft.Extensions.Logging;

namespace HMS.Modules.Platform.Application;

/// <summary>
/// Orchestrates hospital registration: duplicate checks against this module's own
/// platform.tenants table, then delegates the actual database provisioning to
/// <see cref="ITenantProvisioner"/> (implemented in HMS.Api — see that interface's own
/// doc comment). Only writes a platform.tenants row after provisioning has fully
/// succeeded — a provisioning failure never leaves an orphaned tenant record, and never
/// touches Patient/Gender/BloodGroup or any other unrelated domain (see
/// docs/DecisionLog.md's SaaS provisioning ADR, "hard boundary" section).
/// </summary>
internal sealed class HospitalRegistrationService : IHospitalRegistrationService
{
    private readonly ITenantRepository _tenantRepository;
    private readonly ITenantFeatureRepository _featureRepository;
    private readonly ITenantProvisioner _tenantProvisioner;
    private readonly IHospitalRegistrationIdempotencyStore _idempotencyStore;
    private readonly ILogger<HospitalRegistrationService> _logger;

    public HospitalRegistrationService(
        ITenantRepository tenantRepository,
        ITenantFeatureRepository featureRepository,
        ITenantProvisioner tenantProvisioner,
        IHospitalRegistrationIdempotencyStore idempotencyStore,
        ILogger<HospitalRegistrationService> logger)
    {
        _tenantRepository = tenantRepository;
        _featureRepository = featureRepository;
        _tenantProvisioner = tenantProvisioner;
        _idempotencyStore = idempotencyStore;
        _logger = logger;
    }

    public async Task<Result<CreateHospitalResponse>> RegisterAsync(CreateHospitalRequest request, Guid? actorId, string idempotencyKey, CancellationToken cancellationToken)
    {
        var requestHash = ComputeRequestHash(request);
        var reservation = await _idempotencyStore.ReserveAsync(idempotencyKey, requestHash, cancellationToken);

        switch (reservation.Outcome)
        {
            case IdempotencyReservationOutcome.ReplayCompleted:
                _logger.LogInformation("Replayed cached result for Idempotency-Key '{IdempotencyKey}'", idempotencyKey);
                return reservation.ReplayedResult!;

            case IdempotencyReservationOutcome.ReplayInProgress:
                return Result<CreateHospitalResponse>.Failure(
                    PlatformErrorCodes.IdempotencyKeyInProgress,
                    "A request with this Idempotency-Key is already being processed.");

            case IdempotencyReservationOutcome.KeyReusedForDifferentRequest:
                return Result<CreateHospitalResponse>.Failure(
                    PlatformErrorCodes.IdempotencyKeyReused,
                    "This Idempotency-Key was already used for a request with different data.");
        }

        var result = await RegisterCoreAsync(request, actorId, cancellationToken);
        await _idempotencyStore.CompleteAsync(reservation.RecordId!.Value, result, cancellationToken);
        return result;
    }

    private static string ComputeRequestHash(CreateHospitalRequest request)
    {
        var json = JsonSerializer.Serialize(request);
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(json));
        return Convert.ToHexStringLower(bytes);
    }

    private async Task<Result<CreateHospitalResponse>> RegisterCoreAsync(CreateHospitalRequest request, Guid? actorId, CancellationToken cancellationToken)
    {
        var existingByCode = await _tenantRepository.GetByHospitalCodeAsync(request.HospitalCode, cancellationToken);
        if (existingByCode is not null)
        {
            return Result<CreateHospitalResponse>.Failure(
                PlatformErrorCodes.DuplicateHospitalCode,
                $"A hospital with code '{request.HospitalCode}' is already registered.");
        }

        var existingByEmail = await _tenantRepository.GetByEmailAsync(request.SuperAdminEmail, cancellationToken);
        if (existingByEmail is not null)
        {
            return Result<CreateHospitalResponse>.Failure(
                PlatformErrorCodes.DuplicateAdminEmail,
                $"A hospital with Super Admin email '{request.SuperAdminEmail}' is already registered.");
        }

        // Mandatory features are always included regardless of what the caller selected —
        // same defensive union TenantMigrationService itself also applies, done here too so
        // the platform.tenant_features rows written below agree with what was actually
        // migrated.
        var resolvedFeatureKeys = new HashSet<string>(request.EnabledFeatureKeys);
        resolvedFeatureKeys.UnionWith(FeatureCatalog.Mandatory);

        var provisionResult = await _tenantProvisioner.ProvisionAsync(
            new TenantProvisionRequest(
                request.HospitalName,
                request.SuperAdminUsername,
                request.SuperAdminFirstName,
                request.SuperAdminLastName,
                request.SuperAdminEmail,
                request.SuperAdminPhoneNumber,
                request.SuperAdminPassword,
                resolvedFeatureKeys,
                request.ImportedPatientCapacity),
            cancellationToken);

        if (!provisionResult.IsSuccess)
        {
            _logger.LogError(
                "Hospital registration failed for '{HospitalCode}': {ErrorCode}",
                request.HospitalCode,
                provisionResult.ErrorCode);
            return Result<CreateHospitalResponse>.Failure(provisionResult.ErrorCode!, provisionResult.Error!);
        }

        var tenant = Tenant.Create(
            request.HospitalName,
            request.HospitalCode,
            request.MobileNumber,
            request.SuperAdminEmail,
            request.Address,
            request.City,
            request.State,
            request.Pincode,
            provisionResult.Value!.DatabaseName,
            request.ImportedPatientCapacity,
            createdBy: actorId);

        await _tenantRepository.AddAsync(tenant, cancellationToken);

        var featureRows = FeatureCatalog.All.Select(key => TenantFeature.Create(tenant.Id, key, resolvedFeatureKeys.Contains(key), createdBy: actorId));
        await _featureRepository.AddRangeAsync(featureRows, cancellationToken);

        // Both repositories share the same scoped PlatformDbContext, so one SaveChanges
        // persists the tenant row and its initial tenant_features rows together.
        await _tenantRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Provisioned hospital '{HospitalCode}' -> database '{DatabaseName}'",
            tenant.HospitalCode,
            tenant.DatabaseName);

        return Result<CreateHospitalResponse>.Success(new CreateHospitalResponse
        {
            Id = tenant.Id,
            HospitalName = tenant.HospitalName,
            HospitalCode = tenant.HospitalCode,
            Status = tenant.Status.ToString(),
            CreatedAt = tenant.CreatedAt,
            ImportedPatientCapacity = tenant.ImportedPatientCapacity,
        });
    }
}
