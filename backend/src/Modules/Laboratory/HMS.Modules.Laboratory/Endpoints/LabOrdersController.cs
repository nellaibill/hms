using FluentValidation;
using FluentValidation.Results;
using HMS.Modules.Laboratory.Application;
using HMS.Modules.Laboratory.Contracts;
using HMS.Shared.Infrastructure;
using HMS.Shared.Kernel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HMS.Modules.Laboratory.Endpoints;

/// <summary>
/// The lab worklist: sample collection through result entry, verification, and report
/// generation/release. Gated by the existing "diagnostics.*" permissions (seeded once for
/// Masters' diagnostics-admin screens, reused here rather than adding a new permission
/// group) — which human role gets which permission is a tenant admin's own Roles
/// configuration, not something this controller decides. Also gated by
/// [RequireFeature("laboratory")] — every other optional module's controller has this
/// (see e.g. Pharmacy's StockReceiptsController); a tenant that hasn't enabled the
/// "laboratory" feature must not be able to reach this API even if they hold "diagnostics.*"
/// permissions from the unrelated, always-available Masters diagnostics-admin catalog.
///
/// Deliberately has no POST /orders action: ILabOrderService.CreateFromInvoiceAsync is only
/// ever called in-process by Billing's InvoiceService right after an invoice with a
/// Laboratory line item is persisted — lab staff never manually create a patient billing
/// request, only Reception/Billing does.
/// </summary>
[ApiController]
[RequireFeature("laboratory")]
[Route("api/v1/laboratory/orders")]
public class LabOrdersController : ControllerBase
{
    private readonly ILabOrderService _service;
    private readonly IValidator<CollectSampleRequest> _collectSampleValidator;
    private readonly IValidator<RejectSampleRequest> _rejectSampleValidator;
    private readonly IValidator<SaveResultDraftRequest> _saveResultDraftValidator;
    private readonly IValidator<RejectForCorrectionRequest> _rejectForCorrectionValidator;

    public LabOrdersController(
        ILabOrderService service,
        IValidator<CollectSampleRequest> collectSampleValidator,
        IValidator<RejectSampleRequest> rejectSampleValidator,
        IValidator<SaveResultDraftRequest> saveResultDraftValidator,
        IValidator<RejectForCorrectionRequest> rejectForCorrectionValidator)
    {
        _service = service;
        _collectSampleValidator = collectSampleValidator;
        _rejectSampleValidator = rejectSampleValidator;
        _saveResultDraftValidator = saveResultDraftValidator;
        _rejectForCorrectionValidator = rejectForCorrectionValidator;
    }

    /// <summary>Lists lab orders with paging, search, sorting, and status/priority/date-range filtering — the lab worklist.</summary>
    [Authorize]
    [RequirePermission("diagnostics.view")]
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<LabOrderResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPaged([FromQuery] LabOrderListQuery query, CancellationToken cancellationToken)
    {
        var paged = await _service.GetPagedAsync(query, cancellationToken);
        var meta = new PaginationMeta { Page = paged.Page, PageSize = paged.PageSize, TotalCount = paged.TotalCount, TotalPages = paged.TotalPages };

        return Ok(new ApiResponse<IReadOnlyList<LabOrderResponse>> { Data = paged.Items, Meta = meta });
    }

