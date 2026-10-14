using FluentValidation;
using HMS.Modules.Pharmacy.Application;
using HMS.Modules.Pharmacy.Contracts;
using HMS.Shared.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HMS.Modules.Pharmacy.Endpoints;

/// <summary>Purchase orders and the reorder list that feeds them.</summary>
[ApiController]
[RequireFeature("pharmacy")]
[Route("api/v1/pharmacy/purchase-orders")]
public class PurchaseOrdersController : PharmacyControllerBase
{
    private readonly IPurchaseOrderService _service;
    private readonly IValidator<SavePurchaseOrderRequest> _validator;

    public PurchaseOrdersController(IPurchaseOrderService service, IValidator<SavePurchaseOrderRequest> validator)
    {
        _service = service;
        _validator = validator;
    }

    /// <summary>Products at or below their reorder level, with a suggested quantity and where each was last bought.</summary>
    [Authorize]
    [RequirePermission("pharmacy.view")]
    [HttpGet("suggestions")]
    public async Task<IActionResult> GetSuggestions([FromQuery] Guid? storeId, CancellationToken cancellationToken)
        => Data(await _service.GetReorderSuggestionsAsync(storeId, User.HasPermission("pharmacy.view-cost"), cancellationToken));

    /// <summary>Creates a purchase order, as a draft or placed straight away.</summary>
    [Authorize]
    [RequirePermission("pharmacy.create")]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] SavePurchaseOrderRequest request, CancellationToken cancellationToken)
    {
        if (await ValidateAsync(_validator, request, cancellationToken) is { } invalid) return invalid;
        return CreatedFromResult(await _service.CreateAsync(request, User.GetUserId(), cancellationToken));
    }

    /// <summary>Replaces a draft order's supplier, dates and lines.</summary>
    [Authorize]
    [RequirePermission("pharmacy.edit")]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] SavePurchaseOrderRequest request, CancellationToken cancellationToken)
    {
        if (await ValidateAsync(_validator, request, cancellationToken) is { } invalid) return invalid;
        return FromResult(await _service.UpdateAsync(id, request, User.GetUserId(), cancellationToken));
    }

    /// <summary>Marks a draft order as sent to the supplier.</summary>
    [Authorize]
    [RequirePermission("pharmacy.edit")]
    [HttpPost("{id:guid}/place")]
    public async Task<IActionResult> Place(Guid id, CancellationToken cancellationToken)
        => FromResult(await _service.PlaceAsync(id, User.GetUserId(), cancellationToken));

    /// <summary>Cancels what is still outstanding on an order.</summary>
    [Authorize]
    [RequirePermission("pharmacy.edit")]
    [HttpPost("{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken cancellationToken)
        => FromResult(await _service.CancelAsync(id, User.GetUserId(), cancellationToken));

    /// <summary>Lists purchase orders, newest first.</summary>
    [Authorize]
    [RequirePermission("pharmacy.view")]
    [HttpGet]
    public async Task<IActionResult> GetPaged([FromQuery] PurchaseOrderListQuery query, CancellationToken cancellationToken)
        => Paged(await _service.GetPagedAsync(query, cancellationToken));

    /// <summary>Gets one purchase order with its lines.</summary>
    [Authorize]
    [RequirePermission("pharmacy.view")]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
        => FromResult(await _service.GetByIdAsync(id, cancellationToken));
}

/// <summary>Stock sent back to suppliers. Posted on save; no PUT or DELETE.</summary>
[ApiController]
[RequireFeature("pharmacy")]
[Route("api/v1/pharmacy/purchase-returns")]
public class PurchaseReturnsController : PharmacyControllerBase
{
    private readonly IPurchaseReturnService _service;
    private readonly IValidator<CreatePurchaseReturnRequest> _validator;

    public PurchaseReturnsController(IPurchaseReturnService service, IValidator<CreatePurchaseReturnRequest> validator)
    {
        _service = service;
        _validator = validator;
    }

    /// <summary>The batches of a product the store holds and could send back, expired ones included.</summary>
    [Authorize]
    [RequirePermission("pharmacy.view")]
    [HttpGet("returnable-batches/{productId:guid}")]
    public async Task<IActionResult> GetReturnableBatches(Guid productId, [FromQuery] Guid? storeId, CancellationToken cancellationToken)
        => Data(await _service.GetReturnableBatchesAsync(productId, storeId, User.HasPermission("pharmacy.view-cost"), cancellationToken));

    /// <summary>Records a return to a supplier: the stock leaves the store and the supplier's account is credited.</summary>
    [Authorize]
    [RequirePermission("pharmacy.create")]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreatePurchaseReturnRequest request, CancellationToken cancellationToken)
    {
        if (await ValidateAsync(_validator, request, cancellationToken) is { } invalid) return invalid;
        return CreatedFromResult(await _service.CreateAsync(request, User.GetUserId(), cancellationToken));
    }

    /// <summary>Lists purchase returns, newest first. Shows cost, so it needs pharmacy.view-cost.</summary>
    [Authorize]
    [RequirePermission("pharmacy.view-cost")]
    [HttpGet]
    public async Task<IActionResult> GetPaged([FromQuery] PurchaseReturnListQuery query, CancellationToken cancellationToken)
        => Paged(await _service.GetPagedAsync(query, cancellationToken));

    /// <summary>Gets one purchase return with its lines.</summary>
    [Authorize]
    [RequirePermission("pharmacy.view-cost")]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
        => FromResult(await _service.GetByIdAsync(id, cancellationToken));
}

/// <summary>What the pharmacy owes each supplier, their statements, and payments made to them.</summary>
[ApiController]
[RequireFeature("pharmacy")]
[Route("api/v1/pharmacy/supplier-accounts")]
public class SupplierAccountsController : PharmacyControllerBase
{
    private readonly ISupplierAccountService _service;
    private readonly IValidator<CreateSupplierPaymentRequest> _validator;

    public SupplierAccountsController(ISupplierAccountService service, IValidator<CreateSupplierPaymentRequest> validator)
    {
        _service = service;
        _validator = validator;
    }

    /// <summary>Every supplier the pharmacy has dealt with: bought, returned, paid, outstanding.</summary>
    [Authorize]
    [RequirePermission("pharmacy.view-cost")]
    [HttpGet]
    public async Task<IActionResult> GetBalances(CancellationToken cancellationToken)
        => Data(await _service.GetBalancesAsync(cancellationToken));

    /// <summary>One supplier's statement with running balance, optionally for a date range.</summary>
    [Authorize]
    [RequirePermission("pharmacy.view-cost")]
    [HttpGet("{supplierId:guid}/statement")]
    public async Task<IActionResult> GetStatement(Guid supplierId, [FromQuery] DateOnly? fromDate, [FromQuery] DateOnly? toDate, CancellationToken cancellationToken)
        => FromResult(await _service.GetStatementAsync(supplierId, fromDate, toDate, cancellationToken));

    /// <summary>Records a payment made to a supplier.</summary>
    [Authorize]
    [RequirePermission("pharmacy.create")]
    [HttpPost("{supplierId:guid}/payments")]
    public async Task<IActionResult> RecordPayment(Guid supplierId, [FromBody] CreateSupplierPaymentRequest request, CancellationToken cancellationToken)
    {
        if (await ValidateAsync(_validator, request, cancellationToken) is { } invalid) return invalid;
        return CreatedFromResult(await _service.RecordPaymentAsync(supplierId, request, User.GetUserId(), cancellationToken));
    }
}
