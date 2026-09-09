using FluentValidation;
using FluentValidation.Results;
using HMS.Modules.Masters.Application;
using HMS.Modules.Patients.Application;
using HMS.Modules.Patients.Application.Excel;
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
    private readonly IDepartmentService _departmentService;
    private readonly IValidator<CreatePatientRequest> _createValidator;
    private readonly IValidator<UpdatePatientRequest> _updateValidator;
    private readonly IValidator<AddAllergyRequest> _addAllergyValidator;
    private readonly IValidator<AddEmergencyContactRequest> _addEmergencyContactValidator;
    private readonly ILogger<PatientsController> _logger;

    public PatientsController(
        IPatientService patientService,
        IDepartmentService departmentService,
        IValidator<CreatePatientRequest> createValidator,
        IValidator<UpdatePatientRequest> updateValidator,
        IValidator<AddAllergyRequest> addAllergyValidator,
        IValidator<AddEmergencyContactRequest> addEmergencyContactValidator,
        ILogger<PatientsController> logger)
    {
        _patientService = patientService;
        _departmentService = departmentService;
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

    /// <summary>Patient Reports' table — same filters/pagination/sort as the plain list above,
    /// enriched with each row's last visit and department.</summary>
    /// <response code="200">A page of report rows.</response>
    [RequirePermission("patient-management.view")]
    [HttpGet("report")]
    public async Task<IActionResult> GetReportPaged([FromQuery] PatientListQuery query, CancellationToken cancellationToken)
    {
        var paged = await _patientService.GetReportPagedAsync(query, cancellationToken);

        var meta = new PaginationMeta
        {
            Page = paged.Page,
            PageSize = paged.PageSize,
            TotalCount = paged.TotalCount,
            TotalPages = paged.TotalPages,
        };

        return Ok(new ApiResponse<IReadOnlyList<PatientReportRowResponse>> { Data = paged.Items, Meta = meta });
    }

    /// <summary>Patient Reports' four summary-card numbers for the same filters.</summary>
    /// <response code="200">The summary.</response>
    [RequirePermission("patient-management.view")]
    [HttpGet("report/summary")]
    public async Task<IActionResult> GetReportSummary([FromQuery] PatientListQuery query, CancellationToken cancellationToken)
    {
        var summary = await _patientService.GetReportSummaryAsync(query, cancellationToken);
        return Ok(new ApiResponse<PatientReportSummaryResponse> { Data = summary });
    }

    private const string XlsxContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    /// <summary>Excel export of every row matching the current filters (capped at 5,000 rows,
    /// same rate-limit/connection concerns as the frontend's own report page — see
    /// docs/DecisionLog.md's Patient Reports ADR), generated server-side rather than assembled
    /// from whatever the browser already has paginated in memory.</summary>
    /// <response code="200">The .xlsx file.</response>
    [RequirePermission("patient-management.view")]
    [HttpGet("report/export")]
    public async Task<IActionResult> ExportReport([FromQuery] PatientListQuery query, CancellationToken cancellationToken)
    {
        const int maxRows = 5000;
        var rows = await _patientService.GetReportExportRowsAsync(query, maxRows, cancellationToken);

        var departmentIds = rows.Select(r => r.LastVisitDepartmentId).Where(id => id.HasValue).Select(id => id!.Value).Distinct().ToList();
        var departmentNames = new Dictionary<Guid, string>();
        foreach (var departmentId in departmentIds)
        {
            var department = await _departmentService.GetByIdAsync(departmentId, cancellationToken);
            if (department.IsSuccess) departmentNames[departmentId] = department.Value!.Name;
        }

        var bytes = PatientReportExportGenerator.Generate(rows, departmentNames);
        _logger.LogInformation("Exported {RowCount} patient report rows to Excel", rows.Count);

        return File(bytes, XlsxContentType, $"patient-report-{DateTime.UtcNow:yyyyMMdd-HHmmss}.xlsx");
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
