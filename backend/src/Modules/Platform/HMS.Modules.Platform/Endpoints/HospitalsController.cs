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
    private readonly IValidator<CreateHospitalRequest> _createValidator;
    private readonly ILogger<HospitalsController> _logger;

    public HospitalsController(
        IHospitalRegistrationService registrationService,
        IPlatformDashboardService dashboardService,
        IValidator<CreateHospitalRequest> createValidator,
        ILogger<HospitalsController> logger)
    {
        _registrationService = registrationService;
        _dashboardService = dashboardService;
        _createValidator = createValidator;
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
        var result = await _dashboardService.UpdateStatusAsync(id, request.Status, cancellationToken);
        if (!result.IsSuccess)
        {
            return MapFailure(result.ErrorCode!, result.Error!);
        }

        return Ok(new ApiResponse<TenantListItemResponse> { Data = result.Value });
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
    /// <response code="201">The hospital was registered and its database provisioned.</response>
    /// <response code="400">The request failed validation, or provisioning failed.</response>
    /// <response code="409">The hospital code or Super Admin email is already registered.</response>
    [HttpPost]
    [Authorize(Policy = "PlatformSuperAdmin")]
    public async Task<IActionResult> Create([FromBody] CreateHospitalRequest request, CancellationToken cancellationToken)
    {
        var validation = await _createValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(BuildValidationError(validation));
        }

        var result = await _registrationService.RegisterAsync(request, cancellationToken);
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
            PlatformErrorCodes.NotFound => StatusCodes.Status404NotFound,
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
