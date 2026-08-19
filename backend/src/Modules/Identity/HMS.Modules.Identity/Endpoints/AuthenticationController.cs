using FluentValidation;
using FluentValidation.Results;
using HMS.Modules.Identity.Application;
using HMS.Modules.Identity.Contracts;
using HMS.Shared.Infrastructure;
using HMS.Shared.Kernel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Logging;

namespace HMS.Modules.Identity.Endpoints;

/// <summary>
/// The Authentication module's HTTP surface — login and a protected diagnostic endpoint
/// (GET /me) that proves the JWT bearer pipeline actually validates issued tokens.
/// </summary>
[ApiController]
[Route("api/v1/auth")]
public class AuthenticationController : ControllerBase
{
    private readonly IAuthenticationService _authenticationService;
    private readonly IValidator<LoginRequest> _loginValidator;
    private readonly ILogger<AuthenticationController> _logger;

    public AuthenticationController(
        IAuthenticationService authenticationService,
        IValidator<LoginRequest> loginValidator,
        ILogger<AuthenticationController> logger)
    {
        _authenticationService = authenticationService;
        _loginValidator = loginValidator;
        _logger = logger;
    }

    /// <summary>Authenticates a user and issues a JWT.</summary>
    /// <response code="200">Login succeeded; the response contains a bearer token.</response>
    /// <response code="400">The request failed validation.</response>
    /// <response code="401">The login type, username, or password was invalid.</response>
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitingPolicyNames.Login)]
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
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

        _logger.LogInformation("POST /api/v1/auth/login succeeded for {Username}", request.Username);

        return Ok(new ApiResponse<LoginResponse> { Data = result.Value });
    }

    /// <summary>Returns the caller's own identity, as read from the validated JWT — proof
    /// the bearer token pipeline is actually enforced end to end.</summary>
    /// <response code="200">The token was valid.</response>
    /// <response code="401">No token, or the token was missing/invalid/expired.</response>
    [Authorize]
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
            AuthenticationErrorCodes.InvalidLogin => StatusCodes.Status401Unauthorized,
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
