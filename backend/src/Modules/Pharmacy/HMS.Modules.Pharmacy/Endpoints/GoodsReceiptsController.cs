using FluentValidation;
using HMS.Modules.Pharmacy.Application;
using HMS.Modules.Pharmacy.Contracts;
using HMS.Shared.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HMS.Modules.Pharmacy.Endpoints;

/// <summary>Goods receipts — supplier invoices booked into stock. No PUT/DELETE: a receipt is
/// posted when saved, and is corrected by a purchase return or an adjustment.</summary>
[ApiController]
[RequireFeature("pharmacy")]
[Route("api/v1/pharmacy/goods-receipts")]
public class GoodsReceiptsController : PharmacyControllerBase
{
    private readonly IGoodsReceiptService _service;
    private readonly IValidator<CreateGoodsReceiptRequest> _createValidator;

    public GoodsReceiptsController(IGoodsReceiptService service, IValidator<CreateGoodsReceiptRequest> createValidator)
    {
        _service = service;
        _createValidator = createValidator;
    }

    /// <summary>Books a supplier invoice: creates or updates each line's batch and receives the goods into the store.</summary>
    /// <response code="201">The goods receipt was posted.</response>
    /// <response code="400">The request failed validation.</response>
    /// <response code="409">A store, supplier or product is invalid, or the invoice was already entered.</response>
    [Authorize]
    [RequirePermission("pharmacy.create")]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateGoodsReceiptRequest request, CancellationToken cancellationToken)
    {
        if (await ValidateAsync(_createValidator, request, cancellationToken) is { } invalid) return invalid;
        return CreatedFromResult(await _service.CreateAsync(request, User.GetUserId(), cancellationToken));
    }

    /// <summary>Lists goods receipts, newest first. Purchase figures are cost, so this needs pharmacy.view-cost.</summary>
    [Authorize]
    [RequirePermission("pharmacy.view-cost")]
    [HttpGet]
    public async Task<IActionResult> GetPaged([FromQuery] GoodsReceiptListQuery query, CancellationToken cancellationToken)
        => Paged(await _service.GetPagedAsync(query, cancellationToken));

    /// <summary>Gets one goods receipt with its lines.</summary>
    /// <response code="404">No goods receipt with that id exists.</response>
    [Authorize]
    [RequirePermission("pharmacy.view-cost")]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
        => FromResult(await _service.GetByIdAsync(id, cancellationToken));
}
