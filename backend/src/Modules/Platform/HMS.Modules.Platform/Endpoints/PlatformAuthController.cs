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
    private readonly IValidator<PlatformMfaVerifyRequest> _mfaVerifyValidator;
    private readonly IValidator<PlatformMfaEnableRequest> _mfaEnableValidator;
    private readonly IValidator<PlatformMfaDisableRequest> _mfaDisableValidator;
    private readonly IRevokedTokenStore _revokedTokenStore;
    private readonly ILogger<PlatformAuthController> _logger;

    public PlatformAuthController(
        IPlatformAuthenticationService authenticationService,
        IValidator<PlatformLoginRequest> loginValidator,
        IValidator<PlatformMfaVerifyRequest> mfaVerifyValidator,
        IValidator<PlatformMfaEnableRequest> mfaEnableValidator,
        IValidator<PlatformMfaDisableRequest> mfaDisableValidator,
        IRevokedTokenStore revokedTokenStore,
        ILogger<PlatformAuthController> logger)
    {
        _authenticationService = authenticationService;
        _loginValidator = loginValidator;
        _mfaVerifyValidator = mfaVerifyValidator;
        _mfaEnableValidator = mfaEnableValidator;
        _mfaDisableValidator = mfaDisableValidator;
        _revokedTokenStore = revokedTokenStore;
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

        if (result.Value!.MfaRequired)
        {
            _logger.LogInformation("POST /api/platform/auth/login passed the password step for {Email}; MFA required", request.Email);
        }
        else
        {
            _logger.LogInformation("POST /api/platform/auth/login succeeded for {Email}", request.Email);
        }

        return Ok(new ApiResponse<PlatformLoginResponse> { Data = result.Value });
    }

    /// <summary>Second step of a two-step MFA login — exchanges the challenge token
    /// LoginAsync issued, plus a current authenticator code, for the real bearer token.</summary>
    /// <response code="200">The code was correct; the response contains a bearer token.</response>
    /// <response code="400">The request failed validation, the code was wrong, or the challenge is invalid/expired.</response>
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitingPolicyNames.Login)]
    [HttpPost("mfa/verify")]
    public async Task<IActionResult> VerifyMfa([FromBody] PlatformMfaVerifyRequest request, CancellationToken cancellationToken)
    {
        var validation = await _mfaVerifyValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(BuildValidationError(validation));
        }

        var result = await _authenticationService.VerifyMfaAsync(request, cancellationToken);
        if (!result.IsSuccess)
        {
            return MapFailure(result.ErrorCode!, result.Error!);
        }

        _logger.LogInformation("POST /api/platform/auth/mfa/verify succeeded");

        return Ok(new ApiResponse<PlatformLoginResponse> { Data = result.Value });
    }

    /// <summary>Whether the caller's own account currently has MFA enabled.</summary>
    /// <response code="200">The response contains the current status.</response>
    [Authorize(Policy = "Platform")]
    [HttpGet("mfa/status")]
    public async Task<IActionResult> GetMfaStatus(CancellationToken cancellationToken)
    {
        var result = await _authenticationService.GetMfaStatusAsync(User.GetPlatformUserId()!.Value, cancellationToken);
        return result.IsSuccess ? Ok(new ApiResponse<PlatformMfaStatusResponse> { Data = result.Value }) : MapFailure(result.ErrorCode!, result.Error!);
    }

    /// <summary>Starts (or restarts) MFA setup for the caller's own account — generates a
    /// new secret, shown once for the admin to add to their authenticator app.</summary>
    /// <response code="200">Setup started; the response contains the secret and otpauth URI.</response>
    [Authorize(Policy = "Platform")]
    [HttpPost("mfa/setup")]
    public async Task<IActionResult> SetupMfa(CancellationToken cancellationToken)
    {
        var result = await _authenticationService.SetupMfaAsync(User.GetPlatformUserId()!.Value, cancellationToken);
        return result.IsSuccess ? Ok(new ApiResponse<PlatformMfaSetupResponse> { Data = result.Value }) : MapFailure(result.ErrorCode!, result.Error!);
    }

    /// <summary>Confirms a pending MFA setup by proving the caller's authenticator app
    /// produces a valid code — the only way MFA actually turns on.</summary>
    /// <response code="204">MFA is now enabled.</response>
    /// <response code="400">The request failed validation, or the code was wrong.</response>
    [Authorize(Policy = "Platform")]
    [HttpPost("mfa/enable")]
    public async Task<IActionResult> EnableMfa([FromBody] PlatformMfaEnableRequest request, CancellationToken cancellationToken)
    {
        var validation = await _mfaEnableValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(BuildValidationError(validation));
        }

        var result = await _authenticationService.EnableMfaAsync(User.GetPlatformUserId()!.Value, request.Code, cancellationToken);
        if (!result.IsSuccess)
        {
            return MapFailure(result.ErrorCode!, result.Error!);
        }

        _logger.LogInformation("Platform user {PlatformUserId} enabled MFA via the API", User.GetPlatformUserId());
        return NoContent();
    }

    /// <summary>Turns MFA back off for the caller's own account. Requires a valid current
    /// code, not just an authenticated session — same reasoning as requiring the current
    /// password to change a password.</summary>
    /// <response code="204">MFA is now disabled.</response>
    /// <response code="400">The request failed validation, or the code was wrong.</response>
    [Authorize(Policy = "Platform")]
    [HttpPost("mfa/disable")]
    public async Task<IActionResult> DisableMfa([FromBody] PlatformMfaDisableRequest request, CancellationToken cancellationToken)
    {
        var validation = await _mfaDisableValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(BuildValidationError(validation));
        }

        var result = await _authenticationService.DisableMfaAsync(User.GetPlatformUserId()!.Value, request.Code, cancellationToken);
        if (!result.IsSuccess)
        {
            return MapFailure(result.ErrorCode!, result.Error!);
        }

        _logger.LogInformation("Platform user {PlatformUserId} disabled MFA via the API", User.GetPlatformUserId());
        return NoContent();
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

    /// <summary>
    /// Revokes the caller's own token server-side (HMS Security Hardening: previously a
    /// leaked or "logged out" Platform token stayed valid until natural JWT expiry — see
    /// JwtConfiguration's OnTokenValidated for the other half of this). Idempotent: logging
    /// out twice with the same token succeeds both times.
    /// </summary>
    /// <response code="204">The token was revoked (or already was).</response>
    /// <response code="401">No token, or the token was missing/invalid/expired.</response>
    [Authorize(Policy = "Platform")]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        var jti = User.FindFirst("jti")?.Value;
        var expClaim = User.FindFirst("exp")?.Value;
        if (string.IsNullOrEmpty(jti) || !long.TryParse(expClaim, out var expUnixSeconds))
        {
            // Should be unreachable — every Platform token PlatformJwtTokenGenerator issues
            // carries both claims, and JwtConfiguration already rejects a token missing
            // "jti" before this action ever runs.
            throw new InvalidOperationException("Authenticated Platform token is missing a 'jti' or 'exp' claim.");
        }

        var expiresAt = DateTimeOffset.FromUnixTimeSeconds(expUnixSeconds).UtcDateTime;
        await _revokedTokenStore.RevokeAsync(jti, expiresAt, cancellationToken);

        _logger.LogInformation("Platform user {PlatformUserId} logged out", User.FindFirst("PlatformUserId")?.Value);

        return NoContent();
    }

    private IActionResult MapFailure(string errorCode, string message)
    {
        var status = errorCode switch
        {
            PlatformErrorCodes.InvalidLogin => StatusCodes.Status401Unauthorized,
            PlatformErrorCodes.MfaChallengeInvalid => StatusCodes.Status401Unauthorized,
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
