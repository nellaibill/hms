using FluentValidation;
using FluentValidation.Results;
using HMS.Modules.Patients.Application;
using HMS.Modules.Patients.Contracts;
using HMS.Shared.Infrastructure;
using HMS.Shared.Kernel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace HMS.Modules.Patients.Endpoints;

/// <summary>
/// The Patients module's HTTP surface — registration, demographic/address update, soft
/// delete, paged/search listing, and per-row Allergy/Emergency Contact add-remove. File
/// uploads (photo, ID proof) go through the Documents module's own generic endpoints
/// (ownerType=Patient), not through this controller. "Actor" (created/updated-by) is read
/// from the caller's JWT via ClaimsPrincipalExtensions.GetUserId.
/// </summary>
[ApiController]
[Authorize]
[Route("api/v1/patients")]
public class PatientsController : ControllerBase
{
    private readonly IPatientService _patientService;
    private readonly IValidator<CreatePatientRequest> _createValidator;
    private readonly IValidator<UpdatePatientRequest> _updateValidator;
    private readonly IValidator<AddAllergyRequest> _addAllergyValidator;
    private readonly IValidator<AddEmergencyContactRequest> _addEmergencyContactValidator;
    private readonly ILogger<PatientsController> _logger;

    public PatientsController(
        IPatientService patientService,
        IValidator<CreatePatientRequest> createValidator,
        IValidator<UpdatePatientRequest> updateValidator,
        IValidator<AddAllergyRequest> addAllergyValidator,
        IValidator<AddEmergencyContactRequest> addEmergencyContactValidator,
        ILogger<PatientsController> logger)
    {
        _patientService = patientService;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _addAllergyValidator = addAllergyValidator;
        _addEmergencyContactValidator = addEmergencyContactValidator;
        _logger = logger;
    }

