using FluentValidation;
using FluentValidation.Results;
using HMS.Modules.Billing.Application;
using HMS.Modules.Billing.Contracts;
using HMS.Shared.Infrastructure;
using HMS.Shared.Kernel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HMS.Modules.Billing.Endpoints;

/// <summary>
/// Bill discount requests (ADR-109, ADR-111). Anyone who can make bills ("finance-billing.create")
/// asks for a discount on lines of an unpaid bill with a reason. The request is split into
/// approval parts: a consultation fee's discount is decided by that consultant, every other line
/// by the Super Admin, and the Super Admin may decide any part. Approving a part puts its
/// discounts on the bill. A Super Admin's own request is approved as it is raised. A discount
/// entered while the bill is being made is sent with the bill itself (POST invoices) — these
/// endpoints cover a bill that's already saved, and the decisions.
/// </summary>
[ApiController]
[Route("api/v1/billing")]
public class InvoiceDiscountRequestsController : ControllerBase
{
    private readonly IInvoiceDiscountService _service;
    private readonly IValidator<CreateInvoiceDiscountRequest> _createValidator;
    private readonly IValidator<ApproveInvoiceDiscountRequest> _approveValidator;
    private readonly IValidator<RejectInvoiceDiscountRequest> _rejectValidator;

    public InvoiceDiscountRequestsController(
        IInvoiceDiscountService service,
        IValidator<CreateInvoiceDiscountRequest> createValidator,
        IValidator<ApproveInvoiceDiscountRequest> approveValidator,
        IValidator<RejectInvoiceDiscountRequest> rejectValidator)
    {
        _service = service;
        _createValidator = createValidator;
        _approveValidator = approveValidator;
        _rejectValidator = rejectValidator;
    }

