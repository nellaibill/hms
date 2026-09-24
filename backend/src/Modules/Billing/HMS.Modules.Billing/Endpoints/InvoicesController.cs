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

        if (request.Items.Any(i => i.DiscountApproved) && !User.HasPermission("finance-billing.discount-approve"))
        {
            return StatusCode(StatusCodes.Status403Forbidden, BuildDiscountApprovalForbiddenError());
        }

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

    /// <summary>The latest bills across every patient, composed with patient/visit context
    /// (age, gender, contact, registration type, consultant(s)) — backs the Patient Billing
    /// page's "Recent Patient Bills" table. Gated by the same "finance-billing.view" permission
    /// as every other read here, so visibility already follows whatever a Doctor/Receptionist/
    /// Super Admin's assigned role grants — no role-specific filtering is hard-coded.</summary>
    [Authorize]
    [RequirePermission("finance-billing.view")]
    [HttpGet("recent")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<RecentPatientBillResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRecent([FromQuery] int count, CancellationToken cancellationToken)
    {
        var result = await _service.GetRecentAsync(count <= 0 ? 10 : count, cancellationToken);
        return result.IsSuccess ? Ok(Envelope(result.Value)) : MapFailure(result.ErrorCode!, result.Error!);
    }

    /// <summary>Billed revenue per month for the last N months (default 6, max 24) and this
    /// month's revenue by billing type — backs the Executive Dashboard's finance charts (DASH-01).</summary>
    [Authorize]
    [RequirePermission("finance-billing.view")]
    [HttpGet("dashboard-summary")]
    [ProducesResponseType(typeof(ApiResponse<BillingDashboardSummaryResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDashboardSummary([FromQuery] int months = 6, CancellationToken cancellationToken = default)
    {
        var summary = await _service.GetDashboardSummaryAsync(months, cancellationToken);
        return Ok(new ApiResponse<BillingDashboardSummaryResponse> { Data = summary });
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

    /// <summary>Lists BillingType.Procedure invoice line items, paged/filtered — backs the
    /// OPD Procedures List tab. A separate, literal route (not a query-param mode switch on
    /// GetPaged) since this operates at the line-item level, not the whole-invoice level
    /// GetPaged/GetById/GetRecent all share.</summary>
    [Authorize]
    [RequirePermission("finance-billing.view")]
    [HttpGet("procedures")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<ProcedureListItem>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetProcedures([FromQuery] ProcedureListQuery query, CancellationToken cancellationToken)
    {
        // A consultant only ever sees their own billed procedures here — see
        // ClaimsPrincipalExtensions.GetScopedConsultantId's own doc comment. Overrides
        // whatever ConsultantId the client asked for (this also backs the OPD Procedures
        // List tab).
        var scopedConsultantId = User.GetScopedConsultantId();
        if (scopedConsultantId is not null)
        {
            query = query with { ConsultantId = scopedConsultantId.Value.ToString() };
        }

        var paged = await _service.GetProcedureLineItemsAsync(query, cancellationToken);
        var meta = new PaginationMeta { Page = paged.Page, PageSize = paged.PageSize, TotalCount = paged.TotalCount, TotalPages = paged.TotalPages };

        return Ok(new ApiResponse<IReadOnlyList<ProcedureListItem>> { Data = paged.Items, Meta = meta });
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
            BillingErrorCodes.InvoiceVoided => StatusCodes.Status409Conflict,
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

    private ApiErrorResponse BuildDiscountApprovalForbiddenError() => new()
    {
        ErrorCode = "BILLING.DISCOUNT_APPROVAL_FORBIDDEN",
        Message = "You don't have permission to approve a discount. Ask an Accounts Officer, Hospital Administrator, or Super Admin to create this invoice.",
        CorrelationId = HttpContext.GetCorrelationId(),
        Timestamp = DateTime.UtcNow,
    };
}
