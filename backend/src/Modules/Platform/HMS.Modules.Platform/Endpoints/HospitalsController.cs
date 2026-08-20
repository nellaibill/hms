using FluentValidation;
using FluentValidation.Results;
using HMS.Modules.Platform.Application;
using HMS.Modules.Platform.Application.Abstractions;
using HMS.Modules.Platform.Contracts;
using HMS.Shared.Infrastructure;
using HMS.Shared.Kernel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace HMS.Modules.Platform.Endpoints;

/// <summary>Hospital registration/provisioning — every action requires a Platform Admin token.</summary>
[ApiController]
[Route("api/platform/hospitals")]
[Authorize(Policy = "Platform")]
public class HospitalsController : ControllerBase
{
    private readonly IHospitalRegistrationService _registrationService;
    private readonly IPlatformDashboardService _dashboardService;
    private readonly ITenantFeatureService _featureService;
    private readonly IValidator<CreateHospitalRequest> _createValidator;
    private readonly IValidator<UpdateTenantConfigurationRequest> _configurationValidator;
    private readonly IValidator<UpdateTenantFeaturesRequest> _featuresValidator;
    private readonly ILogger<HospitalsController> _logger;

    public HospitalsController(
        IHospitalRegistrationService registrationService,
        IPlatformDashboardService dashboardService,
        ITenantFeatureService featureService,
        IValidator<CreateHospitalRequest> createValidator,
        IValidator<UpdateTenantConfigurationRequest> configurationValidator,
        IValidator<UpdateTenantFeaturesRequest> featuresValidator,
        ILogger<HospitalsController> logger)
    {
        _registrationService = registrationService;
        _dashboardService = dashboardService;
        _featureService = featureService;
        _createValidator = createValidator;
        _configurationValidator = configurationValidator;
        _featuresValidator = featuresValidator;
        _logger = logger;
    }

