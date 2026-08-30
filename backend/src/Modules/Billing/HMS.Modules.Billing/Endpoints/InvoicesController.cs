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
/// Invoice CRUD plus payment recording. Every action requires "finance-billing.*" — unlike
/// Masters/Roles, where GETs stay at the baseline Hospital policy, an invoice is patient
/// financial data (docs/DecisionLog.md's authorization-gap ADRs), so even reads are gated —
/// matching RolesController's now-current end-to-end pattern rather than its original
/// GETs-are-baseline-only one.
/// </summary>
[ApiController]
[Route("api/v1/billing/invoices")]
public class InvoicesController : ControllerBase
{
    private readonly IInvoiceService _service;
    private readonly IValidator<CreateInvoiceRequest> _createValidator;
    private readonly IValidator<VoidInvoiceRequest> _voidValidator;

    public InvoicesController(IInvoiceService service, IValidator<CreateInvoiceRequest> createValidator, IValidator<VoidInvoiceRequest> voidValidator)
    {
        _service = service;
        _createValidator = createValidator;
        _voidValidator = voidValidator;
    }

    /// <summary>Creates a new invoice with its line items in one call.</summary>
    [Authorize]
    [RequirePermission("finance-billing.create")]
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<InvoiceResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateInvoiceRequest request, CancellationToken cancellationToken)
    {
        if (request is null) return BadRequest(BuildRequestRequiredError());

        var validation = await _createValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid) return BadRequest(BuildValidationError(validation));

        var result = await _service.CreateAsync(request, actorId: User.GetUserId(), cancellationToken);
        return !result.IsSuccess
            ? MapFailure(result.ErrorCode!, result.Error!)
            : CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, Envelope(result.Value));
    }

    /// <summary>Lists invoices with paging, search, sorting, and payment-status filtering — the Unified Invoice Ledger.</summary>
    [Authorize]
    [RequirePermission("finance-billing.view")]
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<InvoiceResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPaged([FromQuery] InvoiceListQuery query, CancellationToken cancellationToken)
    {
        var paged = await _service.GetPagedAsync(query, cancellationToken);
        var meta = new PaginationMeta { Page = paged.Page, PageSize = paged.PageSize, TotalCount = paged.TotalCount, TotalPages = paged.TotalPages };

        return Ok(new ApiResponse<IReadOnlyList<InvoiceResponse>> { Data = paged.Items, Meta = meta });
    }

    /// <summary>Gets a single invoice by id, with its line items.</summary>
    [Authorize]
    [RequirePermission("finance-billing.view")]
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<InvoiceResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _service.GetByIdAsync(id, cancellationToken);
        return result.IsSuccess ? Ok(Envelope(result.Value)) : MapFailure(result.ErrorCode!, result.Error!);
    }

    /// <summary>Lists every invoice for one patient, newest first — used by the patient detail page's Billing tab.</summary>
    [Authorize]
    [RequirePermission("finance-billing.view")]
    [HttpGet("by-patient/{patientId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<InvoiceResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByPatientId(Guid patientId, CancellationToken cancellationToken)
    {
        var result = await _service.GetByPatientIdAsync(patientId, cancellationToken);
        return result.IsSuccess ? Ok(Envelope(result.Value)) : MapFailure(result.ErrorCode!, result.Error!);
    }

    /// <summary>Records a payment against one line item, marking it Paid.</summary>
    [Authorize]
    [RequirePermission("finance-billing.edit")]
    [HttpPost("{id:guid}/items/{itemId:guid}/payments")]
    [ProducesResponseType(typeof(ApiResponse<InvoiceResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RecordPayment(Guid id, Guid itemId, [FromBody] RecordPaymentRequest request, CancellationToken cancellationToken)
    {
        if (request is null) return BadRequest(BuildRequestRequiredError());

        var result = await _service.RecordPaymentAsync(id, itemId, request, actorId: User.GetUserId(), cancellationToken);
        return result.IsSuccess ? Ok(Envelope(result.Value)) : MapFailure(result.ErrorCode!, result.Error!);
    }

    /// <summary>Cancels an invoice that hasn't had any payment recorded against it — see
    /// Domain/Invoice.cs's Void for why a paid invoice is rejected instead. Requires the
    /// "delete" permission (already seeded, previously unused by Billing) rather than
    /// "edit" — voiding is a more sensitive action than recording a payment.</summary>
    [Authorize]
    [RequirePermission("finance-billing.delete")]
    [HttpPost("{id:guid}/void")]
    [ProducesResponseType(typeof(ApiResponse<InvoiceResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Void(Guid id, [FromBody] VoidInvoiceRequest request, CancellationToken cancellationToken)
    {
        if (request is null) return BadRequest(BuildRequestRequiredError());

        var validation = await _voidValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid) return BadRequest(BuildValidationError(validation));

        var result = await _service.VoidAsync(id, request, actorId: User.GetUserId(), cancellationToken);
        return result.IsSuccess ? Ok(Envelope(result.Value)) : MapFailure(result.ErrorCode!, result.Error!);
    }

    private static ApiResponse<T> Envelope<T>(T? data) => new() { Data = data };

    private IActionResult MapFailure(string errorCode, string message)
    {
        var status = errorCode switch
        {
            BillingErrorCodes.NotFound => StatusCodes.Status404NotFound,
            BillingErrorCodes.LineItemNotFound => StatusCodes.Status404NotFound,
            BillingErrorCodes.LineItemAlreadyPaid => StatusCodes.Status409Conflict,
            BillingErrorCodes.AlreadyVoided => StatusCodes.Status409Conflict,
            BillingErrorCodes.HasPayments => StatusCodes.Status409Conflict,
            BillingErrorCodes.EmptyInvoice => StatusCodes.Status400BadRequest,
            BillingErrorCodes.InvalidPatient => StatusCodes.Status400BadRequest,
            BillingErrorCodes.PaymentAmountMismatch => StatusCodes.Status400BadRequest,
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
