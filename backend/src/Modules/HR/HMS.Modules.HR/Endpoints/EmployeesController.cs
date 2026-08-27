using FluentValidation;
using FluentValidation.Results;
using HMS.Modules.HR.Application;
using HMS.Modules.HR.Contracts;
using HMS.Shared.Infrastructure;
using HMS.Shared.Kernel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HMS.Modules.HR.Endpoints;

/// <summary>
/// Employee master CRUD for the Hospital HR Management MVP — see docs/DecisionLog.md ADR-036
/// for the Employee/identity.users/Masters.Consultant separation this entity implements.
/// "Actor" is read from the caller's JWT via ClaimsPrincipalExtensions.GetUserId — matches
/// every other module's controllers.
/// </summary>
[ApiController]
[RequireFeature("hr")]
[Route("api/v1/employees")]
public class EmployeesController : ControllerBase
{
    private readonly IEmployeeService _service;
    private readonly IValidator<CreateEmployeeRequest> _createValidator;
    private readonly IValidator<UpdateEmployeeRequest> _updateValidator;

    public EmployeesController(
        IEmployeeService service,
        IValidator<CreateEmployeeRequest> createValidator,
        IValidator<UpdateEmployeeRequest> updateValidator)
    {
        _service = service;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    /// <summary>Creates a new employee.</summary>
    /// <response code="201">The employee was created.</response>
    /// <response code="400">The request failed validation, or a reference (Department/Designation/ReportingManager/User) is invalid.</response>
    [Authorize]
    [RequirePermission("workforce-admin.create")]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateEmployeeRequest request, CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return BadRequest(BuildRequestRequiredError());
        }

        var validation = await _createValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(BuildValidationError(validation));
        }

        var result = await _service.CreateAsync(request, actorId: User.GetUserId(), cancellationToken);
        return !result.IsSuccess
            ? MapFailure(result.ErrorCode!, result.Error!)
            : CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, Envelope(result.Value));
    }

    /// <summary>Lists employees with paging, search (EmployeeCode/FirstName/LastName/Email), sorting, and filtering (Department/Designation/EmployeeType/EmploymentStatus/IsActive).</summary>
    /// <response code="200">A page of employees.</response>
    [Authorize]
    [RequirePermission("workforce-admin.view")]
    [HttpGet]
    public async Task<IActionResult> GetPaged([FromQuery] EmployeeListQuery query, CancellationToken cancellationToken)
    {
        var paged = await _service.GetPagedAsync(query, cancellationToken);
        var meta = new PaginationMeta { Page = paged.Page, PageSize = paged.PageSize, TotalCount = paged.TotalCount, TotalPages = paged.TotalPages };

        return Ok(new ApiResponse<IReadOnlyList<EmployeeResponse>> { Data = paged.Items, Meta = meta });
    }

    /// <summary>Gets a single employee's profile by id — includes resolved Department/Designation/ReportingManager names.</summary>
    /// <response code="200">The employee was found.</response>
    /// <response code="404">No employee was found for the given id.</response>
    [Authorize]
    [RequirePermission("workforce-admin.view")]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _service.GetByIdAsync(id, cancellationToken);
        return result.IsSuccess ? Ok(Envelope(result.Value)) : MapFailure(result.ErrorCode!, result.Error!);
    }

    /// <summary>Updates an employee.</summary>
    /// <response code="200">The employee was updated.</response>
    /// <response code="400">The request failed validation, or a reference is invalid.</response>
    /// <response code="404">No employee was found for the given id.</response>
    [Authorize]
    [RequirePermission("workforce-admin.edit")]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateEmployeeRequest request, CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return BadRequest(BuildRequestRequiredError());
        }

        var validation = await _updateValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(BuildValidationError(validation));
        }

        var result = await _service.UpdateAsync(id, request, actorId: User.GetUserId(), cancellationToken);
        return result.IsSuccess ? Ok(Envelope(result.Value)) : MapFailure(result.ErrorCode!, result.Error!);
    }

    /// <summary>Soft-deletes an employee.</summary>
    /// <response code="204">The employee was deleted.</response>
    /// <response code="404">No employee was found for the given id.</response>
    [Authorize]
    [RequirePermission("workforce-admin.delete")]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await _service.DeleteAsync(id, actorId: User.GetUserId(), cancellationToken);
        return result.IsSuccess ? NoContent() : MapFailure(result.ErrorCode!, result.Error!);
    }

    /// <summary>Activates an employee (the generic IsActive toggle — independent of EmploymentStatus).</summary>
    /// <response code="200">The employee is now active.</response>
    /// <response code="404">No employee was found for the given id.</response>
    [Authorize]
    [RequirePermission("workforce-admin.edit")]
    [HttpPost("{id:guid}/activate")]
    public async Task<IActionResult> Activate(Guid id, CancellationToken cancellationToken)
    {
        var result = await _service.ActivateAsync(id, actorId: User.GetUserId(), cancellationToken);
        return result.IsSuccess ? Ok(Envelope(result.Value)) : MapFailure(result.ErrorCode!, result.Error!);
    }

    /// <summary>Deactivates an employee (the generic IsActive toggle — independent of EmploymentStatus).</summary>
    /// <response code="200">The employee is now inactive.</response>
    /// <response code="404">No employee was found for the given id.</response>
    [Authorize]
    [RequirePermission("workforce-admin.edit")]
    [HttpPost("{id:guid}/deactivate")]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken cancellationToken)
    {
        var result = await _service.DeactivateAsync(id, actorId: User.GetUserId(), cancellationToken);
        return result.IsSuccess ? Ok(Envelope(result.Value)) : MapFailure(result.ErrorCode!, result.Error!);
    }

    private static ApiResponse<EmployeeResponse> Envelope(EmployeeResponse? data) => new() { Data = data };

    private IActionResult MapFailure(string errorCode, string message)
    {
        var status = errorCode switch
        {
            HRErrorCodes.NotFound => StatusCodes.Status404NotFound,
            _ => StatusCodes.Status400BadRequest,
        };

        var error = new ApiErrorResponse { ErrorCode = errorCode, Message = message, CorrelationId = HttpContext.GetCorrelationId(), Timestamp = DateTime.UtcNow };
        return StatusCode(status, error);
    }

    private ApiErrorResponse BuildValidationError(ValidationResult validation) => new()
    {
        ErrorCode = "VALIDATION.FAILED",
        Message = "One or more validation errors occurred.",
        ValidationErrors = validation.Errors.Select(e => new ValidationErrorItem { Field = e.PropertyName, Message = e.ErrorMessage }).ToList(),
        CorrelationId = HttpContext.GetCorrelationId(),
        Timestamp = DateTime.UtcNow,
    };

    private ApiErrorResponse BuildRequestRequiredError() => new()
    {
        ErrorCode = "VALIDATION.FAILED",
        Message = "The request body is missing or could not be parsed.",
        CorrelationId = HttpContext.GetCorrelationId(),
        Timestamp = DateTime.UtcNow,
    };
}
