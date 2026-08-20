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

    /// <summary>Gets the current stock balance for a single product/batch pair.</summary>
    /// <response code="200">The stock balance.</response>
    /// <response code="404">No stock balance exists for that product/batch.</response>
    [Authorize]
    [RequirePermission("pharmacy.view")]
    [HttpGet("{productId:guid}/{productBatchId:guid}")]
    public async Task<IActionResult> Get(Guid productId, Guid productBatchId, CancellationToken cancellationToken)
    {
        var result = await _service.GetAsync(productId, productBatchId, cancellationToken);
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
