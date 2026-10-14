using HMS.Modules.Pharmacy.Application;
using HMS.Shared.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HMS.Modules.Pharmacy.Endpoints;

/// <summary>
/// Pharmacy reports and the dashboard — all read-only, all summed from the documents. Open to
/// pharmacy staff (pharmacy.view) and to accounts (finance-billing.view): pharmacy bills are
/// not in the Billing module (ADR-094), so this is where finance reads pharmacy revenue.
/// Cost, margin, stock value and anything about purchases need pharmacy.view-cost or
/// finance-billing.view.
/// </summary>
[ApiController]
[Authorize]
[RequireFeature("pharmacy")]
[Route("api/v1/pharmacy/reports")]
public class ReportsController : PharmacyControllerBase
{
    private const int MaxPeriodDays = 366;
    private const int MaxRegisterDays = 93;

    private readonly IPharmacyReportService _service;

    public ReportsController(IPharmacyReportService service)
    {
        _service = service;
    }

    private bool CanView => User.HasPermission("pharmacy.view") || User.HasPermission("finance-billing.view");

    private bool CanSeeCost => User.HasPermission("pharmacy.view-cost") || User.HasPermission("finance-billing.view");

    /// <summary>Today, the month so far, the last fortnight, stock alerts and pending work.</summary>
    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard([FromQuery] Guid? storeId, CancellationToken cancellationToken)
    {
        if (!CanView) return Forbid();
        return Data(await _service.GetDashboardAsync(storeId, CanSeeCost, cancellationToken));
    }

    /// <summary>Sales, returns, collections and margin for each day of a period.</summary>
    [HttpGet("daily-sales")]
    public async Task<IActionResult> GetDailySales([FromQuery] DateOnly fromDate, [FromQuery] DateOnly toDate, [FromQuery] Guid? storeId, CancellationToken cancellationToken)
    {
        if (!CanView) return Forbid();
        if (CheckPeriod(fromDate, toDate, MaxPeriodDays) is { } invalid) return invalid;
        return Data(await _service.GetDailySalesAsync(fromDate, toDate, storeId, CanSeeCost, cancellationToken));
    }

    /// <summary>Every bill and every sale return in a period of up to three months.</summary>
    [HttpGet("sales-register")]
    public async Task<IActionResult> GetSalesRegister([FromQuery] DateOnly fromDate, [FromQuery] DateOnly toDate, [FromQuery] Guid? storeId, CancellationToken cancellationToken)
    {
        if (!CanView) return Forbid();
        if (CheckPeriod(fromDate, toDate, MaxRegisterDays) is { } invalid) return invalid;
        return Data(await _service.GetSalesRegisterAsync(fromDate, toDate, storeId, cancellationToken));
    }

    /// <summary>What each product sold, net of returns — fast movers and margin by item.</summary>
    [HttpGet("product-sales")]
    public async Task<IActionResult> GetProductSales([FromQuery] DateOnly fromDate, [FromQuery] DateOnly toDate, [FromQuery] Guid? storeId, CancellationToken cancellationToken)
    {
        if (!CanView) return Forbid();
        if (CheckPeriod(fromDate, toDate, MaxPeriodDays) is { } invalid) return invalid;
        return Data(await _service.GetProductSalesAsync(fromDate, toDate, storeId, CanSeeCost, cancellationToken));
    }

    /// <summary>Money taken and refunded at the counter, by day and payment mode.</summary>
    [HttpGet("collections")]
    public async Task<IActionResult> GetCollections([FromQuery] DateOnly fromDate, [FromQuery] DateOnly toDate, [FromQuery] Guid? storeId, CancellationToken cancellationToken)
    {
        if (!CanView) return Forbid();
        if (CheckPeriod(fromDate, toDate, MaxPeriodDays) is { } invalid) return invalid;
        return Data(await _service.GetCollectionsAsync(fromDate, toDate, storeId, cancellationToken));
    }

    /// <summary>GST by HSN code and rate: on sales, and (for those who may see purchases) on purchases.</summary>
    [HttpGet("gst")]
    public async Task<IActionResult> GetGstSummary([FromQuery] DateOnly fromDate, [FromQuery] DateOnly toDate, [FromQuery] Guid? storeId, CancellationToken cancellationToken)
    {
        if (!CanView) return Forbid();
        if (CheckPeriod(fromDate, toDate, MaxPeriodDays) is { } invalid) return invalid;
        return Data(await _service.GetGstSummaryAsync(fromDate, toDate, storeId, CanSeeCost, cancellationToken));
    }

    /// <summary>Every supplier invoice received and every purchase return in a period.</summary>
    [HttpGet("purchase-register")]
    public async Task<IActionResult> GetPurchaseRegister([FromQuery] DateOnly fromDate, [FromQuery] DateOnly toDate, [FromQuery] Guid? storeId, CancellationToken cancellationToken)
    {
        if (!CanView || !CanSeeCost) return Forbid();
        if (CheckPeriod(fromDate, toDate, MaxPeriodDays) is { } invalid) return invalid;
        return Data(await _service.GetPurchaseRegisterAsync(fromDate, toDate, storeId, cancellationToken));
    }

    /// <summary>What the stock on hand is worth, product by product.</summary>
    [HttpGet("stock-valuation")]
    public async Task<IActionResult> GetStockValuation([FromQuery] Guid? storeId, CancellationToken cancellationToken)
    {
        if (!CanView) return Forbid();
        return Data(await _service.GetStockValuationAsync(storeId, CanSeeCost, cancellationToken));
    }

    /// <summary>Stock that has not sold for <paramref name="days"/> days (90 by default).</summary>
    [HttpGet("non-moving")]
    public async Task<IActionResult> GetNonMovingStock([FromQuery] Guid? storeId, [FromQuery] int? days, CancellationToken cancellationToken)
    {
        if (!CanView) return Forbid();
        return Data(await _service.GetNonMovingStockAsync(storeId, days ?? 90, CanSeeCost, cancellationToken));
    }

    private IActionResult? CheckPeriod(DateOnly fromDate, DateOnly toDate, int maxDays)
    {
        if (fromDate == default || toDate == default || fromDate > toDate)
        {
            return Failure(PharmacyErrorCodes.InvalidState, "Choose a from date and a to date, in that order.");
        }

        return toDate.DayNumber - fromDate.DayNumber >= maxDays
            ? Failure(PharmacyErrorCodes.InvalidState, maxDays == MaxRegisterDays ? "Choose a period of up to three months." : "Choose a period of up to a year.")
            : null;
    }
}
