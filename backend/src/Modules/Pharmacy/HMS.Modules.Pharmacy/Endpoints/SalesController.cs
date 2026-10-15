using FluentValidation;
using HMS.Modules.Pharmacy.Application;
using HMS.Modules.Pharmacy.Contracts;
using HMS.Shared.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HMS.Modules.Pharmacy.Endpoints;

/// <summary>Pharmacy bills, the money taken against them, and goods returned on them. A bill
/// is posted when saved: there is no PUT or DELETE, a mistake is put right with a return.</summary>
[ApiController]
[RequireFeature("pharmacy")]
[Route("api/v1/pharmacy/sales")]
public class SalesController : PharmacyControllerBase
{
    private readonly ISaleService _service;
    private readonly IValidator<CreateSaleRequest> _createValidator;
    private readonly IValidator<CollectSalePaymentRequest> _paymentValidator;
    private readonly IValidator<CreateSaleReturnRequest> _returnValidator;

    public SalesController(
        ISaleService service,
        IValidator<CreateSaleRequest> createValidator,
        IValidator<CollectSalePaymentRequest> paymentValidator,
        IValidator<CreateSaleReturnRequest> returnValidator)
    {
        _service = service;
        _createValidator = createValidator;
        _paymentValidator = paymentValidator;
        _returnValidator = returnValidator;
    }

    /// <summary>Makes a bill: takes the stock (earliest expiry first unless a batch is named), prices and taxes each line, and records the payment.</summary>
    /// <response code="201">The bill, with its number, lines and any change to hand back.</response>
    /// <response code="400">The request failed validation.</response>
    /// <response code="409">Not enough stock, payment short, or details a scheduled drug requires are missing.</response>
    [Authorize]
    [RequirePermission("pharmacy.create")]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateSaleRequest request, CancellationToken cancellationToken)
    {
        if (await ValidateAsync(_createValidator, request, cancellationToken) is { } invalid) return invalid;
        return CreatedFromResult(await _service.CreateAsync(request, User.GetUserId(), cancellationToken));
    }

    /// <summary>Lists bills, newest first — by bill number, customer, phone or UHID.</summary>
    [Authorize]
    [RequirePermission("pharmacy.view")]
    [HttpGet]
    public async Task<IActionResult> GetPaged([FromQuery] SaleListQuery query, CancellationToken cancellationToken)
        => Paged(await _service.GetPagedAsync(query, cancellationToken));

    /// <summary>A patient's pharmacy bills with their lines, and the returns against them — the
    /// patient record's Billing and Timeline read this. Open to pharmacy and billing staff.</summary>
    [Authorize]
    [HttpGet("patient/{patientId:guid}")]
    public async Task<IActionResult> GetForPatient(Guid patientId, CancellationToken cancellationToken)
    {
        if (!User.HasPermission("pharmacy.view") && !User.HasPermission("finance-billing.view")) return Forbid();
        return Data(await _service.GetPatientBillsAsync(patientId, cancellationToken));
    }

    /// <summary>Gets one bill with its lines and payments.</summary>
    [Authorize]
    [RequirePermission("pharmacy.view")]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
        => FromResult(await _service.GetByIdAsync(id, cancellationToken));

    /// <summary>Takes a payment against a bill that has a balance due.</summary>
    [Authorize]
    [RequirePermission("pharmacy.create")]
    [HttpPost("{id:guid}/payments")]
    public async Task<IActionResult> CollectPayment(Guid id, [FromBody] CollectSalePaymentRequest request, CancellationToken cancellationToken)
    {
        if (await ValidateAsync(_paymentValidator, request, cancellationToken) is { } invalid) return invalid;
        return FromResult(await _service.CollectPaymentAsync(id, request, User.GetUserId(), cancellationToken));
    }

    /// <summary>Records goods returned on a bill: credits the customer and, unless told otherwise, puts the goods back in stock.</summary>
    [Authorize]
    [RequirePermission("pharmacy.create")]
    [HttpPost("{id:guid}/returns")]
    public async Task<IActionResult> CreateReturn(Guid id, [FromBody] CreateSaleReturnRequest request, CancellationToken cancellationToken)
    {
        if (await ValidateAsync(_returnValidator, request, cancellationToken) is { } invalid) return invalid;
        return CreatedFromResult(await _service.CreateReturnAsync(id, request, User.GetUserId(), cancellationToken));
    }

    /// <summary>Lists sale returns, newest first.</summary>
    [Authorize]
    [RequirePermission("pharmacy.view")]
    [HttpGet("returns")]
    public async Task<IActionResult> GetReturns([FromQuery] SaleReturnListQuery query, CancellationToken cancellationToken)
        => Paged(await _service.GetReturnsPagedAsync(query, cancellationToken));

    /// <summary>Gets one sale return with its lines.</summary>
    [Authorize]
    [RequirePermission("pharmacy.view")]
    [HttpGet("returns/{id:guid}")]
    public async Task<IActionResult> GetReturnById(Guid id, CancellationToken cancellationToken)
        => FromResult(await _service.GetReturnByIdAsync(id, cancellationToken));
}