    /// <summary>Whether a discount can be requested for this invoice, and every request raised on it.</summary>
    /// <response code="200">The invoice's discount state.</response>
    /// <response code="404">No invoice was found for the given id.</response>
    [Authorize]
    [RequirePermission("finance-billing.view")]
    [HttpGet("invoices/{invoiceId:guid}/discount")]
    [ProducesResponseType(typeof(ApiResponse<InvoiceDiscountStatusResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetForInvoice(Guid invoiceId, CancellationToken cancellationToken)
    {
        var result = await _service.GetForInvoiceAsync(invoiceId, cancellationToken);
        return result.IsSuccess ? Ok(Envelope(result.Value)) : MapFailure(result.ErrorCode!, result.Error!);
    }

    /// <summary>Asks for a discount on lines of an unpaid bill — a reason is required.</summary>
    /// <response code="201">The request was raised (approved at once for a Super Admin).</response>
    /// <response code="400">No reason or lines given, or a line can't be discounted.</response>
    /// <response code="404">No invoice was found for the given id.</response>
    /// <response code="409">A discount request for this bill is already pending.</response>
    [Authorize]
    [RequirePermission("finance-billing.create")]
    [HttpPost("invoices/{invoiceId:guid}/discount-requests")]
    [ProducesResponseType(typeof(ApiResponse<InvoiceDiscountRequestResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(Guid invoiceId, [FromBody] CreateInvoiceDiscountRequest request, CancellationToken cancellationToken)
    {
        if (request is null) return BadRequest(BuildRequestRequiredError());

        var validation = await _createValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid) return BadRequest(BuildValidationError(validation));

        var result = await _service.CreateAsync(invoiceId, request, User.GetBillingActor(), cancellationToken);
        return result.IsSuccess
            ? StatusCode(StatusCodes.Status201Created, Envelope(result.Value))
            : MapFailure(result.ErrorCode!, result.Error!);
    }

    /// <summary>Lists discount requests, optionally by status — the Super Admin's approval queue.</summary>
    /// <response code="200">A page of discount requests.</response>
    [Authorize]
    [RequirePermission("finance-billing.view")]
    [HttpGet("discount-requests")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<InvoiceDiscountRequestResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPaged([FromQuery] InvoiceDiscountRequestListQuery query, CancellationToken cancellationToken)
    {
        var paged = await _service.GetPagedAsync(query, cancellationToken);
        var meta = new PaginationMeta { Page = paged.Page, PageSize = paged.PageSize, TotalCount = paged.TotalCount, TotalPages = paged.TotalPages };

        return Ok(new ApiResponse<IReadOnlyList<InvoiceDiscountRequestResponse>> { Data = paged.Items, Meta = meta });
    }

    /// <summary>How many discount requests are waiting for a decision — the tab badge.</summary>
    /// <response code="200">The number of Pending requests.</response>
    [Authorize]
    [RequirePermission("finance-billing.view")]
    [HttpGet("discount-requests/pending-count")]
    [ProducesResponseType(typeof(ApiResponse<int>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPendingCount(CancellationToken cancellationToken)
        => Ok(new ApiResponse<int> { Data = await _service.GetPendingCountAsync(cancellationToken) });

    /// <summary>Reasons this hospital has given for discounts before, most-used first — the
    /// suggestions under the counter's Reason box. Needs the same permission as raising a discount.</summary>
    /// <response code="200">Up to <paramref name="limit"/> reasons (default 20, at most 50).</response>
    [Authorize]
    [RequirePermission("finance-billing.create")]
    [HttpGet("discount-requests/reasons")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<ReasonSuggestionResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetReasonSuggestions([FromQuery] int limit = 20, CancellationToken cancellationToken = default)
        => Ok(new ApiResponse<IReadOnlyList<ReasonSuggestionResponse>> { Data = await _service.GetReasonSuggestionsAsync(limit, cancellationToken) });

    /// <summary>A consultant's own discount approvals — requests with a part for their fees,
    /// filtered on that part's status (ADR-111). Consultant logins only; no billing permission
    /// is needed.</summary>
    /// <response code="200">A page of discount requests.</response>
    /// <response code="403">The caller isn't signed in as a consultant linked to a consultant record.</response>
    [Authorize]
    [HttpGet("discount-approvals/mine")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<InvoiceDiscountRequestResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetMine([FromQuery] InvoiceDiscountRequestListQuery query, CancellationToken cancellationToken)
    {
        if (User.GetBillingActor().ConsultantId is not { } consultantId) return ConsultantOnly();

        var paged = await _service.GetPagedForConsultantAsync(consultantId, query, cancellationToken);
        var meta = new PaginationMeta { Page = paged.Page, PageSize = paged.PageSize, TotalCount = paged.TotalCount, TotalPages = paged.TotalPages };

        return Ok(new ApiResponse<IReadOnlyList<InvoiceDiscountRequestResponse>> { Data = paged.Items, Meta = meta });
    }

    /// <summary>How many discount requests wait for the calling consultant — their tab badge.</summary>
    /// <response code="200">The number of requests with a Pending part for this consultant.</response>
    /// <response code="403">The caller isn't signed in as a consultant linked to a consultant record.</response>
    [Authorize]
    [HttpGet("discount-approvals/mine/pending-count")]
    [ProducesResponseType(typeof(ApiResponse<int>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetMinePendingCount(CancellationToken cancellationToken)
    {
        if (User.GetBillingActor().ConsultantId is not { } consultantId) return ConsultantOnly();

        return Ok(new ApiResponse<int> { Data = await _service.GetPendingCountForConsultantAsync(consultantId, cancellationToken) });
    }

    /// <summary>Approves one Pending part of a discount request and puts its discounts on the
    /// bill — the Super Admin (any part) or the consultant whose fee it is (their own part).</summary>
    /// <response code="200">The part was approved and the bill discounted.</response>
    /// <response code="403">The caller may not decide this part.</response>
    /// <response code="404">No approval part was found for the given id.</response>
    /// <response code="409">The part was already decided, or the bill was voided or changed.</response>
    [Authorize]
    [HttpPost("discount-approvals/{approvalId:guid}/approve")]
    [ProducesResponseType(typeof(ApiResponse<InvoiceDiscountRequestResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Approve(Guid approvalId, [FromBody] ApproveInvoiceDiscountRequest? request, CancellationToken cancellationToken)
    {
        request ??= new ApproveInvoiceDiscountRequest();
        var validation = await _approveValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid) return BadRequest(BuildValidationError(validation));

        var result = await _service.ApproveAsync(approvalId, request, User.GetBillingActor(), cancellationToken);
        return result.IsSuccess ? Ok(Envelope(result.Value)) : MapFailure(result.ErrorCode!, result.Error!);
    }

    /// <summary>Rejects one Pending part of a discount request — the Super Admin (any part) or the
    /// consultant whose fee it is (their own part); a reason is required.</summary>
    /// <response code="200">The part was rejected; its lines keep their price.</response>
    /// <response code="400">No reason given.</response>
    /// <response code="403">The caller may not decide this part.</response>
    /// <response code="404">No approval part was found for the given id.</response>
    /// <response code="409">The part was already decided.</response>
    [Authorize]
    [HttpPost("discount-approvals/{approvalId:guid}/reject")]
    [ProducesResponseType(typeof(ApiResponse<InvoiceDiscountRequestResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Reject(Guid approvalId, [FromBody] RejectInvoiceDiscountRequest request, CancellationToken cancellationToken)
    {
        if (request is null) return BadRequest(BuildRequestRequiredError());

        var validation = await _rejectValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid) return BadRequest(BuildValidationError(validation));

        var result = await _service.RejectAsync(approvalId, request, User.GetBillingActor(), cancellationToken);
        return result.IsSuccess ? Ok(Envelope(result.Value)) : MapFailure(result.ErrorCode!, result.Error!);
    }

    private static ApiResponse<T> Envelope<T>(T? data) => new() { Data = data };

    private IActionResult ConsultantOnly() => StatusCode(StatusCodes.Status403Forbidden, new ApiErrorResponse
    {
        ErrorCode = BillingErrorCodes.DiscountConsultantOnly,
        Message = "Discount approvals are listed for a consultant's own login only.",
        CorrelationId = HttpContext.GetCorrelationId(),
        Timestamp = DateTime.UtcNow,
    });

    private IActionResult MapFailure(string errorCode, string message)
    {
        var status = errorCode switch
        {
            BillingErrorCodes.NotFound => StatusCodes.Status404NotFound,
            BillingErrorCodes.DiscountRequestNotFound => StatusCodes.Status404NotFound,
            BillingErrorCodes.DiscountNotYourApproval => StatusCodes.Status403Forbidden,
            BillingErrorCodes.DiscountAlreadyPending => StatusCodes.Status409Conflict,
            BillingErrorCodes.DiscountRequestNotPending => StatusCodes.Status409Conflict,
            BillingErrorCodes.AlreadyVoided => StatusCodes.Status409Conflict,
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
