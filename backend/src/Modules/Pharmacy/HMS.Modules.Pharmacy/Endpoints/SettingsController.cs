using FluentValidation;
using HMS.Modules.Pharmacy.Application;
using HMS.Modules.Pharmacy.Contracts;
using HMS.Shared.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HMS.Modules.Pharmacy.Endpoints;

/// <summary>The pharmacy's bill particulars (GSTIN, drug licence, footer) and counter rules.</summary>
[ApiController]
[RequireFeature("pharmacy")]
[Route("api/v1/pharmacy/settings")]
public class SettingsController : PharmacyControllerBase
{
    private readonly ISettingsService _service;
    private readonly IValidator<UpdatePharmacySettingsRequest> _validator;

    public SettingsController(ISettingsService service, IValidator<UpdatePharmacySettingsRequest> validator)
    {
        _service = service;
        _validator = validator;
    }

    /// <summary>The current settings, or the defaults if none have been saved.</summary>
    [Authorize]
    [RequirePermission("pharmacy.view")]
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
        => Data(await _service.GetAsync(cancellationToken));

    /// <summary>Saves the settings.</summary>
    [Authorize]
    [RequirePermission("pharmacy.edit")]
    [HttpPut]
    public async Task<IActionResult> Update([FromBody] UpdatePharmacySettingsRequest request, CancellationToken cancellationToken)
    {
        if (await ValidateAsync(_validator, request, cancellationToken) is { } invalid) return invalid;
        return FromResult(await _service.UpdateAsync(request, User.GetUserId(), cancellationToken));
    }
}
