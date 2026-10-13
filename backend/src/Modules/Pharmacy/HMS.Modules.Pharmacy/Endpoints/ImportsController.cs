using FluentValidation;
using HMS.Modules.Pharmacy.Application;
using HMS.Modules.Pharmacy.Contracts;
using HMS.Shared.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HMS.Modules.Pharmacy.Endpoints;

/// <summary>Go-live data load: products and the stock already on the shelf, from a spreadsheet.</summary>
[ApiController]
[RequireFeature("pharmacy")]
[Route("api/v1/pharmacy/imports")]
public class ImportsController : PharmacyControllerBase
{
    private readonly IProductImportService _service;
    private readonly IValidator<ImportProductsRequest> _validator;

    public ImportsController(IProductImportService service, IValidator<ImportProductsRequest> validator)
    {
        _service = service;
        _validator = validator;
    }

    /// <summary>Imports a batch of spreadsheet rows — or, with ValidateOnly, reports what importing them would do.</summary>
    /// <response code="200">Row-by-row results. A row that failed is reported, not thrown: the rest still go in.</response>
    /// <response code="400">The request failed validation.</response>
    [Authorize]
    [RequirePermission("pharmacy.edit")]
    [HttpPost("products")]
    public async Task<IActionResult> ImportProducts([FromBody] ImportProductsRequest request, CancellationToken cancellationToken)
    {
        if (await ValidateAsync(_validator, request, cancellationToken) is { } invalid) return invalid;
        return FromResult(await _service.ImportAsync(request, User.GetUserId(), cancellationToken));
    }
}
