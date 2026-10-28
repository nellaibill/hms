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
/// OP bill revert requests (ADR-106). Anyone who can make bills ("finance-billing.create" — the
/// front desk's Receptionist role has create but not edit) raises a request with a reason; only a
/// Super Admin can approve or reject it, and approving reverts the bill.
/// "Super Admin" is the caller's login type — the login page only lets a user sign in as
/// superAdmin when their role is the Super Admin role (HMS.Modules.Identity's LoginTypes), the
/// same claim OpdConsultationsController's list gates read.
/// </summary>
[ApiController]
[Route("api/v1/billing")]
public class InvoiceRevertRequestsController : ControllerBase
{
    private const string SuperAdminLoginType = "superAdmin";

    private readonly IInvoiceRevertService _service;
    private readonly IValidator<CreateInvoiceRevertRequest> _createValidator;
    private readonly IValidator<ApproveInvoiceRevertRequest> _approveValidator;
    private readonly IValidator<RejectInvoiceRevertRequest> _rejectValidator;

    public InvoiceRevertRequestsController(
        IInvoiceRevertService service,
        IValidator<CreateInvoiceRevertRequest> createValidator,
        IValidator<ApproveInvoiceRevertRequest> approveValidator,
        IValidator<RejectInvoiceRevertRequest> rejectValidator)
    {
        _service = service;
        _createValidator = createValidator;
        _approveValidator = approveValidator;
        _rejectValidator = rejectValidator;
    }

    private bool IsSuperAdmin => string.Equals(User.FindFirst("LoginType")?.Value, SuperAdminLoginType, StringComparison.OrdinalIgnoreCase);

    private string? ActorName => User.FindFirst("Username")?.Value;

