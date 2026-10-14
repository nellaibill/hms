using FluentValidation;
using HMS.Modules.Pharmacy.Application;
using HMS.Modules.Pharmacy.Contracts;
using HMS.Shared.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HMS.Modules.Pharmacy.Endpoints;

/// <summary>What doctors prescribed in OPD, as the pharmacy counter sees it.</summary>
[ApiController]
[RequireFeature("pharmacy")]
[Route("api/v1/pharmacy/prescriptions")]
public class PrescriptionsController : PharmacyControllerBase
{
    private readonly IPrescriptionQueueService _service;

    public PrescriptionsController(IPrescriptionQueueService service)
    {
        _service = service;
    }

    /// <summary>Completed consultations with medicines prescribed, newest first, with any bills already made. Defaults to today.</summary>
    [Authorize]
    [RequirePermission("pharmacy.view")]
    [HttpGet]
    public async Task<IActionResult> GetQueue([FromQuery] DateOnly? fromDate, [FromQuery] DateOnly? toDate, CancellationToken cancellationToken)
    {
        var to = toDate ?? PharmacyClock.Today();
        var from = fromDate ?? to;
        if (from > to || to.DayNumber - from.DayNumber > 31)
        {
            return Failure(PharmacyErrorCodes.InvalidState, "Choose a date range of up to 31 days.");
        }

        return Data(await _service.GetQueueAsync(from, to, cancellationToken));
    }
}

/// <summary>Ward requests for inpatients' medicines.</summary>
[ApiController]
[RequireFeature("pharmacy")]
[Route("api/v1/pharmacy/indents")]
public class IndentsController : PharmacyControllerBase
{
    private readonly IIndentService _service;
    private readonly IValidator<CreateIndentRequest> _validator;

    public IndentsController(IIndentService service, IValidator<CreateIndentRequest> validator)
    {
        _service = service;
        _validator = validator;
    }

    /// <summary>Raises an indent for an admitted patient. Open to ward staff as well as the pharmacy.</summary>
    [Authorize]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateIndentRequest request, CancellationToken cancellationToken)
    {
        // A ward nurse raises indents without having any pharmacy rights; the pharmacy can
        // raise one on a ward's behalf. Either permission will do.
        if (!User.HasPermission("pharmacy.create") && !User.HasPermission("clinical-care.create")) return Forbid();
        if (await ValidateAsync(_validator, request, cancellationToken) is { } invalid) return invalid;
        return CreatedFromResult(await _service.CreateAsync(request, User.GetUserId(), cancellationToken));
    }

    /// <summary>Lists indents: open ones first, urgent before routine, oldest first.</summary>
    [Authorize]
    [HttpGet]
    public async Task<IActionResult> GetPaged([FromQuery] IndentListQuery query, CancellationToken cancellationToken)
    {
        if (!User.HasPermission("pharmacy.view") && !User.HasPermission("clinical-care.view")) return Forbid();
        return Paged(await _service.GetPagedAsync(query, cancellationToken));
    }

    /// <summary>Gets one indent with its lines.</summary>
    [Authorize]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        if (!User.HasPermission("pharmacy.view") && !User.HasPermission("clinical-care.view")) return Forbid();
        return FromResult(await _service.GetByIdAsync(id, cancellationToken));
    }

    /// <summary>Closes what is left of an indent.</summary>
    [Authorize]
    [HttpPost("{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken cancellationToken)
    {
        if (!User.HasPermission("pharmacy.create") && !User.HasPermission("clinical-care.create")) return Forbid();
        return FromResult(await _service.CancelAsync(id, User.GetUserId(), cancellationToken));
    }
}

/// <summary>The pharmacy's catalog as clinical staff may see it: what exists and what is in
/// stock, with selling prices but nothing about cost. It is what a doctor's prescription box
/// and a ward's indent screen search — neither of which has pharmacy rights.</summary>
[ApiController]
[RequireFeature("pharmacy")]
[Route("api/v1/pharmacy/catalog")]
public class CatalogController : PharmacyControllerBase
{
    private readonly ICounterService _service;

    public CatalogController(ICounterService service)
    {
        _service = service;
    }

    /// <summary>Finds items by name, generic or code, each with what the pharmacy has of it.</summary>
    [Authorize]
    [HttpGet]
    public async Task<IActionResult> Search([FromQuery] string? search, [FromQuery] Guid? storeId, CancellationToken cancellationToken, [FromQuery] int limit = 15)
    {
        if (!User.HasPermission("pharmacy.view") && !User.HasPermission("clinical-care.view")) return Forbid();
        return Data(await _service.SearchItemsAsync(search, storeId, limit, cancellationToken));
    }
}
