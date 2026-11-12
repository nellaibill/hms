using HMS.Modules.Patients.Contracts;
using HMS.Modules.Sync.Application;
using HMS.Modules.Sync.Contracts;
using HMS.Shared.Infrastructure;
using HMS.Shared.Kernel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HMS.Modules.Sync.Endpoints;

/// <summary>
/// The cloud's sync receiver (ADR-133 §8). Accepts only the <c>HmsSync</c> API-key scheme:
/// a hospital user token is not enough, and an Edge key can call nothing but these two actions.
/// Answers 404 on any server whose Sync:Role is not Cloud.
/// </summary>
[ApiController]
[Route("api/v1/sync")]
[Authorize(AuthenticationSchemes = SyncAuthenticationDefaults.Scheme, Policy = SyncAuthenticationDefaults.Policy)]
public class SyncReceiverController : ControllerBase
{
    private const long MaxBatchBytes = 20 * 1024 * 1024;

    private readonly ISyncReceiverService _receiver;

    public SyncReceiverController(ISyncReceiverService receiver)
    {
        _receiver = receiver;
    }

    /// <summary>Applies an ordered batch of patient events from a local server. Idempotent by
    /// EventId; per patient, stops at the first event that does not succeed.</summary>
    /// <response code="200">One result per event, in request order.</response>
    /// <response code="400">The batch itself is malformed (no events, or too many).</response>
    /// <response code="401">Missing, unknown or revoked sync key.</response>
    /// <response code="404">This server is not a sync Cloud server.</response>
    [HttpPost("patients")]
    [RequestSizeLimit(MaxBatchBytes)]
    public async Task<IActionResult> ReceivePatients([FromBody] SyncBatchRequest request, CancellationToken cancellationToken)
    {
        if (!_receiver.AcceptsSyncTraffic)
        {
            return NotFoundError();
        }

        if (request.Events.Count == 0 || request.Events.Count > SyncReceiverService.MaxEventsPerBatch)
        {
            return BadRequest(Error("SYNC.INVALID_BATCH", $"A batch must contain between 1 and {SyncReceiverService.MaxEventsPerBatch} events."));
        }

        var response = await _receiver.ProcessAsync(request, cancellationToken);
        return Ok(new ApiResponse<SyncBatchResponse> { Data = response });
    }

    /// <summary>Connectivity and key check used by the local server's probe.</summary>
    /// <response code="200">The key is valid for this hospital.</response>
    /// <response code="401">Missing, unknown or revoked sync key.</response>
    [HttpGet("ping")]
    public IActionResult Ping()
    {
        if (!_receiver.AcceptsSyncTraffic)
        {
            return NotFoundError();
        }

        return Ok(new ApiResponse<SyncPingResponse>
        {
            Data = new SyncPingResponse
            {
                HospitalCode = User.FindFirst(SyncAuthenticationDefaults.HospitalCodeClaim)?.Value ?? string.Empty,
                ServerTime = DateTime.UtcNow,
                SupportedPayloadVersions = PatientSyncAggregate.SupportedPayloadSchemaVersions,
            },
        });
    }

    private NotFoundObjectResult NotFoundError() =>
        NotFound(Error("SYNC.NOT_CLOUD_SERVER", "This server does not accept sync traffic."));

    private ApiErrorResponse Error(string code, string message) => new()
    {
        ErrorCode = code,
        Message = message,
        CorrelationId = HttpContext.GetCorrelationId(),
        Timestamp = DateTime.UtcNow,
    };
}