/// <summary>What the billing counter screen needs besides the bill: item search, substitutes, and held bills.</summary>
[ApiController]
[RequireFeature("pharmacy")]
[Route("api/v1/pharmacy/counter")]
public class CounterController : PharmacyControllerBase
{
    private readonly ICounterService _service;
    private readonly IValidator<HoldBillRequest> _holdValidator;

    public CounterController(ICounterService service, IValidator<HoldBillRequest> holdValidator)
    {
        _service = service;
        _holdValidator = holdValidator;
    }

    /// <summary>Finds items by name, generic, code or scanned barcode, each with its sellable batches and prices in the store.</summary>
    [Authorize]
    [RequirePermission("pharmacy.view")]
    [HttpGet("items")]
    public async Task<IActionResult> SearchItems([FromQuery] string? search, [FromQuery] Guid? storeId, CancellationToken cancellationToken, [FromQuery] int limit = 20)
        => Data(await _service.SearchItemsAsync(search, storeId, limit, cancellationToken));

    /// <summary>The named products with their sellable stock as it is now — for re-checking a recalled bill or a prescription.</summary>
    [Authorize]
    [RequirePermission("pharmacy.view")]
    [HttpPost("items/lookup")]
    public async Task<IActionResult> LookupItems([FromBody] IReadOnlyList<Guid> productIds, [FromQuery] Guid? storeId, CancellationToken cancellationToken)
        => Data(await _service.GetItemsAsync(productIds ?? [], storeId, cancellationToken));

    /// <summary>In-stock products with the same generic as the given one.</summary>
    [Authorize]
    [RequirePermission("pharmacy.view")]
    [HttpGet("items/{productId:guid}/substitutes")]
    public async Task<IActionResult> GetSubstitutes(Guid productId, [FromQuery] Guid? storeId, CancellationToken cancellationToken)
        => Data(await _service.GetSubstitutesAsync(productId, storeId, cancellationToken));

    /// <summary>Parks the counter's current bill.</summary>
    [Authorize]
    [RequirePermission("pharmacy.create")]
    [HttpPost("held-bills")]
    public async Task<IActionResult> Hold([FromBody] HoldBillRequest request, CancellationToken cancellationToken)
    {
        if (await ValidateAsync(_holdValidator, request, cancellationToken) is { } invalid) return invalid;
        return CreatedFromResult(await _service.HoldAsync(request, User.GetUserId(), cancellationToken));
    }

    /// <summary>The store's parked bills, newest first.</summary>
    [Authorize]
    [RequirePermission("pharmacy.view")]
    [HttpGet("held-bills")]
    public async Task<IActionResult> GetHeld([FromQuery] Guid? storeId, CancellationToken cancellationToken)
        => Data(await _service.GetHeldAsync(storeId, cancellationToken));

    /// <summary>Throws a parked bill away.</summary>
    [Authorize]
    [RequirePermission("pharmacy.create")]
    [HttpDelete("held-bills/{id:guid}")]
    public async Task<IActionResult> DiscardHeld(Guid id, CancellationToken cancellationToken)
        => FromResult(await _service.DiscardHeldAsync(id, cancellationToken));
}
