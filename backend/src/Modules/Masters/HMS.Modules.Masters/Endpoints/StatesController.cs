using HMS.Modules.Masters.Application;
using HMS.Modules.Masters.Contracts;
using HMS.Shared.Infrastructure;
using HMS.Shared.Kernel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HMS.Modules.Masters.Endpoints;

/// <summary>
/// Read-only India state/district reference data for Patient Registration's Address section
/// (see docs/DecisionLog.md). India is the only country supported, so there is no Country
/// endpoint — states are the top level. No admin CRUD in this iteration: both lists come
/// from the seeded data in StateConfiguration/DistrictConfiguration.
/// </summary>
[ApiController]
[Route("api/v1/masters/states")]
public class StatesController : ControllerBase
{
    private readonly IStateService _stateService;
    private readonly IDistrictService _districtService;

    public StatesController(IStateService stateService, IDistrictService districtService)
    {
        _stateService = stateService;
        _districtService = districtService;
    }

    /// <summary>Lists every state/union territory.</summary>
    /// <response code="200">The full state list.</response>
    [Authorize]
    [RequirePermission("identity-administration.view")]
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var states = await _stateService.GetAllAsync(cancellationToken);
        return Ok(new ApiResponse<IReadOnlyList<StateResponse>> { Data = states });
    }

    /// <summary>Lists every district in the given state. An unknown state id yields an empty list.</summary>
    /// <response code="200">The state's districts (possibly empty).</response>
    [Authorize]
    [RequirePermission("identity-administration.view")]
    [HttpGet("{stateId:guid}/districts")]
    public async Task<IActionResult> GetDistricts(Guid stateId, CancellationToken cancellationToken)
    {
        var districts = await _districtService.GetByStateIdAsync(stateId, cancellationToken);
        return Ok(new ApiResponse<IReadOnlyList<DistrictResponse>> { Data = districts });
    }
}
