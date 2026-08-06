using FluentValidation;
using FluentValidation.Results;
using HMS.Modules.Patients.Application;
using HMS.Modules.Patients.Contracts;
using HMS.Shared.Infrastructure;
using HMS.Shared.Kernel;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace HMS.Modules.Patients.Endpoints;

/// <summary>
/// The Patients module's HTTP surface — New Patient Registration (combined patient +
/// first encounter), demographic update, soft delete, paged/search listing, and
/// photo/ID-proof upload, per docs/PatientRegistrationModule.md's MVP scope (see
/// docs/DecisionLog.md for what's deferred). "Actor" (created/updated-by) is read from
/// the caller's JWT via ClaimsPrincipalExtensions.GetUserId — same as
/// HMS.Modules.Identity.UsersController.
/// </summary>
[ApiController]
[Route("api/v1/patients")]
public class PatientsController : ControllerBase
{
    private readonly IPatientService _patientService;
    private readonly IValidator<CreatePatientRequest> _createValidator;
    private readonly IValidator<UpdatePatientRequest> _updateValidator;
    private readonly IValidator<PatientRegistrationDetails> _registrationValidator;
    private readonly ILogger<PatientsController> _logger;

    public PatientsController(
        IPatientService patientService,
        IValidator<CreatePatientRequest> createValidator,
        IValidator<UpdatePatientRequest> updateValidator,
        IValidator<PatientRegistrationDetails> registrationValidator,
        ILogger<PatientsController> logger)
    {
        _patientService = patientService;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _registrationValidator = registrationValidator;
        _logger = logger;
    }

    /// <summary>Registers a new patient and their first encounter in one transaction.</summary>
    /// <response code="201">The patient was registered.</response>
    /// <response code="400">The request failed validation.</response>
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

    /// <summary>Updates a patient's demographic/master-data fields.</summary>
    /// <response code="200">The patient was updated.</response>
    /// <response code="400">The request failed validation.</response>
    /// <response code="404">No patient was found for the given id.</response>
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
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await _patientService.DeleteAsync(id, actorId: User.GetUserId(), cancellationToken);
        return result.IsSuccess ? NoContent() : MapFailure(result.ErrorCode!, result.Error!);
    }

    /// <summary>Gets a single patient by id.</summary>
    /// <response code="200">The patient was found.</response>
    /// <response code="404">No patient was found for the given id.</response>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _patientService.GetByIdAsync(id, cancellationToken);
        return result.IsSuccess ? Ok(Envelope(result.Value)) : MapFailure(result.ErrorCode!, result.Error!);
    }

    /// <summary>Records a new encounter/visit for an existing patient — a returning patient
    /// needs this, since Create only ever produces their first registration.</summary>
    /// <response code="201">The registration was recorded.</response>
    /// <response code="400">The request failed validation.</response>
    /// <response code="404">No patient was found for the given id.</response>
    [HttpPost("{id:guid}/registrations")]
    public async Task<IActionResult> AddRegistration(Guid id, [FromBody] PatientRegistrationDetails request, CancellationToken cancellationToken)
    {
        var validation = await _registrationValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(BuildValidationError(validation));
        }

        var result = await _patientService.AddRegistrationAsync(id, request, actorId: User.GetUserId(), cancellationToken);
        if (!result.IsSuccess)
        {
            return MapFailure(result.ErrorCode!, result.Error!);
        }

        _logger.LogInformation("POST /api/v1/patients/{PatientId}/registrations succeeded for {RegistrationNumber}", id, result.Value!.RegistrationNumber);

        return StatusCode(StatusCodes.Status201Created, new ApiResponse<PatientRegistrationResponse> { Data = result.Value });
    }

    /// <summary>Lists every encounter/visit a patient has had, newest first.</summary>
    /// <response code="200">The patient's registration history.</response>
    /// <response code="404">No patient was found for the given id.</response>
    [HttpGet("{id:guid}/registrations")]
    public async Task<IActionResult> GetRegistrations(Guid id, CancellationToken cancellationToken)
    {
        var result = await _patientService.GetRegistrationsAsync(id, cancellationToken);
        return result.IsSuccess
            ? Ok(new ApiResponse<IReadOnlyList<PatientRegistrationResponse>> { Data = result.Value })
            : MapFailure(result.ErrorCode!, result.Error!);
    }

    /// <summary>
    /// Lists patients with paging and search by Name, UHID, or Phone (see
    /// docs/PatientRegistrationModule.md §8 — duplicate-confidence ranking is deferred).
    /// </summary>
    /// <response code="200">A page of patients.</response>
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

    /// <summary>Uploads/replaces a patient's photo (JPG/PNG, max 5MB).</summary>
    /// <response code="200">The photo was uploaded.</response>
    /// <response code="400">The file failed validation.</response>
    /// <response code="404">No patient was found for the given id.</response>
    [HttpPost("{id:guid}/photo")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadPhoto(Guid id, IFormFile file, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(BuildFileRequiredError());
        }

        await using var stream = file.OpenReadStream();
        var result = await _patientService.UploadPhotoAsync(id, stream, file.FileName, file.ContentType, file.Length, actorId: User.GetUserId(), cancellationToken);
        return result.IsSuccess ? Ok(Envelope(result.Value)) : MapFailure(result.ErrorCode!, result.Error!);
    }

    /// <summary>Uploads/replaces a patient's ID proof (JPG/PNG/PDF, max 5MB).</summary>
    /// <response code="200">The ID proof was uploaded.</response>
    /// <response code="400">The file failed validation.</response>
    /// <response code="404">No patient was found for the given id.</response>
    [HttpPost("{id:guid}/id-proof")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadIdProof(Guid id, IFormFile file, [FromForm] IdProofType idProofType, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(BuildFileRequiredError());
        }

        await using var stream = file.OpenReadStream();
        var result = await _patientService.UploadIdProofAsync(id, idProofType, stream, file.FileName, file.ContentType, file.Length, actorId: User.GetUserId(), cancellationToken);
        return result.IsSuccess ? Ok(Envelope(result.Value)) : MapFailure(result.ErrorCode!, result.Error!);
    }

    private static ApiResponse<PatientResponse> Envelope(PatientResponse? data) => new() { Data = data };

    private ApiErrorResponse BuildFileRequiredError() => new()
    {
        ErrorCode = "VALIDATION.FAILED",
        Message = "A file is required.",
        CorrelationId = HttpContext.GetCorrelationId(),
        Timestamp = DateTime.UtcNow,
    };

    private IActionResult MapFailure(string errorCode, string message)
    {
        var status = errorCode switch
        {
            PatientErrorCodes.NotFound => StatusCodes.Status404NotFound,
            PatientErrorCodes.InvalidFile => StatusCodes.Status400BadRequest,
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
