using HMS.Modules.Pharmacy.Application;
using HMS.Modules.Pharmacy.Contracts;
using HMS.Shared.Infrastructure;
using HMS.Shared.Kernel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HMS.Modules.Pharmacy.Endpoints;

/// <summary>Read-only current on-hand stock balances per product/batch.</summary>
[ApiController]
[RequireFeature("pharmacy")]
[Route("api/v1/pharmacy/stock-balances")]
public class StockBalancesController : ControllerBase
{
    private readonly IStockBalanceService _service;

    public StockBalancesController(IStockBalanceService service)
    {
        _service = service;
    }

    /// <summary>Lists current stock balances, optionally filtered by product.</summary>
    /// <response code="200">The page of stock balances.</response>
    [Authorize]
    [RequirePermission("pharmacy.view")]
    [HttpGet]
    public async Task<IActionResult> GetPaged([FromQuery] StockBalanceListQuery query, CancellationToken cancellationToken)
    {
        var paged = await _service.GetPagedAsync(query, cancellationToken);
        var meta = new PaginationMeta { Page = paged.Page, PageSize = paged.PageSize, TotalCount = paged.TotalCount, TotalPages = paged.TotalPages };
        return Ok(new ApiResponse<IReadOnlyList<StockBalanceResponse>> { Data = paged.Items, Meta = meta });
    }

    /// <summary>Lists stock per product — every batch rolled up, split into available /
    /// expiring soon / expired / on hold — name-ordered, searchable by name, generic name, SKU
    /// or code, and optionally narrowed to the products needing attention.</summary>
    /// <response code="200">The page of product stock summaries.</response>
    [Authorize]
    [RequirePermission("pharmacy.view")]
    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary([FromQuery] StockSummaryListQuery query, CancellationToken cancellationToken)
    {
        var paged = await _service.GetSummaryPagedAsync(query, cancellationToken);
        var meta = new PaginationMeta { Page = paged.Page, PageSize = paged.PageSize, TotalCount = paged.TotalCount, TotalPages = paged.TotalPages };
        return Ok(new ApiResponse<IReadOnlyList<StockSummaryResponse>> { Data = paged.Items, Meta = meta });
    }

    /// <summary>Lists the batches of a product that can be dispensed right now from one store
    /// (active, unexpired, with stock on hand), earliest expiry first, each with its unit
    /// price. The default store when <paramref name="storeId"/> is omitted.</summary>
    /// <response code="200">The dispensable batches — empty when the product has none.</response>
    [Authorize]
    [RequirePermission("pharmacy.view")]
    [HttpGet("{productId:guid}/available-batches")]
    public async Task<IActionResult> GetAvailableBatches(Guid productId, [FromQuery] Guid? storeId, CancellationToken cancellationToken)
    {
        var batches = await _service.GetAvailableBatchesAsync(productId, storeId, cancellationToken);
        return Ok(new ApiResponse<IReadOnlyList<AvailableBatchResponse>> { Data = batches });
    }

    /// <summary>Gets the current stock balance for a single product/batch pair in one store
    /// (the default store when <paramref name="storeId"/> is omitted).</summary>
    /// <response code="200">The stock balance.</response>
    /// <response code="404">No stock balance exists for that product/batch.</response>
    [Authorize]
    [RequirePermission("pharmacy.view")]
    [HttpGet("{productId:guid}/{productBatchId:guid}")]
    public async Task<IActionResult> Get(Guid productId, Guid productBatchId, [FromQuery] Guid? storeId, CancellationToken cancellationToken)
    {
        var result = await _service.GetAsync(productId, productBatchId, storeId, cancellationToken);
        return result.IsSuccess ? Ok(Envelope(result.Value)) : MapFailure(result.ErrorCode!, result.Error!);
    }

    private static ApiResponse<StockBalanceResponse> Envelope(StockBalanceResponse? data) => new() { Data = data };

    private IActionResult MapFailure(string errorCode, string message)
    {
        var status = errorCode switch
        {
            PharmacyErrorCodes.NotFound => StatusCodes.Status404NotFound,
            _ => StatusCodes.Status400BadRequest,
        };

        var error = new ApiErrorResponse { ErrorCode = errorCode, Message = message, CorrelationId = HttpContext.GetCorrelationId(), Timestamp = DateTime.UtcNow };
        return StatusCode(status, error);
    }
}
