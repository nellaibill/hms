using HMS.Modules.Identity.Application;
using HMS.Modules.Identity.Contracts;
using HMS.Shared.Kernel;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HMS.Modules.Identity.Endpoints;

/// <summary>
/// Read-only: permissions are a system-seeded catalog (see PermissionSeedData) and
/// cannot be created, updated or deleted through the API.
/// </summary>
[ApiController]
[Route("api/v1/permissions")]
public sealed class PermissionsController : ControllerBase
{
    private readonly IPermissionService _service;

    public PermissionsController(IPermissionService service)
    {
        _service = service;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<PermissionResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        CancellationToken cancellationToken)
    {
        var result = await _service.GetAllAsync(cancellationToken);

        return Ok(Envelope(result));
    }

    private static ApiResponse<T> Envelope<T>(T? data)
        => new()
        {
            Data = data
        };
}
