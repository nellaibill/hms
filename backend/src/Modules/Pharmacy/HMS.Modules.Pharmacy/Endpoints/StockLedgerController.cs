using HMS.Modules.Pharmacy.Application;
using HMS.Modules.Pharmacy.Contracts;
using HMS.Shared.Infrastructure;
using HMS.Shared.Kernel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HMS.Modules.Pharmacy.Endpoints;

/// <summary>Read-only combined stock ledger (receipts + dispenses), filterable across every
/// PharmacyStockTransaction field.</summary>
[ApiController]
[RequireFeature("pharmacy")]
[Route("api/v1/pharmacy/stock-ledger")]
public class StockLedgerController : ControllerBase
{
    private readonly IStockLedgerService _service;

    public StockLedgerController(IStockLedgerService service)
    {
        _service = service;
    }

    /// <summary>Lists ledger entries, newest first, filterable by product, batch, transaction
    /// type, patient, and/or a transaction-date range.</summary>
    /// <response code="200">The page of ledger entries.</response>
    [Authorize]
    [RequirePermission("pharmacy.view")]
    [HttpGet]
    public async Task<IActionResult> GetPaged([FromQuery] StockLedgerListQuery query, CancellationToken cancellationToken)
    {
        var paged = await _service.GetPagedAsync(query, cancellationToken);
        var meta = new PaginationMeta { Page = paged.Page, PageSize = paged.PageSize, TotalCount = paged.TotalCount, TotalPages = paged.TotalPages };
        return Ok(new ApiResponse<IReadOnlyList<StockTransactionResponse>> { Data = paged.Items, Meta = meta });
    }
}
