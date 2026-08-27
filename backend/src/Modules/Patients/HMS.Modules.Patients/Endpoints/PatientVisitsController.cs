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
/// Records a patient's registration/encounter events ("Registration Details" on the
/// frontend). A separate controller from PatientsController: PatientVisit is its own
/// aggregate root with its own repository/service, not a child collection off Patient the
/// way Allergies/EmergencyContacts are.
/// </summary>
[ApiController]
[Authorize]
[Route("api/v1/patients/{patientId:guid}/visits")]
public class PatientVisitsController : ControllerBase
{
    private readonly IPatientVisitService _visitService;
    private readonly IValidator<CreatePatientVisitRequest> _createValidator;
    private readonly ILogger<PatientVisitsController> _logger;

    public PatientVisitsController(
        IPatientVisitService visitService,
        IValidator<CreatePatientVisitRequest> createValidator,
        ILogger<PatientVisitsController> logger)
    {
        _visitService = visitService;
        _createValidator = createValidator;
        _logger = logger;
    }

    /// <summary>Records one visit — Encounter Type + (for OP) Appointment Type, plus one or
    /// more consultation lines (primary + any "Add another Consultant" rows). All lines share
    /// the returned VisitId.</summary>
    /// <response code="201">The visit was recorded.</response>
    /// <response code="400">The request failed validation.</response>
    /// <response code="404">No patient was found for the given id, or a referenced
    /// Department/Consultant/AppointmentType/ConsultationType id doesn't exist.</response>
    [RequirePermission("patient-management.edit")]
    [HttpPost]
    public async Task<IActionResult> Create(Guid patientId, [FromBody] CreatePatientVisitRequest request, CancellationToken cancellationToken)
    {
        var validation = await _createValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(BuildValidationError(validation));
        }

        var result = await _visitService.CreateAsync(patientId, request, actorId: User.GetUserId(), cancellationToken);
        if (!result.IsSuccess)
        {
            return MapFailure(result.ErrorCode!, result.Error!);
        }

        _logger.LogInformation("POST /api/v1/patients/{PatientId}/visits succeeded, visit {VisitId}", patientId, result.Value!.VisitId);

        return CreatedAtAction(nameof(GetById), new { patientId, visitId = result.Value!.VisitId }, Envelope(result.Value));
    }

    /// <summary>Gets one visit by id.</summary>
    /// <response code="200">The visit was found.</response>
    /// <response code="404">No matching visit was found for this patient.</response>
    [RequirePermission("patient-management.view")]
    [HttpGet("{visitId:guid}")]
    public async Task<IActionResult> GetById(Guid patientId, Guid visitId, CancellationToken cancellationToken)
    {
        var result = await _visitService.GetByIdAsync(patientId, visitId, cancellationToken);
        return result.IsSuccess ? Ok(Envelope(result.Value)) : MapFailure(result.ErrorCode!, result.Error!);
    }

    /// <summary>Lists every visit recorded for this patient, newest first.</summary>
    /// <response code="200">The patient's visits (possibly empty).</response>
    [RequirePermission("patient-management.view")]
    [HttpGet]
    public async Task<IActionResult> GetByPatientId(Guid patientId, CancellationToken cancellationToken)
    {
        var result = await _visitService.GetByPatientIdAsync(patientId, cancellationToken);
        return result.IsSuccess
            ? Ok(new ApiResponse<IReadOnlyList<PatientVisitResponse>> { Data = result.Value })
            : MapFailure(result.ErrorCode!, result.Error!);
    }

    private static ApiResponse<PatientVisitResponse> Envelope(PatientVisitResponse? data) => new() { Data = data };

    private IActionResult MapFailure(string errorCode, string message)
    {
        var status = errorCode switch
        {
            PatientErrorCodes.NotFound => StatusCodes.Status404NotFound,
            PatientErrorCodes.VisitNotFound => StatusCodes.Status404NotFound,
            PatientErrorCodes.InvalidDepartment => StatusCodes.Status404NotFound,
            PatientErrorCodes.InvalidConsultant => StatusCodes.Status404NotFound,
            PatientErrorCodes.InvalidAppointmentType => StatusCodes.Status404NotFound,
            PatientErrorCodes.InvalidConsultationType => StatusCodes.Status404NotFound,
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
