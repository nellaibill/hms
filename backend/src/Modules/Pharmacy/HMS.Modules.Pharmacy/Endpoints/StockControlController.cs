using FluentValidation;
using HMS.Modules.Pharmacy.Application;
using HMS.Modules.Pharmacy.Contracts;
using HMS.Modules.Products.Contracts;
using HMS.Shared.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HMS.Modules.Pharmacy.Endpoints;

/// <summary>Stock adjustments, transfers between stores, physical counts, the expiry list and
/// the statutory registers. Adjustments, transfers and counts post stock; they need pharmacy.edit.</summary>
[ApiController]
[RequireFeature("pharmacy")]
[Route("api/v1/pharmacy/stock-control")]
public class StockControlController : PharmacyControllerBase
{
    private readonly IStockControlService _service;
    private readonly IValidator<CreateStockAdjustmentRequest> _adjustmentValidator;
    private readonly IValidator<CreateStockTransferRequest> _transferValidator;

    public StockControlController(
        IStockControlService service,
        IValidator<CreateStockAdjustmentRequest> adjustmentValidator,
        IValidator<CreateStockTransferRequest> transferValidator)
    {
        _service = service;
        _adjustmentValidator = adjustmentValidator;
        _transferValidator = transferValidator;
    }

    private bool CanSeeCost => User.HasPermission("pharmacy.view-cost");

    // ---- Adjustments

    /// <summary>Corrects stock, with a reason. Positive quantities add; negative take away.</summary>
    [Authorize]
    [RequirePermission("pharmacy.edit")]
    [HttpPost("adjustments")]
    public async Task<IActionResult> CreateAdjustment([FromBody] CreateStockAdjustmentRequest request, CancellationToken cancellationToken)
    {
        if (await ValidateAsync(_adjustmentValidator, request, cancellationToken) is { } invalid) return invalid;
        return CreatedFromResult(await _service.CreateAdjustmentAsync(request, User.GetUserId(), CanSeeCost, cancellationToken));
    }

    [Authorize]
    [RequirePermission("pharmacy.view")]
    [HttpGet("adjustments")]
    public async Task<IActionResult> GetAdjustments([FromQuery] StockAdjustmentListQuery query, CancellationToken cancellationToken)
        => Paged(await _service.GetAdjustmentsPagedAsync(query, CanSeeCost, cancellationToken));

    [Authorize]
    [RequirePermission("pharmacy.view")]
    [HttpGet("adjustments/{id:guid}")]
    public async Task<IActionResult> GetAdjustment(Guid id, CancellationToken cancellationToken)
        => FromResult(await _service.GetAdjustmentAsync(id, CanSeeCost, cancellationToken));

    // ---- Transfers

    /// <summary>Moves stock from one store to another, in one step.</summary>
    [Authorize]
    [RequirePermission("pharmacy.edit")]
    [HttpPost("transfers")]
    public async Task<IActionResult> CreateTransfer([FromBody] CreateStockTransferRequest request, CancellationToken cancellationToken)
    {
        if (await ValidateAsync(_transferValidator, request, cancellationToken) is { } invalid) return invalid;
        return CreatedFromResult(await _service.CreateTransferAsync(request, User.GetUserId(), cancellationToken));
    }

    [Authorize]
    [RequirePermission("pharmacy.view")]
    [HttpGet("transfers")]
    public async Task<IActionResult> GetTransfers([FromQuery] StockTransferListQuery query, CancellationToken cancellationToken)
        => Paged(await _service.GetTransfersPagedAsync(query, cancellationToken));

    [Authorize]
    [RequirePermission("pharmacy.view")]
    [HttpGet("transfers/{id:guid}")]
    public async Task<IActionResult> GetTransfer(Guid id, CancellationToken cancellationToken)
        => FromResult(await _service.GetTransferAsync(id, cancellationToken));

    // ---- Stock counts

    /// <summary>Starts a physical count of a store: lists every batch the books say it holds.</summary>
    [Authorize]
    [RequirePermission("pharmacy.edit")]
    [HttpPost("counts")]
    public async Task<IActionResult> StartCount([FromBody] StartStockCountRequest? request, CancellationToken cancellationToken)
        => CreatedFromResult(await _service.StartCountAsync(request ?? new StartStockCountRequest(), User.GetUserId(), cancellationToken));

    /// <summary>Saves counted quantities. Can be called as often as needed while the count is open.</summary>
    [Authorize]
    [RequirePermission("pharmacy.edit")]
    [HttpPut("counts/{id:guid}")]
    public async Task<IActionResult> SaveCount(Guid id, [FromBody] SaveStockCountRequest request, CancellationToken cancellationToken)
        => FromResult(await _service.SaveCountAsync(id, request ?? new SaveStockCountRequest(), User.GetUserId(), cancellationToken));

    /// <summary>Posts the count: every difference becomes one stock adjustment.</summary>
    [Authorize]
    [RequirePermission("pharmacy.edit")]
    [HttpPost("counts/{id:guid}/post")]
    public async Task<IActionResult> PostCount(Guid id, CancellationToken cancellationToken)
        => FromResult(await _service.PostCountAsync(id, User.GetUserId(), cancellationToken));

    [Authorize]
    [RequirePermission("pharmacy.edit")]
    [HttpPost("counts/{id:guid}/cancel")]
    public async Task<IActionResult> CancelCount(Guid id, CancellationToken cancellationToken)
        => FromResult(await _service.CancelCountAsync(id, User.GetUserId(), cancellationToken));

    [Authorize]
    [RequirePermission("pharmacy.view")]
    [HttpGet("counts")]
    public async Task<IActionResult> GetCounts([FromQuery] StockCountListQuery query, CancellationToken cancellationToken)
        => Paged(await _service.GetCountsPagedAsync(query, cancellationToken));

    [Authorize]
    [RequirePermission("pharmacy.view")]
    [HttpGet("counts/{id:guid}")]
    public async Task<IActionResult> GetCount(Guid id, CancellationToken cancellationToken)
        => FromResult(await _service.GetCountAsync(id, cancellationToken));

    // ---- Expiry and registers

    /// <summary>Batches on the shelf that have expired or will within the window (the pharmacy's alert window by default).</summary>
    [Authorize]
    [RequirePermission("pharmacy.view")]
    [HttpGet("expiring")]
    public async Task<IActionResult> GetExpiring([FromQuery] Guid? storeId, [FromQuery] int? days, CancellationToken cancellationToken)
        => Data(await _service.GetExpiringAsync(storeId, days, CanSeeCost, cancellationToken));

    /// <summary>Every sale of a scheduled drug in a period — the Schedule H1 and narcotics registers. Up to a year at a time.</summary>
    [Authorize]
    [RequirePermission("pharmacy.view")]
    [HttpGet("schedule-register")]
    public async Task<IActionResult> GetScheduleRegister(
        [FromQuery] DateOnly fromDate, [FromQuery] DateOnly toDate, [FromQuery] DrugSchedule[]? schedule, [FromQuery] Guid? storeId, CancellationToken cancellationToken)
    {
        if (fromDate > toDate || toDate.DayNumber - fromDate.DayNumber > 366)
        {
            return Failure(PharmacyErrorCodes.InvalidState, "Choose a date range of up to a year.");
        }

        var schedules = schedule is { Length: > 0 } ? schedule : [DrugSchedule.H1, DrugSchedule.X, DrugSchedule.Narcotic];
        return Data(await _service.GetScheduleRegisterAsync(fromDate, toDate, schedules, storeId, cancellationToken));
    }
}
