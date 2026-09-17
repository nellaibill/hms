using HMS.Modules.Patients.Application;
using HMS.Modules.Patients.Contracts;
using HMS.Shared.Infrastructure;
using HMS.Shared.Kernel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HMS.Modules.Patients.Endpoints;

/// <summary>
/// The Out Patient Department (OPD) queue — read/transition endpoints over
/// PatientVisitConsultation's AppointmentTime/Status (see that entity's own doc comment for
/// why this rides on the existing Registration Details data rather than a new Appointment
/// concept). A separate controller from PatientVisitsController: that one owns creating
/// visits/consultations, this one owns querying and advancing the queue built from them.
/// </summary>
[ApiController]
[Authorize]
[Route("api/v1/opd")]
public class OpdController : ControllerBase
{
    private readonly IOpdQueryService _service;

    public OpdController(IOpdQueryService service)
    {
        _service = service;
    }

    /// <summary>The OPD Patient List — one row per consultation matching the filters, paged.</summary>
    /// <response code="200">A page of OPD patient rows.</response>
    [RequirePermission("clinical-care.view")]
    [HttpGet("patients")]
    public async Task<IActionResult> GetPatientList([FromQuery] OpdPatientListQuery query, CancellationToken cancellationToken)
    {
        // A consultant only ever sees their own patients here — see
        // ClaimsPrincipalExtensions.GetScopedConsultantId's own doc comment for exactly who
        // this does (and doesn't) restrict. Overrides whatever ConsultantId the client asked
        // for, so this can't be bypassed by editing the query string.
        var scopedConsultantId = User.GetScopedConsultantId();
        if (scopedConsultantId is not null)
        {
            query.ConsultantId = scopedConsultantId;
        }

        var paged = await _service.GetPatientListAsync(query, cancellationToken);
        var meta = new PaginationMeta { Page = paged.Page, PageSize = paged.PageSize, TotalCount = paged.TotalCount, TotalPages = paged.TotalPages };
        return Ok(new ApiResponse<IReadOnlyList<OpdPatientListItem>> { Data = paged.Items, Meta = meta });
    }

    /// <summary>The OPD Consultation List — one row per consultant with Waiting/InConsultation/
    /// Completed counts across their matching consultations.</summary>
    /// <response code="200">The per-consultant summary rows.</response>
    [RequirePermission("clinical-care.view")]
    [HttpGet("consultations/summary")]
    public async Task<IActionResult> GetConsultationSummary([FromQuery] OpdConsultationSummaryQuery query, CancellationToken cancellationToken)
    {
        // See GetPatientList's own comment — same forced scoping.
        var scopedConsultantId = User.GetScopedConsultantId();
        if (scopedConsultantId is not null)
        {
            query = query with { ConsultantId = scopedConsultantId };
        }

        var result = await _service.GetConsultationSummaryAsync(query, cancellationToken);
        return result.IsSuccess
            ? Ok(new ApiResponse<IReadOnlyList<OpdConsultationSummaryItem>> { Data = result.Value })
            : MapFailure(result.ErrorCode!, result.Error!);
    }

    /// <summary>Front desk marks the patient as physically present.</summary>
    /// <response code="200">The consultation is now CheckedIn.</response>
    /// <response code="404">No matching consultation was found.</response>
    /// <response code="409">The consultation isn't in a state this transition allows.</response>
    [RequirePermission("clinical-care.edit")]
    [HttpPost("consultations/{id:guid}/check-in")]
    public Task<IActionResult> CheckIn(Guid id, CancellationToken cancellationToken) => TransitionAsync(id, "check-in", cancellationToken);

    /// <summary>The "Consult" action — pulls the patient into the consultant's room.</summary>
    /// <response code="200">The consultation is now InConsultation.</response>
    /// <response code="404">No matching consultation was found.</response>
    /// <response code="409">The consultation isn't in a state this transition allows.</response>
    [RequirePermission("clinical-care.edit")]
    [HttpPost("consultations/{id:guid}/start-consultation")]
    public Task<IActionResult> StartConsultation(Guid id, CancellationToken cancellationToken) => TransitionAsync(id, "start-consultation", cancellationToken);

    /// <summary>Marks the consultation finished.</summary>
    /// <response code="200">The consultation is now Completed.</response>
    /// <response code="404">No matching consultation was found.</response>
    /// <response code="409">The consultation isn't in a state this transition allows.</response>
    [RequirePermission("clinical-care.edit")]
    [HttpPost("consultations/{id:guid}/complete")]
    public Task<IActionResult> Complete(Guid id, CancellationToken cancellationToken) => TransitionAsync(id, "complete", cancellationToken);

    /// <summary>Pulls a patient off the queue before their consultation happens.</summary>
    /// <response code="200">The consultation is now Cancelled.</response>
    /// <response code="404">No matching consultation was found.</response>
    /// <response code="409">The consultation isn't in a state this transition allows.</response>
    [RequirePermission("clinical-care.edit")]
    [HttpPost("consultations/{id:guid}/cancel")]
    public Task<IActionResult> Cancel(Guid id, CancellationToken cancellationToken) => TransitionAsync(id, "cancel", cancellationToken);

    /// <summary>Marks that the patient never arrived.</summary>
    /// <response code="200">The consultation is now NoShow.</response>
    /// <response code="404">No matching consultation was found.</response>
    /// <response code="409">The consultation isn't in a state this transition allows.</response>
    [RequirePermission("clinical-care.edit")]
    [HttpPost("consultations/{id:guid}/no-show")]
    public Task<IActionResult> NoShow(Guid id, CancellationToken cancellationToken) => TransitionAsync(id, "no-show", cancellationToken);

    private async Task<IActionResult> TransitionAsync(Guid id, string action, CancellationToken cancellationToken)
    {
        var result = await _service.TransitionAsync(id, action, actorId: User.GetUserId(), cancellationToken);
        return result.IsSuccess
            ? Ok(new ApiResponse<VisitConsultationResponse> { Data = result.Value })
            : MapFailure(result.ErrorCode!, result.Error!);
    }

    private IActionResult MapFailure(string errorCode, string message)
    {
        var status = errorCode switch
        {
            PatientErrorCodes.ConsultationNotFound => StatusCodes.Status404NotFound,
            PatientErrorCodes.InvalidStatusTransition => StatusCodes.Status409Conflict,
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
}