    /// <summary>Registers a new patient — Patient Info + Address + any Allergies/Emergency
    /// Contacts supplied up front, in one transaction.</summary>
    /// <response code="201">The patient was registered.</response>
    /// <response code="400">The request failed validation.</response>
    /// <response code="409">A matching patient (name + phone [+ ID number]) is already registered.</response>
    [RequirePermission("patient-management.create")]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreatePatientRequest request, CancellationToken cancellationToken)
    {
        var validation = await _createValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(BuildValidationError(validation));
        }

        var result = await _patientService.CreateAsync(request, actorId: User.GetUserId(), cancellationToken);
        if (!result.IsSuccess)
        {
            return MapFailure(result.ErrorCode!, result.Error!);
        }

        _logger.LogInformation("POST /api/v1/patients succeeded for UHID {Uhid}", result.Value!.Uhid);

        return CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, Envelope(result.Value));
    }

    /// <summary>Updates a patient's demographic/contact/address/mode-of-arrival fields.</summary>
    /// <response code="200">The patient was updated.</response>
    /// <response code="400">The request failed validation.</response>
    /// <response code="404">No patient was found for the given id.</response>
    /// <response code="409">The patient was changed by someone else since it was loaded.</response>
    [RequirePermission("patient-management.edit")]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdatePatientRequest request, CancellationToken cancellationToken)
    {
        var validation = await _updateValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(BuildValidationError(validation));
        }

        var result = await _patientService.UpdateAsync(id, request, actorId: User.GetUserId(), cancellationToken);
        return result.IsSuccess ? Ok(Envelope(result.Value)) : MapFailure(result.ErrorCode!, result.Error!);
    }

    /// <summary>Soft-deletes a patient.</summary>
    /// <response code="204">The patient was deleted.</response>
    /// <response code="404">No patient was found for the given id.</response>
    [RequirePermission("patient-management.delete")]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await _patientService.DeleteAsync(id, actorId: User.GetUserId(), cancellationToken);
        return result.IsSuccess ? NoContent() : MapFailure(result.ErrorCode!, result.Error!);
    }

    /// <summary>Gets a single patient by id — Patient Info + Address + Allergies + Emergency
    /// Contacts. Documents are fetched separately from the Documents module.</summary>
    /// <response code="200">The patient was found.</response>
    /// <response code="404">No patient was found for the given id.</response>
    [RequirePermission("patient-management.view")]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _patientService.GetByIdAsync(id, cancellationToken);
        return result.IsSuccess ? Ok(Envelope(result.Value)) : MapFailure(result.ErrorCode!, result.Error!);
    }

    /// <summary>Lists patients with paging and search by Name, UHID, or Phone.</summary>
    /// <response code="200">A page of patients.</response>
    [RequirePermission("patient-management.view")]
    [HttpGet]
    public async Task<IActionResult> GetPaged([FromQuery] PatientListQuery query, CancellationToken cancellationToken)
    {
        var paged = await _patientService.GetPagedAsync(query, cancellationToken);

        var meta = new PaginationMeta
        {
            Page = paged.Page,
            PageSize = paged.PageSize,
            TotalCount = paged.TotalCount,
            TotalPages = paged.TotalPages,
        };

        return Ok(new ApiResponse<IReadOnlyList<PatientResponse>> { Data = paged.Items, Meta = meta });
    }

    /// <summary>Adds one allergy row ("Add another Allergy").</summary>
    /// <response code="200">The allergy was added; returns the updated patient.</response>
    /// <response code="400">The request failed validation.</response>
    /// <response code="404">No patient was found for the given id.</response>
    [RequirePermission("patient-management.edit")]
    [HttpPost("{id:guid}/allergies")]
    public async Task<IActionResult> AddAllergy(Guid id, [FromBody] AddAllergyRequest request, CancellationToken cancellationToken)
    {
        var validation = await _addAllergyValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(BuildValidationError(validation));
        }

        var result = await _patientService.AddAllergyAsync(id, request, actorId: User.GetUserId(), cancellationToken);
        return result.IsSuccess ? Ok(Envelope(result.Value)) : MapFailure(result.ErrorCode!, result.Error!);
    }

    /// <summary>Removes one allergy row.</summary>
    /// <response code="200">The allergy was removed; returns the updated patient.</response>
    /// <response code="404">No patient, or no matching allergy, was found.</response>
    [RequirePermission("patient-management.edit")]
    [HttpDelete("{id:guid}/allergies/{allergyId:guid}")]
    public async Task<IActionResult> RemoveAllergy(Guid id, Guid allergyId, CancellationToken cancellationToken)
    {
        var result = await _patientService.RemoveAllergyAsync(id, allergyId, actorId: User.GetUserId(), cancellationToken);
        return result.IsSuccess ? Ok(Envelope(result.Value)) : MapFailure(result.ErrorCode!, result.Error!);
    }

    /// <summary>Adds one emergency contact ("Add another Emergency Contact").</summary>
    /// <response code="200">The contact was added; returns the updated patient.</response>
    /// <response code="400">The request failed validation.</response>
    /// <response code="404">No patient was found for the given id.</response>
    [RequirePermission("patient-management.edit")]
    [HttpPost("{id:guid}/emergency-contacts")]
    public async Task<IActionResult> AddEmergencyContact(Guid id, [FromBody] AddEmergencyContactRequest request, CancellationToken cancellationToken)
    {
        var validation = await _addEmergencyContactValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(BuildValidationError(validation));
        }

        var result = await _patientService.AddEmergencyContactAsync(id, request, actorId: User.GetUserId(), cancellationToken);
        return result.IsSuccess ? Ok(Envelope(result.Value)) : MapFailure(result.ErrorCode!, result.Error!);
    }

    /// <summary>Removes one emergency contact — rejected if it's the patient's last one.</summary>
    /// <response code="200">The contact was removed; returns the updated patient.</response>
    /// <response code="404">No patient, or no matching contact, was found.</response>
    /// <response code="409">This is the patient's only emergency contact.</response>
    [RequirePermission("patient-management.edit")]
    [HttpDelete("{id:guid}/emergency-contacts/{emergencyContactId:guid}")]
    public async Task<IActionResult> RemoveEmergencyContact(Guid id, Guid emergencyContactId, CancellationToken cancellationToken)
    {
        var result = await _patientService.RemoveEmergencyContactAsync(id, emergencyContactId, actorId: User.GetUserId(), cancellationToken);
        return result.IsSuccess ? Ok(Envelope(result.Value)) : MapFailure(result.ErrorCode!, result.Error!);
    }

    private static ApiResponse<PatientResponse> Envelope(PatientResponse? data) => new() { Data = data };

    private IActionResult MapFailure(string errorCode, string message)
    {
        var status = errorCode switch
        {
            PatientErrorCodes.NotFound => StatusCodes.Status404NotFound,
            PatientErrorCodes.AllergyNotFound => StatusCodes.Status404NotFound,
            PatientErrorCodes.EmergencyContactNotFound => StatusCodes.Status404NotFound,
            PatientErrorCodes.DuplicatePatient => StatusCodes.Status409Conflict,
            PatientErrorCodes.ConcurrencyConflict => StatusCodes.Status409Conflict,
            PatientErrorCodes.CannotRemoveLastEmergencyContact => StatusCodes.Status409Conflict,
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
