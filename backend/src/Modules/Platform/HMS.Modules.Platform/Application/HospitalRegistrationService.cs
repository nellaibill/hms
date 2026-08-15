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
    private readonly ITenantProvisioner _tenantProvisioner;
    private readonly ILogger<HospitalRegistrationService> _logger;

    public HospitalRegistrationService(
        ITenantRepository tenantRepository,
        ITenantProvisioner tenantProvisioner,
        ILogger<HospitalRegistrationService> logger)
    {
        _tenantRepository = tenantRepository;
        _tenantProvisioner = tenantProvisioner;
        _logger = logger;
    }

    public async Task<Result<CreateHospitalResponse>> RegisterAsync(CreateHospitalRequest request, CancellationToken cancellationToken)
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

        var provisionResult = await _tenantProvisioner.ProvisionAsync(
            new TenantProvisionRequest(
                request.HospitalName,
                request.SuperAdminUsername,
                request.SuperAdminFirstName,
                request.SuperAdminLastName,
                request.SuperAdminEmail,
                request.SuperAdminPhoneNumber,
                request.SuperAdminPassword),
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
            createdBy: null);

        await _tenantRepository.AddAsync(tenant, cancellationToken);
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
            DatabaseName = tenant.DatabaseName,
            Status = tenant.Status.ToString(),
            CreatedAt = tenant.CreatedAt,
        });
    }
}
