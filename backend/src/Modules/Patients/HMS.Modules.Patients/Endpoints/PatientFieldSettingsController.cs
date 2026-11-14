using FluentValidation;
using HMS.Modules.Patients.Application;
using HMS.Modules.Patients.Contracts;
using HMS.Shared.Infrastructure;
using HMS.Shared.Kernel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HMS.Modules.Patients.Endpoints;

/// <summary>
/// Settings → Patient Registration Fields (ADR-135): which registration fields this hospital
/// makes mandatory. Reading is open to any signed-in user, because the New Patient
/// Registration and Edit Patient forms need it; saving needs identity-administration.edit,
/// the same grain as the other hospital-wide settings on the Settings page.
/// </summary>
[ApiController]
[Authorize]
[Route("api/v1/patients/field-settings")]
public class PatientFieldSettingsController : ControllerBase
{
    private readonly IPatientFieldSettingsService _service;
    private readonly IValidator<PatientFieldRequirements> _validator;

    public PatientFieldSettingsController(IPatientFieldSettingsService service, IValidator<PatientFieldRequirements> validator)
    {
        _service = service;
        _validator = validator;
    }

    /// <summary>The saved choices, or the defaults (today's behaviour) if none were saved.</summary>
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
        => Ok(new ApiResponse<PatientFieldRequirements> { Data = await _service.GetAsync(cancellationToken) });

    /// <summary>Saves the choices. They apply to the next registration or edit.</summary>
    /// <response code="200">The settings were saved.</response>
    /// <response code="400">The request failed validation.</response>
    [RequirePermission("identity-administration.edit")]
    [HttpPut]
    public async Task<IActionResult> Update([FromBody] PatientFieldRequirements request, CancellationToken cancellationToken)
    {
        var validation = await _validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(new ApiErrorResponse
            {
                ErrorCode = "VALIDATION.FAILED",
                Message = "One or more validation errors occurred.",
                ValidationErrors = validation.Errors
                    .Select(e => new ValidationErrorItem { Field = e.PropertyName, Message = e.ErrorMessage })
                    .ToList(),
                CorrelationId = HttpContext.GetCorrelationId(),
                Timestamp = DateTime.UtcNow,
            });
        }

        var result = await _service.UpdateAsync(request, User.GetUserId(), cancellationToken);
        return Ok(new ApiResponse<PatientFieldRequirements> { Data = result.Value });
    }
}
