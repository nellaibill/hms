using HMS.Modules.IPD.Application;
using HMS.Modules.IPD.Contracts;
using HMS.Shared.Infrastructure;
using HMS.Shared.Kernel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HMS.Modules.IPD.Endpoints;

/// <summary>
/// Generates a real HMS.Modules.Billing Invoice from a discharged admission's AdmissionCharge
/// ledger. Gated by "finance-billing.create" (Billing's own permission), not
/// "clinical-care.*" — this creates a real financial document, so it sits behind the same
/// permission as InvoicesController.Create rather than IPD's clinical permissions. See
/// Application/IPDBillingService.cs and ADR-066.
/// </summary>
[ApiController]
[RequireFeature("ipd")]
[Route("api/v1/ipd/admissions/{admissionId:guid}/final-bill")]
public class IPDBillingController : ControllerBase
{
    private readonly IIPDBillingService _service;

    public IPDBillingController(IIPDBillingService service)
    {
        _service = service;
    }

    [Authorize]
    [RequirePermission("finance-billing.create")]
    [HttpPost]
    public async Task<IActionResult> GenerateFinalBill(Guid admissionId, CancellationToken cancellationToken)
    {
        var result = await _service.GenerateFinalBillAsync(admissionId, actorId: User.GetUserId(), cancellationToken);
        return !result.IsSuccess
            ? MapFailure(result.ErrorCode!, result.Error!)
            : StatusCode(StatusCodes.Status201Created, new ApiResponse<GenerateFinalBillResponse> { Data = result.Value });
    }

    private IActionResult MapFailure(string errorCode, string message)
    {
        var status = errorCode switch
        {
            IPDErrorCodes.NotFound => StatusCodes.Status404NotFound,
            IPDErrorCodes.AdmissionNotDischarged => StatusCodes.Status409Conflict,
            IPDErrorCodes.FinalBillAlreadyGenerated => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status400BadRequest,
        };

        var error = new ApiErrorResponse { ErrorCode = errorCode, Message = message, CorrelationId = HttpContext.GetCorrelationId(), Timestamp = DateTime.UtcNow };
        return StatusCode(status, error);
    }
}