    /// <summary>Whether a revert can be requested for this invoice, and every request raised on it.</summary>
    /// <response code="200">The invoice's revert state.</response>
    /// <response code="404">No invoice was found for the given id.</response>
    [Authorize]
    [RequirePermission("finance-billing.view")]
    [HttpGet("invoices/{invoiceId:guid}/revert")]
    [ProducesResponseType(typeof(ApiResponse<InvoiceRevertStatusResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetForInvoice(Guid invoiceId, CancellationToken cancellationToken)
    {
        var result = await _service.GetForInvoiceAsync(invoiceId, cancellationToken);
        return result.IsSuccess ? Ok(Envelope(result.Value)) : MapFailure(result.ErrorCode!, result.Error!);
    }

    /// <summary>Asks a Super Admin to revert an OP bill — a reason is required.</summary>
    /// <response code="201">The revert request was raised and is waiting for approval.</response>
    /// <response code="400">No reason given, or the bill isn't an OP bill that can be reverted.</response>
    /// <response code="404">No invoice was found for the given id.</response>
    /// <response code="409">A revert request for this bill is already pending.</response>
    [Authorize]
    [RequirePermission("finance-billing.create")]
    [HttpPost("invoices/{invoiceId:guid}/revert-requests")]
    [ProducesResponseType(typeof(ApiResponse<InvoiceRevertRequestResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(Guid invoiceId, [FromBody] CreateInvoiceRevertRequest request, CancellationToken cancellationToken)
    {
        if (request is null) return BadRequest(BuildRequestRequiredError());

        var validation = await _createValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid) return BadRequest(BuildValidationError(validation));

        var result = await _service.CreateAsync(invoiceId, request, User.GetUserId(), ActorName, cancellationToken);
        return result.IsSuccess
            ? StatusCode(StatusCodes.Status201Created, Envelope(result.Value))
            : MapFailure(result.ErrorCode!, result.Error!);
    }

    /// <summary>Lists revert requests, optionally by status — the Super Admin's approval queue.</summary>
    /// <response code="200">A page of revert requests.</response>
    [Authorize]
    [RequirePermission("finance-billing.view")]
    [HttpGet("revert-requests")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<InvoiceRevertRequestResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPaged([FromQuery] InvoiceRevertRequestListQuery query, CancellationToken cancellationToken)
    {
        var paged = await _service.GetPagedAsync(query, cancellationToken);
        var meta = new PaginationMeta { Page = paged.Page, PageSize = paged.PageSize, TotalCount = paged.TotalCount, TotalPages = paged.TotalPages };

        return Ok(new ApiResponse<IReadOnlyList<InvoiceRevertRequestResponse>> { Data = paged.Items, Meta = meta });
    }

    /// <summary>How many revert requests are waiting for a decision — the tab badge.</summary>
    /// <response code="200">The number of Pending requests.</response>
    [Authorize]
    [RequirePermission("finance-billing.view")]
    [HttpGet("revert-requests/pending-count")]
    [ProducesResponseType(typeof(ApiResponse<int>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPendingCount(CancellationToken cancellationToken)
        => Ok(new ApiResponse<int> { Data = await _service.GetPendingCountAsync(cancellationToken) });

    /// <summary>Approves a Pending revert request and reverts its bill — Super Admin only.</summary>
    /// <response code="200">The request was approved and the bill reverted.</response>
    /// <response code="403">The caller isn't signed in as a Super Admin.</response>
    /// <response code="404">No revert request was found for the given id.</response>
    /// <response code="409">The request was already decided, or the bill was already voided.</response>
    [Authorize]
    [RequirePermission("finance-billing.view")]
    [HttpPost("revert-requests/{id:guid}/approve")]
    [ProducesResponseType(typeof(ApiResponse<InvoiceRevertRequestResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Approve(Guid id, [FromBody] ApproveInvoiceRevertRequest? request, CancellationToken cancellationToken)
    {
        if (!IsSuperAdmin) return SuperAdminOnly();

        request ??= new ApproveInvoiceRevertRequest();
        var validation = await _approveValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid) return BadRequest(BuildValidationError(validation));

        var result = await _service.ApproveAsync(id, request, User.GetUserId(), ActorName, cancellationToken);
        return result.IsSuccess ? Ok(Envelope(result.Value)) : MapFailure(result.ErrorCode!, result.Error!);
    }

    /// <summary>Rejects a Pending revert request — Super Admin only; a reason is required.</summary>
    /// <response code="200">The request was rejected; the bill is unchanged.</response>
    /// <response code="400">No reason given.</response>
    /// <response code="403">The caller isn't signed in as a Super Admin.</response>
    /// <response code="404">No revert request was found for the given id.</response>
    /// <response code="409">The request was already decided.</response>
    [Authorize]
    [RequirePermission("finance-billing.view")]
    [HttpPost("revert-requests/{id:guid}/reject")]
    [ProducesResponseType(typeof(ApiResponse<InvoiceRevertRequestResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Reject(Guid id, [FromBody] RejectInvoiceRevertRequest request, CancellationToken cancellationToken)
    {
        if (!IsSuperAdmin) return SuperAdminOnly();
        if (request is null) return BadRequest(BuildRequestRequiredError());

        var validation = await _rejectValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid) return BadRequest(BuildValidationError(validation));

        var result = await _service.RejectAsync(id, request, User.GetUserId(), ActorName, cancellationToken);
        return result.IsSuccess ? Ok(Envelope(result.Value)) : MapFailure(result.ErrorCode!, result.Error!);
    }

    private static ApiResponse<T> Envelope<T>(T? data) => new() { Data = data };

    private IActionResult SuperAdminOnly() => StatusCode(StatusCodes.Status403Forbidden, new ApiErrorResponse
    {
        ErrorCode = BillingErrorCodes.RevertSuperAdminOnly,
        Message = "Only a Super Admin can approve or reject a bill revert request.",
        CorrelationId = HttpContext.GetCorrelationId(),
        Timestamp = DateTime.UtcNow,
    });

    private IActionResult MapFailure(string errorCode, string message)
    {
        var status = errorCode switch
        {
            BillingErrorCodes.NotFound => StatusCodes.Status404NotFound,
            BillingErrorCodes.RevertRequestNotFound => StatusCodes.Status404NotFound,
            BillingErrorCodes.RevertAlreadyPending => StatusCodes.Status409Conflict,
            BillingErrorCodes.RevertRequestNotPending => StatusCodes.Status409Conflict,
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
