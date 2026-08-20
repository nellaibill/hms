using FluentValidation;
using FluentValidation.Results;
using HMS.Modules.IPD.Application;
using HMS.Modules.IPD.Contracts;
using HMS.Shared.Infrastructure;
using HMS.Shared.Kernel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HMS.Modules.IPD.Endpoints;

[ApiController]
[RequireFeature("ipd")]
[Route("api/v1/ipd/admissions")]
public class AdmissionsController : ControllerBase
{
    private readonly IAdmissionService _service;
    private readonly IValidator<CreateAdmissionRequest> _createValidator;
    private readonly IValidator<UpdateAdmissionRequest> _updateValidator;
    private readonly IValidator<TransferBedRequest> _transferValidator;
    private readonly IValidator<DischargeAdmissionRequest> _dischargeValidator;

    public AdmissionsController(
        IAdmissionService service,
        IValidator<CreateAdmissionRequest> createValidator,
        IValidator<UpdateAdmissionRequest> updateValidator,
        IValidator<TransferBedRequest> transferValidator,
        IValidator<DischargeAdmissionRequest> dischargeValidator)
    {
        _service = service;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _transferValidator = transferValidator;
        _dischargeValidator = dischargeValidator;
    }

    [Authorize]
    [RequirePermission("clinical-care.create")]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateAdmissionRequest request, CancellationToken cancellationToken)
    {
        if (request is null) return BadRequest(BuildRequestRequiredError());

        var validation = await _createValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid) return BadRequest(BuildValidationError(validation));

        var result = await _service.CreateAsync(request, actorId: User.GetUserId(), cancellationToken);
        return !result.IsSuccess
            ? MapFailure(result.ErrorCode!, result.Error!)
            : CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, Envelope(result.Value));
    }

    [Authorize]
    [RequirePermission("clinical-care.view")]
    [HttpGet]
    public async Task<IActionResult> GetPaged([FromQuery] AdmissionListQuery query, CancellationToken cancellationToken)
    {
        var paged = await _service.GetPagedAsync(query, cancellationToken);
        var meta = new PaginationMeta { Page = paged.Page, PageSize = paged.PageSize, TotalCount = paged.TotalCount, TotalPages = paged.TotalPages };
        return Ok(new ApiResponse<IReadOnlyList<AdmissionResponse>> { Data = paged.Items, Meta = meta });
    }

    [Authorize]
    [RequirePermission("clinical-care.view")]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _service.GetByIdAsync(id, cancellationToken);
        return result.IsSuccess ? Ok(Envelope(result.Value)) : MapFailure(result.ErrorCode!, result.Error!);
    }

    [Authorize]
    [RequirePermission("clinical-care.edit")]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateAdmissionRequest request, CancellationToken cancellationToken)
    {
        if (request is null) return BadRequest(BuildRequestRequiredError());

        var validation = await _updateValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid) return BadRequest(BuildValidationError(validation));

        var result = await _service.UpdateAsync(id, request, actorId: User.GetUserId(), cancellationToken);
        return result.IsSuccess ? Ok(Envelope(result.Value)) : MapFailure(result.ErrorCode!, result.Error!);
    }

    [Authorize]
    [RequirePermission("clinical-care.edit")]
    [HttpPost("{id:guid}/transfer-bed")]
    public async Task<IActionResult> TransferBed(Guid id, [FromBody] TransferBedRequest request, CancellationToken cancellationToken)
    {
        if (request is null) return BadRequest(BuildRequestRequiredError());

        var validation = await _transferValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid) return BadRequest(BuildValidationError(validation));

        var result = await _service.TransferBedAsync(id, request, actorId: User.GetUserId(), cancellationToken);
        return result.IsSuccess ? Ok(Envelope(result.Value)) : MapFailure(result.ErrorCode!, result.Error!);
    }

    [Authorize]
    [RequirePermission("clinical-care.view")]
    [HttpGet("{id:guid}/transfer-history")]
    public async Task<IActionResult> GetTransferHistory(Guid id, CancellationToken cancellationToken)
    {
        var result = await _service.GetTransferHistoryAsync(id, cancellationToken);
        return result.IsSuccess
            ? Ok(new ApiResponse<IReadOnlyList<BedTransferHistoryResponse>> { Data = result.Value })
            : MapFailure(result.ErrorCode!, result.Error!);
    }

    [Authorize]
    [RequirePermission("clinical-care.view")]
    [HttpGet("{id:guid}/bed-history")]
    public async Task<IActionResult> GetBedStayHistory(Guid id, CancellationToken cancellationToken)
    {
        var result = await _service.GetBedStayHistoryAsync(id, cancellationToken);
        return result.IsSuccess
            ? Ok(new ApiResponse<IReadOnlyList<AdmissionBedStayResponse>> { Data = result.Value })
            : MapFailure(result.ErrorCode!, result.Error!);
    }

    [Authorize]
    [RequirePermission("clinical-care.edit")]
    [HttpPost("{id:guid}/discharge")]
    public async Task<IActionResult> Discharge(Guid id, [FromBody] DischargeAdmissionRequest request, CancellationToken cancellationToken)
    {
        if (request is null) return BadRequest(BuildRequestRequiredError());

        var validation = await _dischargeValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid) return BadRequest(BuildValidationError(validation));

        var result = await _service.DischargeAsync(id, request, actorId: User.GetUserId(), cancellationToken);
        return result.IsSuccess ? Ok(Envelope(result.Value)) : MapFailure(result.ErrorCode!, result.Error!);
    }

    private static ApiResponse<AdmissionResponse> Envelope(AdmissionResponse? data) => new() { Data = data };

    private IActionResult MapFailure(string errorCode, string message)
    {
        var status = errorCode switch
        {
            IPDErrorCodes.NotFound => StatusCodes.Status404NotFound,
            IPDErrorCodes.BedNotAvailable => StatusCodes.Status409Conflict,
            IPDErrorCodes.PatientAlreadyAdmitted => StatusCodes.Status409Conflict,
            IPDErrorCodes.AdmissionAlreadyDischarged => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status400BadRequest,
        };

        var error = new ApiErrorResponse { ErrorCode = errorCode, Message = message, CorrelationId = HttpContext.GetCorrelationId(), Timestamp = DateTime.UtcNow };
        return StatusCode(status, error);
    }

    private ApiErrorResponse BuildValidationError(ValidationResult validation) => new()
    {
        ErrorCode = "VALIDATION.FAILED",
        Message = "One or more validation errors occurred.",
        ValidationErrors = validation.Errors.Select(e => new ValidationErrorItem { Field = e.PropertyName, Message = e.ErrorMessage }).ToList(),
        CorrelationId = HttpContext.GetCorrelationId(),
        Timestamp = DateTime.UtcNow,
    };

    private ApiErrorResponse BuildRequestRequiredError() => new()
    {
        ErrorCode = "VALIDATION.FAILED",
        Message = "The request body is missing or could not be parsed.",
        CorrelationId = HttpContext.GetCorrelationId(),
        Timestamp = DateTime.UtcNow,
    };
}
