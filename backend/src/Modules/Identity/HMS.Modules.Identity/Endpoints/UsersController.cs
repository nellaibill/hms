using FluentValidation;
using FluentValidation.Results;
using HMS.Modules.Identity.Application;
using HMS.Modules.Identity.Contracts;
using HMS.Shared.Infrastructure;
using HMS.Shared.Kernel;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace HMS.Modules.Identity.Endpoints;

/// <summary>
/// The Users module's HTTP surface — CRUD, soft delete, paged/search/sort listing, and
/// activate/deactivate, per docs/modules/Identity/Users.md and docs/ApiStandards.md.
/// This module has no authentication yet, so "actor" (created/updated-by) is null for
/// now; wire it to the authenticated principal once Authentication ships.
/// </summary>
[ApiController]
[Route("api/v1/users")]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly IValidator<CreateUserRequest> _createValidator;
    private readonly IValidator<UpdateUserRequest> _updateValidator;
    private readonly IValidator<SetPasswordRequest> _setPasswordValidator;
    private readonly ILogger<UsersController> _logger;

    public UsersController(
        IUserService userService,
        IValidator<CreateUserRequest> createValidator,
        IValidator<UpdateUserRequest> updateValidator,
        IValidator<SetPasswordRequest> setPasswordValidator,
        ILogger<UsersController> logger)
    {
        _userService = userService;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _setPasswordValidator = setPasswordValidator;
        _logger = logger;
    }

    /// <summary>Creates a new user.</summary>
    /// <response code="201">The user was created.</response>
    /// <response code="400">The request failed validation.</response>
    /// <response code="409">A user with the given username or email already exists.</response>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateUserRequest request, CancellationToken cancellationToken)
    {
        var validation = await _createValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(BuildValidationError(validation));
        }

        var result = await _userService.CreateAsync(request, actorId: null, cancellationToken);
        if (!result.IsSuccess)
        {
            return MapFailure(result.ErrorCode!, result.Error!);
        }

        _logger.LogInformation("POST /api/v1/users succeeded for {Email}", request.Email);

        return CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, Envelope(result.Value));
    }

    /// <summary>Updates an existing user.</summary>
    /// <response code="200">The user was updated.</response>
    /// <response code="400">The request failed validation.</response>
    /// <response code="404">No user was found for the given id.</response>
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateUserRequest request, CancellationToken cancellationToken)
    {
        var validation = await _updateValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(BuildValidationError(validation));
        }

        var result = await _userService.UpdateAsync(id, request, actorId: null, cancellationToken);
        return result.IsSuccess ? Ok(Envelope(result.Value)) : MapFailure(result.ErrorCode!, result.Error!);
    }

    /// <summary>Soft-deletes a user.</summary>
    /// <response code="204">The user was deleted.</response>
    /// <response code="404">No user was found for the given id.</response>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await _userService.DeleteAsync(id, actorId: null, cancellationToken);
        return result.IsSuccess ? NoContent() : MapFailure(result.ErrorCode!, result.Error!);
    }

    /// <summary>Gets a single user by id.</summary>
    /// <response code="200">The user was found.</response>
    /// <response code="404">No user was found for the given id.</response>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _userService.GetByIdAsync(id, cancellationToken);
        return result.IsSuccess ? Ok(Envelope(result.Value)) : MapFailure(result.ErrorCode!, result.Error!);
    }

    /// <summary>
    /// Lists users with paging, sorting, and filtering. "Search" is this same endpoint's
    /// <c>search</c> query parameter rather than a separate route (see
    /// <see cref="UserListQuery"/>) — there is no dedicated /search route.
    /// </summary>
    /// <response code="200">A page of users.</response>
    [HttpGet]
    public async Task<IActionResult> GetPaged([FromQuery] UserListQuery query, CancellationToken cancellationToken)
    {
        var paged = await _userService.GetPagedAsync(query, cancellationToken);

        var meta = new PaginationMeta
        {
            Page = paged.Page,
            PageSize = paged.PageSize,
            TotalCount = paged.TotalCount,
            TotalPages = paged.TotalPages,
        };

        return Ok(new ApiResponse<IReadOnlyList<UserResponse>> { Data = paged.Items, Meta = meta });
    }

    /// <summary>Activates a user.</summary>
    /// <response code="200">The user was activated.</response>
    /// <response code="404">No user was found for the given id.</response>
    [HttpPost("{id:guid}/activate")]
    public async Task<IActionResult> Activate(Guid id, CancellationToken cancellationToken)
    {
        var result = await _userService.ActivateAsync(id, actorId: null, cancellationToken);
        return result.IsSuccess ? Ok(Envelope(result.Value)) : MapFailure(result.ErrorCode!, result.Error!);
    }

    /// <summary>Deactivates a user.</summary>
    /// <response code="200">The user was deactivated.</response>
    /// <response code="404">No user was found for the given id.</response>
    [HttpPost("{id:guid}/deactivate")]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken cancellationToken)
    {
        var result = await _userService.DeactivateAsync(id, actorId: null, cancellationToken);
        return result.IsSuccess ? Ok(Envelope(result.Value)) : MapFailure(result.ErrorCode!, result.Error!);
    }

    /// <summary>Uploads/replaces a user's profile photo (JPG/PNG/WEBP, max 2MB). Never set at creation — always a later, separate step from the User Details page.</summary>
    /// <response code="200">The profile photo was uploaded.</response>
    /// <response code="400">The file failed validation.</response>
    /// <response code="404">No user was found for the given id.</response>
    [HttpPost("{id:guid}/profile-photo")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadProfilePhoto(Guid id, IFormFile photo, CancellationToken cancellationToken)
    {
        if (photo is null || photo.Length == 0)
        {
            return BadRequest(BuildFileRequiredError());
        }

        await using var stream = photo.OpenReadStream();
        var result = await _userService.UploadProfilePhotoAsync(id, stream, photo.FileName, photo.ContentType, photo.Length, actorId: null, cancellationToken);
        return result.IsSuccess ? Ok(Envelope(result.Value)) : MapFailure(result.ErrorCode!, result.Error!);
    }

    /// <summary>Sets or resets a user's password (admin action). The only way a user ever
    /// gets a PasswordHash — user creation has no credential step.</summary>
    /// <response code="200">The password was set.</response>
    /// <response code="400">The request failed validation.</response>
    /// <response code="404">No user was found for the given id.</response>
    [HttpPost("{id:guid}/password")]
    public async Task<IActionResult> SetPassword(Guid id, [FromBody] SetPasswordRequest request, CancellationToken cancellationToken)
    {
        var validation = await _setPasswordValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(BuildValidationError(validation));
        }

        var result = await _userService.SetPasswordAsync(id, request.Password, actorId: null, cancellationToken);
        return result.IsSuccess ? Ok(Envelope(result.Value)) : MapFailure(result.ErrorCode!, result.Error!);
    }

    private static ApiResponse<UserResponse> Envelope(UserResponse? data) => new() { Data = data };

    private ApiErrorResponse BuildFileRequiredError() => new()
    {
        ErrorCode = "VALIDATION.FAILED",
        Message = "A file is required.",
        CorrelationId = HttpContext.GetCorrelationId(),
        Timestamp = DateTime.UtcNow,
    };

    private IActionResult MapFailure(string errorCode, string message)
    {
        var status = errorCode switch
        {
            UserErrorCodes.NotFound => StatusCodes.Status404NotFound,
            UserErrorCodes.DuplicateEmail => StatusCodes.Status409Conflict,
            UserErrorCodes.DuplicateUsername => StatusCodes.Status409Conflict,
            UserErrorCodes.InvalidRole => StatusCodes.Status400BadRequest,
            UserErrorCodes.InvalidFile => StatusCodes.Status400BadRequest,
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
