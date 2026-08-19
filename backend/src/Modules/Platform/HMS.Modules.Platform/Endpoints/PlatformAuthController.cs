using FluentValidation;
using FluentValidation.Results;
using HMS.Modules.Platform.Application;
using HMS.Modules.Platform.Application.Abstractions;
using HMS.Modules.Platform.Contracts;
using HMS.Shared.Infrastructure;
using HMS.Shared.Kernel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Logging;

namespace HMS.Modules.Platform.Endpoints;

/// <summary>
/// The Platform module's authentication HTTP surface — entirely separate from
/// HMS.Modules.Identity.Endpoints.AuthenticationController. Platform Admins and hospital
/// users never share a login endpoint, a session, or a database.
/// </summary>
[ApiController]
[Route("api/platform/auth")]
public class PlatformAuthController : ControllerBase
{
    private readonly IPlatformAuthenticationService _authenticationService;
    private readonly IValidator<PlatformLoginRequest> _loginValidator;
    private readonly ILogger<PlatformAuthController> _logger;

    public PlatformAuthController(
        IPlatformAuthenticationService authenticationService,
        IValidator<PlatformLoginRequest> loginValidator,
        ILogger<PlatformAuthController> logger)
    {
        _authenticationService = authenticationService;
        _loginValidator = loginValidator;
        _logger = logger;
    }

    /// <summary>Authenticates a Platform Admin and issues a JWT.</summary>
    /// <response code="200">Login succeeded; the response contains a bearer token.</response>
    /// <response code="400">The request failed validation.</response>
    /// <response code="401">The email or password was invalid.</response>
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitingPolicyNames.Login)]
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] PlatformLoginRequest request, CancellationToken cancellationToken)
    {
        var validation = await _loginValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(BuildValidationError(validation));
        }

        var result = await _authenticationService.LoginAsync(request, cancellationToken);
        if (!result.IsSuccess)
        {
            return MapFailure(result.ErrorCode!, result.Error!);
        }

        _logger.LogInformation("POST /api/platform/auth/login succeeded for {Email}", request.Email);

        return Ok(new ApiResponse<PlatformLoginResponse> { Data = result.Value });
    }

    /// <summary>Returns the caller's own identity, as read from the validated JWT.</summary>
    /// <response code="200">The token was valid.</response>
    /// <response code="401">No token, or the token was missing/invalid/expired.</response>
    /// <response code="403">The token was valid but was not issued for the Platform Portal.</response>
    [Authorize(Policy = "Platform")]
    [HttpGet("me")]
    public IActionResult Me()
    {
        var claims = User.Claims.ToDictionary(c => c.Type, c => c.Value);
        return Ok(new ApiResponse<Dictionary<string, string>> { Data = claims });
    }

    private IActionResult MapFailure(string errorCode, string message)
    {
        var status = errorCode switch
        {
            PlatformErrorCodes.InvalidLogin => StatusCodes.Status401Unauthorized,
            _ => StatusCodes.Status400BadRequest,
        };

        var error = new ApiErrorResponse
        {
            ErrorCode = errorCode,
            Message = message,
            CorrelationId = HttpContext.GetCorrelationId(),
            Timestamp = DateTime.UtcNow,
        };

        return StatusCode(status, error);
    }

    private ApiErrorResponse BuildValidationError(ValidationResult validation) => new()
    {
        ErrorCode = "VALIDATION.FAILED",
        Message = "One or more validation errors occurred.",
        ValidationErrors = validation.Errors
            .Select(e => new ValidationErrorItem { Field = e.PropertyName, Message = e.ErrorMessage })
            .ToList(),
        CorrelationId = HttpContext.GetCorrelationId(),
        Timestamp = DateTime.UtcNow,
    };
}
