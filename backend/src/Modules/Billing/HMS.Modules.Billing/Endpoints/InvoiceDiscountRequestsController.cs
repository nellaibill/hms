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
/// Bill discount requests (ADR-109). Anyone who can make bills ("finance-billing.create") asks
/// for a discount on lines of an unpaid bill with a reason; only a Super Admin can approve or
/// reject it, and approving puts the discount on the bill. A Super Admin's own request is
/// approved as it is raised. A discount entered while the bill is being made is sent with the
/// bill itself (POST invoices) — these endpoints cover a bill that's already saved.
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

    /// <summary>Approves a Pending discount request and puts the discount on its bill — Super Admin only.</summary>
    /// <response code="200">The request was approved and the bill discounted.</response>
    /// <response code="403">The caller isn't signed in as a Super Admin.</response>
    /// <response code="404">No discount request was found for the given id.</response>
    /// <response code="409">The request was already decided, or the bill was voided or changed.</response>
    [Authorize]
    [RequirePermission("finance-billing.view")]
    [HttpPost("discount-requests/{id:guid}/approve")]
    [ProducesResponseType(typeof(ApiResponse<InvoiceDiscountRequestResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Approve(Guid id, [FromBody] ApproveInvoiceDiscountRequest? request, CancellationToken cancellationToken)
    {
        if (!User.IsSuperAdmin()) return SuperAdminOnly();

        request ??= new ApproveInvoiceDiscountRequest();
        var validation = await _approveValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid) return BadRequest(BuildValidationError(validation));

        var result = await _service.ApproveAsync(id, request, User.GetUserId(), User.GetUsername(), cancellationToken);
        return result.IsSuccess ? Ok(Envelope(result.Value)) : MapFailure(result.ErrorCode!, result.Error!);
    }

    /// <summary>Rejects a Pending discount request — Super Admin only; a reason is required.</summary>
    /// <response code="200">The request was rejected; the bill keeps its price.</response>
    /// <response code="400">No reason given.</response>
    /// <response code="403">The caller isn't signed in as a Super Admin.</response>
    /// <response code="404">No discount request was found for the given id.</response>
    /// <response code="409">The request was already decided.</response>
    [Authorize]
    [RequirePermission("finance-billing.view")]
    [HttpPost("discount-requests/{id:guid}/reject")]
    [ProducesResponseType(typeof(ApiResponse<InvoiceDiscountRequestResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Reject(Guid id, [FromBody] RejectInvoiceDiscountRequest request, CancellationToken cancellationToken)
    {
        if (!User.IsSuperAdmin()) return SuperAdminOnly();
        if (request is null) return BadRequest(BuildRequestRequiredError());

        var validation = await _rejectValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid) return BadRequest(BuildValidationError(validation));

        var result = await _service.RejectAsync(id, request, User.GetUserId(), User.GetUsername(), cancellationToken);
        return result.IsSuccess ? Ok(Envelope(result.Value)) : MapFailure(result.ErrorCode!, result.Error!);
    }

    private static ApiResponse<T> Envelope<T>(T? data) => new() { Data = data };

    private IActionResult SuperAdminOnly() => StatusCode(StatusCodes.Status403Forbidden, new ApiErrorResponse
    {
        ErrorCode = BillingErrorCodes.DiscountSuperAdminOnly,
        Message = "Only a Super Admin can approve or reject a discount request.",
        CorrelationId = HttpContext.GetCorrelationId(),
        Timestamp = DateTime.UtcNow,
    });

    private IActionResult MapFailure(string errorCode, string message)
    {
        var status = errorCode switch
        {
            BillingErrorCodes.NotFound => StatusCodes.Status404NotFound,
            BillingErrorCodes.DiscountRequestNotFound => StatusCodes.Status404NotFound,
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