    /// <summary>Lists hospitals, paged, most recently created first.</summary>
    /// <response code="200">Returns the requested page.</response>
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] TenantListQuery query, CancellationToken cancellationToken)
    {
        var paged = await _dashboardService.GetHospitalsAsync(query, cancellationToken);
        var meta = new PaginationMeta
        {
            Page = paged.Page,
            PageSize = paged.PageSize,
            TotalCount = paged.TotalCount,
            TotalPages = paged.TotalPages,
        };
        return Ok(new ApiResponse<IReadOnlyList<TenantListItemResponse>> { Data = paged.Items, Meta = meta });
    }

    /// <summary>Returns hospital counts for the dashboard's stat tiles.</summary>
    /// <response code="200">Returns the counts.</response>
    [HttpGet("stats")]
    public async Task<IActionResult> GetStats(CancellationToken cancellationToken)
    {
        var stats = await _dashboardService.GetStatsAsync(cancellationToken);
        return Ok(new ApiResponse<TenantDashboardStatsResponse> { Data = stats });
    }

    /// <summary>Enables or disables a hospital.</summary>
    /// <response code="200">The status was updated.</response>
    /// <response code="400">The status value was not valid.</response>
    /// <response code="404">No hospital was found for the given id.</response>
    [HttpPatch("{id:guid}/status")]
    [Authorize(Policy = "PlatformSuperAdmin")]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateTenantStatusRequest request, CancellationToken cancellationToken)
    {
        var result = await _dashboardService.UpdateStatusAsync(id, request.Status, User.GetPlatformUserId(), cancellationToken);
        if (!result.IsSuccess)
        {
            return MapFailure(result.ErrorCode!, result.Error!);
        }

        return Ok(new ApiResponse<TenantListItemResponse> { Data = result.Value });
    }

    /// <summary>Lists soft-deleted hospitals, paged, most recently deleted first — the only
    /// way to find one to restore.</summary>
    /// <response code="200">Returns the requested page.</response>
    [HttpGet("deleted")]
    public async Task<IActionResult> GetDeleted([FromQuery] TenantListQuery query, CancellationToken cancellationToken)
    {
        var paged = await _dashboardService.GetDeletedHospitalsAsync(query, cancellationToken);
        var meta = new PaginationMeta
        {
            Page = paged.Page,
            PageSize = paged.PageSize,
            TotalCount = paged.TotalCount,
            TotalPages = paged.TotalPages,
        };
        return Ok(new ApiResponse<IReadOnlyList<DeletedTenantListItemResponse>> { Data = paged.Items, Meta = meta });
    }

    /// <summary>Dry-run preview shown before confirming a delete.</summary>
    /// <response code="200">Returns the preview.</response>
    /// <response code="404">No hospital was found for the given id.</response>
    [HttpGet("{id:guid}/delete-preview")]
    [Authorize(Policy = "PlatformSuperAdmin")]
    public async Task<IActionResult> GetDeletePreview(Guid id, CancellationToken cancellationToken)
    {
        var result = await _dashboardService.GetDeletePreviewAsync(id, cancellationToken);
        return result.IsSuccess ? Ok(new ApiResponse<TenantDeletePreviewResponse> { Data = result.Value }) : MapFailure(result.ErrorCode!, result.Error!);
    }

    /// <summary>
    /// Soft-deletes a hospital: blocks its staff from signing in and hides it from the
    /// active list, but its own database is never touched — fully reversible via
    /// <see cref="Restore"/>. <paramref name="confirmHospitalCode"/> must match the
    /// hospital's actual code (server-enforced "type to confirm").
    /// </summary>
    /// <response code="204">The hospital was soft-deleted.</response>
    /// <response code="400">The confirmation code did not match.</response>
    /// <response code="404">No hospital was found for the given id.</response>
    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "PlatformSuperAdmin")]
    public async Task<IActionResult> Delete(Guid id, [FromQuery] string confirmHospitalCode, CancellationToken cancellationToken)
    {
        var result = await _dashboardService.DeleteHospitalAsync(id, confirmHospitalCode ?? string.Empty, User.GetPlatformUserId(), cancellationToken);
        if (!result.IsSuccess)
        {
            return MapFailure(result.ErrorCode!, result.Error!);
        }

        _logger.LogInformation("DELETE /api/platform/hospitals/{Id} succeeded", id);
        return NoContent();
    }

    /// <summary>Reverses a soft-delete, restoring the hospital's previous status and login access.</summary>
    /// <response code="200">The hospital was restored.</response>
    /// <response code="404">No soft-deleted hospital was found for the given id.</response>
    [HttpPost("{id:guid}/restore")]
    [Authorize(Policy = "PlatformSuperAdmin")]
    public async Task<IActionResult> Restore(Guid id, CancellationToken cancellationToken)
    {
        var result = await _dashboardService.RestoreHospitalAsync(id, User.GetPlatformUserId(), cancellationToken);
        return result.IsSuccess ? Ok(new ApiResponse<TenantListItemResponse> { Data = result.Value }) : MapFailure(result.ErrorCode!, result.Error!);
    }

    /// <summary>Returns which business-domain modules this hospital's staff can use, and its
    /// subscription tier.</summary>
    /// <response code="200">Returns the configuration.</response>
    /// <response code="404">No hospital was found for the given id.</response>
    [HttpGet("{id:guid}/configuration")]
    [Authorize(Policy = "PlatformSuperAdmin")]
    public async Task<IActionResult> GetConfiguration(Guid id, CancellationToken cancellationToken)
    {
        var result = await _dashboardService.GetConfigurationAsync(id, cancellationToken);
        return result.IsSuccess ? Ok(new ApiResponse<TenantConfigurationResponse> { Data = result.Value }) : MapFailure(result.ErrorCode!, result.Error!);
    }

    /// <summary>
    /// Updates which business-domain modules this hospital's staff can use, and its
    /// subscription tier. Takes effect on each affected user's next login (permissions for
    /// a disabled module are stripped from the JWT at login time — see
    /// AuthenticationService.LoginAsync — not revoked from tokens already issued).
    /// </summary>
    /// <response code="200">The configuration was updated.</response>
    /// <response code="400">The request failed validation.</response>
    /// <response code="404">No hospital was found for the given id.</response>
    [HttpPut("{id:guid}/configuration")]
    [Authorize(Policy = "PlatformSuperAdmin")]
    public async Task<IActionResult> UpdateConfiguration(Guid id, [FromBody] UpdateTenantConfigurationRequest request, CancellationToken cancellationToken)
    {
        var validation = await _configurationValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(BuildValidationError(validation));
        }

        var result = await _dashboardService.UpdateConfigurationAsync(id, request, User.GetPlatformUserId(), cancellationToken);
        return result.IsSuccess ? Ok(new ApiResponse<TenantConfigurationResponse> { Data = result.Value }) : MapFailure(result.ErrorCode!, result.Error!);
    }

    /// <summary>Returns which schema-level modules this hospital tenant has — distinct from
    /// GetConfiguration's RBAC EnabledModules. See TenantFeaturesResponse's own doc comment.</summary>
    /// <response code="200">Returns the tenant's features.</response>
    /// <response code="404">No hospital was found for the given id.</response>
    [HttpGet("{id:guid}/features")]
    [Authorize(Policy = "PlatformSuperAdmin")]
    public async Task<IActionResult> GetFeatures(Guid id, CancellationToken cancellationToken)
    {
        var result = await _featureService.GetAsync(id, cancellationToken);
        return result.IsSuccess ? Ok(new ApiResponse<TenantFeaturesResponse> { Data = result.Value }) : MapFailure(result.ErrorCode!, result.Error!);
    }

    /// <summary>
    /// Updates which schema-level modules this hospital tenant has. Enabling a feature that
    /// was never provisioned schedules its schema to be created on next migrate; disabling a
    /// feature never drops its schema or data — only its nav/API access is revoked, enforced
    /// live on every request via FeatureAuthorizationHandler (not from a JWT snapshot), so it
    /// takes effect immediately without waiting for affected users to re-login.
    /// </summary>
    /// <response code="200">The features were updated.</response>
    /// <response code="400">The request failed validation (unknown key, or a mandatory feature was omitted).</response>
    /// <response code="404">No hospital was found for the given id.</response>
    [HttpPut("{id:guid}/features")]
    [Authorize(Policy = "PlatformSuperAdmin")]
    public async Task<IActionResult> UpdateFeatures(Guid id, [FromBody] UpdateTenantFeaturesRequest request, CancellationToken cancellationToken)
    {
        var validation = await _featuresValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(BuildValidationError(validation));
        }

        var result = await _featureService.UpdateAsync(id, request, User.GetPlatformUserId(), cancellationToken);
        return result.IsSuccess ? Ok(new ApiResponse<TenantFeaturesResponse> { Data = result.Value }) : MapFailure(result.ErrorCode!, result.Error!);
    }

    /// <summary>
    /// Applies any pending EF Core migrations to this hospital's existing database — HMS
    /// Multi-Tenancy Phase C's migration-management action. An explicit, operator-triggered
    /// operation; never run automatically or per-request.
    /// </summary>
    /// <response code="200">Migrations were applied (or the database was already current).</response>
    /// <response code="404">No hospital was found for the given id.</response>
    /// <response code="400">Migration failed; the existing database was left unchanged.</response>
    [HttpPost("{id:guid}/migrate")]
    [Authorize(Policy = "PlatformSuperAdmin")]
    public async Task<IActionResult> Migrate(Guid id, CancellationToken cancellationToken)
    {
        var result = await _dashboardService.MigrateAsync(id, cancellationToken);
        if (!result.IsSuccess)
        {
            return MapFailure(result.ErrorCode!, result.Error!);
        }

        return Ok(new ApiResponse<TenantListItemResponse> { Data = result.Value });
    }

    /// <summary>Registers a new hospital: provisions its isolated database and its first Super Admin.</summary>
    /// <remarks>
    /// Requires an "Idempotency-Key" header — provisioning is not cheap to retry safely, so a
    /// client must supply the same key across retries of the same logical attempt (e.g. after
    /// a client-side timeout) rather than risk double-provisioning. A fresh key must be used
    /// for each genuinely new registration.
    /// </remarks>
    /// <response code="201">The hospital was registered and its database provisioned.</response>
    /// <response code="400">The request failed validation, provisioning failed, or the Idempotency-Key header was missing.</response>
    /// <response code="409">The hospital code or Super Admin email is already registered, or the Idempotency-Key is still in flight or was reused for different data.</response>
    [HttpPost]
    [Authorize(Policy = "PlatformSuperAdmin")]
    public async Task<IActionResult> Create([FromBody] CreateHospitalRequest request, CancellationToken cancellationToken)
    {
        if (!Request.Headers.TryGetValue("Idempotency-Key", out var idempotencyKeyHeader) || string.IsNullOrWhiteSpace(idempotencyKeyHeader))
        {
            return BadRequest(new ApiErrorResponse
            {
                ErrorCode = "VALIDATION.MISSING_IDEMPOTENCY_KEY",
                Message = "The 'Idempotency-Key' header is required.",
                CorrelationId = HttpContext.GetCorrelationId(),
                Timestamp = DateTime.UtcNow,
            });
        }

        var validation = await _createValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(BuildValidationError(validation));
        }

        var result = await _registrationService.RegisterAsync(request, User.GetPlatformUserId(), idempotencyKeyHeader.ToString(), cancellationToken);
        if (!result.IsSuccess)
        {
            return MapFailure(result.ErrorCode!, result.Error!);
        }

        _logger.LogInformation("POST /api/platform/hospitals succeeded for {HospitalCode}", request.HospitalCode);

        return StatusCode(StatusCodes.Status201Created, new ApiResponse<CreateHospitalResponse> { Data = result.Value });
    }

    private IActionResult MapFailure(string errorCode, string message)
    {
        var status = errorCode switch
        {
            PlatformErrorCodes.DuplicateHospitalCode => StatusCodes.Status409Conflict,
            PlatformErrorCodes.DuplicateAdminEmail => StatusCodes.Status409Conflict,
            PlatformErrorCodes.IdempotencyKeyInProgress => StatusCodes.Status409Conflict,
            PlatformErrorCodes.IdempotencyKeyReused => StatusCodes.Status409Conflict,
            PlatformErrorCodes.NotFound => StatusCodes.Status404NotFound,
            PlatformErrorCodes.NotDeleted => StatusCodes.Status404NotFound,
            _ => StatusCodes.Status400BadRequest,
        };

        var error = new ApiErrorResponse
        {
            ErrorCode = errorCode,
            Message = message,
            CorrelationId = HttpContext.GetCorrelationId(),
            Timestamp = DateTime.UtcNow,
        };

        return StatusCode(status, error);
    }

    private ApiErrorResponse BuildValidationError(ValidationResult validation) => new()
    {
        ErrorCode = "VALIDATION.FAILED",
        Message = "One or more validation errors occurred.",
        ValidationErrors = validation.Errors
            .Select(e => new ValidationErrorItem { Field = e.PropertyName, Message = e.ErrorMessage })
            .ToList(),
        CorrelationId = HttpContext.GetCorrelationId(),
        Timestamp = DateTime.UtcNow,
    };
}