    /// <summary>Aggregated worklist dashboard tiles (pending collection, in progress, pending verification, reports ready, etc.) for the current tenant.</summary>
    [Authorize]
    [RequirePermission("diagnostics.view")]
    [HttpGet("dashboard-summary")]
    [ProducesResponseType(typeof(ApiResponse<LabDashboardSummaryResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDashboardSummary(CancellationToken cancellationToken)
    {
        var result = await _service.GetDashboardSummaryAsync(cancellationToken);
        return result.IsSuccess ? Ok(Envelope(result.Value)) : MapFailure(result.ErrorCode!, result.Error!);
    }

    /// <summary>Gets a single lab order by id, with every item, its result parameters, and its audit event history.</summary>
    [Authorize]
    [RequirePermission("diagnostics.view")]
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<LabOrderResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _service.GetByIdAsync(id, cancellationToken);
        return result.IsSuccess ? Ok(Envelope(result.Value)) : MapFailure(result.ErrorCode!, result.Error!);
    }

    /// <summary>Lists every lab order for one patient, newest first.</summary>
    [Authorize]
    [RequirePermission("diagnostics.view")]
    [HttpGet("by-patient/{patientId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<LabOrderResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByPatientId(Guid patientId, CancellationToken cancellationToken)
    {
        var result = await _service.GetByPatientIdAsync(patientId, cancellationToken);
        return result.IsSuccess ? Ok(Envelope(result.Value)) : MapFailure(result.ErrorCode!, result.Error!);
    }

    /// <summary>Records sample collection for one item — valid from PendingCollection or RecollectionRequired.</summary>
    [Authorize]
    [RequirePermission("diagnostics.create")]
    [HttpPost("items/{itemId:guid}/collect-sample")]
    [ProducesResponseType(typeof(ApiResponse<LabOrderResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CollectSample(Guid itemId, [FromBody] CollectSampleRequest request, CancellationToken cancellationToken)
    {
        if (request is null) return BadRequest(BuildRequestRequiredError());

        var validation = await _collectSampleValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid) return BadRequest(BuildValidationError(validation));

        var result = await _service.CollectSampleAsync(itemId, request, actorId: User.GetUserId(), cancellationToken);
        return result.IsSuccess ? Ok(Envelope(result.Value)) : MapFailure(result.ErrorCode!, result.Error!);
    }

    /// <summary>Rejects a collected sample — valid only from Collected.</summary>
    [Authorize]
    [RequirePermission("diagnostics.create")]
    [HttpPost("items/{itemId:guid}/reject-sample")]
    [ProducesResponseType(typeof(ApiResponse<LabOrderResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RejectSample(Guid itemId, [FromBody] RejectSampleRequest request, CancellationToken cancellationToken)
    {
        if (request is null) return BadRequest(BuildRequestRequiredError());

        var validation = await _rejectSampleValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid) return BadRequest(BuildValidationError(validation));

        var result = await _service.RejectSampleAsync(itemId, request, actorId: User.GetUserId(), cancellationToken);
        return result.IsSuccess ? Ok(Envelope(result.Value)) : MapFailure(result.ErrorCode!, result.Error!);
    }

    /// <summary>Requests a recollection for a rejected sample — valid only from Rejected.</summary>
    [Authorize]
    [RequirePermission("diagnostics.create")]
    [HttpPost("items/{itemId:guid}/recollect")]
    [ProducesResponseType(typeof(ApiResponse<LabOrderResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RequestRecollection(Guid itemId, CancellationToken cancellationToken)
    {
        var result = await _service.RequestRecollectionAsync(itemId, actorId: User.GetUserId(), cancellationToken);
        return result.IsSuccess ? Ok(Envelope(result.Value)) : MapFailure(result.ErrorCode!, result.Error!);
    }

    /// <summary>Marks a collected sample as received by the lab — valid only from Collected.</summary>
    [Authorize]
    [RequirePermission("diagnostics.create")]
    [HttpPost("items/{itemId:guid}/receive")]
    [ProducesResponseType(typeof(ApiResponse<LabOrderResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ReceiveSample(Guid itemId, CancellationToken cancellationToken)
    {
        var result = await _service.ReceiveSampleAsync(itemId, actorId: User.GetUserId(), cancellationToken);
        return result.IsSuccess ? Ok(Envelope(result.Value)) : MapFailure(result.ErrorCode!, result.Error!);
    }

    /// <summary>Starts processing a received sample — valid from Received or CorrectionRequired.</summary>
    [Authorize]
    [RequirePermission("diagnostics.create")]
    [HttpPost("items/{itemId:guid}/start-processing")]
    [ProducesResponseType(typeof(ApiResponse<LabOrderResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> StartProcessing(Guid itemId, CancellationToken cancellationToken)
    {
        var result = await _service.StartProcessingAsync(itemId, actorId: User.GetUserId(), cancellationToken);
        return result.IsSuccess ? Ok(Envelope(result.Value)) : MapFailure(result.ErrorCode!, result.Error!);
    }

    /// <summary>Saves (replaces) the full set of result parameters for one item — valid from Processing, ResultEntryInProgress, or CorrectionRequired.</summary>
    [Authorize]
    [RequirePermission("diagnostics.create")]
    [HttpPut("items/{itemId:guid}/result-draft")]
    [ProducesResponseType(typeof(ApiResponse<LabOrderResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> SaveResultDraft(Guid itemId, [FromBody] SaveResultDraftRequest request, CancellationToken cancellationToken)
    {
        if (request is null) return BadRequest(BuildRequestRequiredError());

        var validation = await _saveResultDraftValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid) return BadRequest(BuildValidationError(validation));

        var result = await _service.SaveResultDraftAsync(itemId, request, actorId: User.GetUserId(), cancellationToken);
        return result.IsSuccess ? Ok(Envelope(result.Value)) : MapFailure(result.ErrorCode!, result.Error!);
    }

    /// <summary>Submits the drafted result for verification — valid only from ResultEntryInProgress, requires at least one saved parameter.</summary>
    [Authorize]
    [RequirePermission("diagnostics.create")]
    [HttpPost("items/{itemId:guid}/submit-for-verification")]
    [ProducesResponseType(typeof(ApiResponse<LabOrderResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> SubmitForVerification(Guid itemId, CancellationToken cancellationToken)
    {
        var result = await _service.SubmitForVerificationAsync(itemId, actorId: User.GetUserId(), cancellationToken);
        return result.IsSuccess ? Ok(Envelope(result.Value)) : MapFailure(result.ErrorCode!, result.Error!);
    }

    /// <summary>Verifies a submitted result — valid only from PendingVerification. Requires the more sensitive "edit" permission, not "create".</summary>
    [Authorize]
    [RequirePermission("diagnostics.edit")]
    [HttpPost("items/{itemId:guid}/verify")]
    [ProducesResponseType(typeof(ApiResponse<LabOrderResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Verify(Guid itemId, CancellationToken cancellationToken)
    {
        var result = await _service.VerifyAsync(itemId, actorId: User.GetUserId(), cancellationToken);
        return result.IsSuccess ? Ok(Envelope(result.Value)) : MapFailure(result.ErrorCode!, result.Error!);
    }

    /// <summary>Sends a submitted result back for correction — valid only from PendingVerification. Requires "edit", same as Verify.</summary>
    [Authorize]
    [RequirePermission("diagnostics.edit")]
    [HttpPost("items/{itemId:guid}/reject-for-correction")]
    [ProducesResponseType(typeof(ApiResponse<LabOrderResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RejectForCorrection(Guid itemId, [FromBody] RejectForCorrectionRequest request, CancellationToken cancellationToken)
    {
        if (request is null) return BadRequest(BuildRequestRequiredError());

        var validation = await _rejectForCorrectionValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid) return BadRequest(BuildValidationError(validation));

        var result = await _service.RejectForCorrectionAsync(itemId, request, actorId: User.GetUserId(), cancellationToken);
        return result.IsSuccess ? Ok(Envelope(result.Value)) : MapFailure(result.ErrorCode!, result.Error!);
    }

    /// <summary>Generates the order's report — requires every item to be Verified first.</summary>
    [Authorize]
    [RequirePermission("diagnostics.edit")]
    [HttpPost("{id:guid}/generate-report")]
    [ProducesResponseType(typeof(ApiResponse<LabOrderResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> GenerateReport(Guid id, CancellationToken cancellationToken)
    {
        var result = await _service.GenerateReportAsync(id, actorId: User.GetUserId(), cancellationToken);
        return result.IsSuccess ? Ok(Envelope(result.Value)) : MapFailure(result.ErrorCode!, result.Error!);
    }

    /// <summary>Releases a generated report — requires GenerateReport to have run first, and can only happen once.</summary>
    [Authorize]
    [RequirePermission("diagnostics.edit")]
    [HttpPost("{id:guid}/release-report")]
    [ProducesResponseType(typeof(ApiResponse<LabOrderResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ReleaseReport(Guid id, CancellationToken cancellationToken)
    {
        var result = await _service.ReleaseReportAsync(id, actorId: User.GetUserId(), cancellationToken);
        return result.IsSuccess ? Ok(Envelope(result.Value)) : MapFailure(result.ErrorCode!, result.Error!);
    }

    private static ApiResponse<T> Envelope<T>(T? data) => new() { Data = data };

    private IActionResult MapFailure(string errorCode, string message)
    {
        var status = errorCode switch
        {
            LaboratoryErrorCodes.OrderNotFound => StatusCodes.Status404NotFound,
            LaboratoryErrorCodes.ItemNotFound => StatusCodes.Status404NotFound,
            LaboratoryErrorCodes.InvalidStatusTransition => StatusCodes.Status409Conflict,
            LaboratoryErrorCodes.NotAllItemsVerified => StatusCodes.Status409Conflict,
            LaboratoryErrorCodes.ReportNotGenerated => StatusCodes.Status409Conflict,
            LaboratoryErrorCodes.AlreadyReleased => StatusCodes.Status409Conflict,
            LaboratoryErrorCodes.EmptyOrder => StatusCodes.Status400BadRequest,
            LaboratoryErrorCodes.InvalidServiceOrPackage => StatusCodes.Status400BadRequest,
            _ => StatusCodes.Status400BadRequest,
        };

        return StatusCode(status, new ApiErrorResponse
        {
            ErrorCode = errorCode,
            Message = message,
            CorrelationId = HttpContext.GetCorrelationId(),
            Timestamp = DateTime.UtcNow,
        });
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

    private ApiErrorResponse BuildRequestRequiredError() => new()
    {
        ErrorCode = "VALIDATION.FAILED",
        Message = "The request body is missing or could not be parsed.",
        CorrelationId = HttpContext.GetCorrelationId(),
        Timestamp = DateTime.UtcNow,
    };
}
