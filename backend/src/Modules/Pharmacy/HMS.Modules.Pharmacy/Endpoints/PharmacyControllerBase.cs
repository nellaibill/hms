using FluentValidation;
using FluentValidation.Results;
using HMS.Shared.Infrastructure;
using HMS.Shared.Kernel;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HMS.Modules.Pharmacy.Endpoints;

/// <summary>
/// What every document controller in this module does the same way: validate the body, turn
/// a failed Result into the right status code, and wrap data in the standard envelope.
/// </summary>
public abstract class PharmacyControllerBase : ControllerBase
{
    /// <summary>Validates <paramref name="request"/>; returns the 400 to send, or null when it is valid.</summary>
    protected async Task<IActionResult?> ValidateAsync<T>(IValidator<T> validator, T? request, CancellationToken cancellationToken)
        where T : class
    {
        if (request is null)
        {
            return BadRequest(new ApiErrorResponse
            {
                ErrorCode = "VALIDATION.FAILED",
                Message = "The request body is missing or could not be parsed.",
                CorrelationId = HttpContext.GetCorrelationId(),
                Timestamp = DateTime.UtcNow,
            });
        }

        var validation = await validator.ValidateAsync(request, cancellationToken);
        return validation.IsValid ? null : BadRequest(BuildValidationError(validation));
    }

    protected IActionResult FromResult<T>(Result<T> result)
        => result.IsSuccess ? Ok(new ApiResponse<T> { Data = result.Value }) : Failure(result.ErrorCode!, result.Error!);

    protected IActionResult CreatedFromResult<T>(Result<T> result)
        => result.IsSuccess
            ? StatusCode(StatusCodes.Status201Created, new ApiResponse<T> { Data = result.Value })
            : Failure(result.ErrorCode!, result.Error!);

    protected IActionResult Paged<T>(PagedResult<T> paged)
    {
        var meta = new PaginationMeta { Page = paged.Page, PageSize = paged.PageSize, TotalCount = paged.TotalCount, TotalPages = paged.TotalPages };
        return Ok(new ApiResponse<IReadOnlyList<T>> { Data = paged.Items, Meta = meta });
    }

    protected IActionResult Data<T>(T data) => Ok(new ApiResponse<T> { Data = data });

    /// <summary>Not found is 404; everything else a service refuses is a rule of the pharmacy
    /// being enforced against otherwise well-formed input, which is 409.</summary>
    protected IActionResult Failure(string errorCode, string message)
    {
        var status = errorCode.EndsWith(".NOT_FOUND", StringComparison.Ordinal) ? StatusCodes.Status404NotFound : StatusCodes.Status409Conflict;
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
}
