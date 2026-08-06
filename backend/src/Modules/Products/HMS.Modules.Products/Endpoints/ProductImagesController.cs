using FluentValidation;
using FluentValidation.Results;
using HMS.Modules.Products.Application;
using HMS.Modules.Products.Contracts;
using HMS.Shared.Infrastructure;
using HMS.Shared.Kernel;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HMS.Modules.Products.Endpoints;

/// <summary>Image CRUD, scoped to a parent product. Creation only happens via <see cref="Upload"/> — there is no plain JSON create endpoint, since image_url must come from the storage layer (see IProductImageStorage), never an arbitrary client-supplied URL.</summary>
[ApiController]
[Route("api/v1/products/{productId:guid}/images")]
public class ProductImagesController : ControllerBase
{
    private readonly IProductImageService _service;
    private readonly IValidator<UpdateProductImageRequest> _updateValidator;

    public ProductImagesController(IProductImageService service, IValidator<UpdateProductImageRequest> updateValidator)
    {
        _service = service;
        _updateValidator = updateValidator;
    }

    /// <summary>Uploads a new image for a product (PNG/JPG/WEBP, max 5MB).</summary>
    /// <response code="201">The image was uploaded and its metadata created.</response>
    /// <response code="400">The file is missing, failed validation, or the type/order slot is already taken.</response>
    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Upload(
        Guid productId,
        IFormFile file,
        [FromForm] string imageType,
        [FromForm] bool isPrimary,
        [FromForm] int displayOrder,
        [FromForm] bool isActive,
        CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(BuildFileRequiredError());
        }

        await using var stream = file.OpenReadStream();
        var result = await _service.UploadAsync(productId, stream, file.FileName, file.Length, imageType, isPrimary, displayOrder, isActive, actorId: User.GetUserId(), cancellationToken);
        return !result.IsSuccess
            ? MapFailure(result.ErrorCode!, result.Error!)
            : CreatedAtAction(nameof(GetById), new { productId, id = result.Value!.Id }, Envelope(result.Value));
    }

    /// <summary>Updates a product image's metadata (type, order, primary flag, active status) — does not replace the file itself.</summary>
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid productId, Guid id, [FromBody] UpdateProductImageRequest request, CancellationToken cancellationToken)
    {
        var validation = await _updateValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(BuildValidationError(validation));
        }

        var result = await _service.UpdateAsync(productId, id, request, actorId: User.GetUserId(), cancellationToken);
        return result.IsSuccess ? Ok(Envelope(result.Value)) : MapFailure(result.ErrorCode!, result.Error!);
    }

    /// <summary>Gets a single image by id.</summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid productId, Guid id, CancellationToken cancellationToken)
    {
        var result = await _service.GetByIdAsync(productId, id, cancellationToken);
        return result.IsSuccess ? Ok(Envelope(result.Value)) : MapFailure(result.ErrorCode!, result.Error!);
    }

    /// <summary>Lists a product's images with paging and active-status filtering.</summary>
    [HttpGet]
    public async Task<IActionResult> GetPaged(Guid productId, [FromQuery] ProductImageListQuery query, CancellationToken cancellationToken)
    {
        var paged = await _service.GetPagedAsync(productId, query, cancellationToken);
        var meta = new PaginationMeta { Page = paged.Page, PageSize = paged.PageSize, TotalCount = paged.TotalCount, TotalPages = paged.TotalPages };

        return Ok(new ApiResponse<IReadOnlyList<ProductImageResponse>> { Data = paged.Items, Meta = meta });
    }

    private static ApiResponse<ProductImageResponse> Envelope(ProductImageResponse? data) => new() { Data = data };

    private IActionResult MapFailure(string errorCode, string message)
    {
        var status = errorCode switch
        {
            ProductsErrorCodes.NotFound => StatusCodes.Status404NotFound,
            ProductsErrorCodes.DuplicateCode => StatusCodes.Status400BadRequest,
            ProductsErrorCodes.InvalidReference => StatusCodes.Status400BadRequest,
            _ => StatusCodes.Status400BadRequest,
        };

        var error = new ApiErrorResponse { ErrorCode = errorCode, Message = message, CorrelationId = HttpContext.GetCorrelationId(), Timestamp = DateTime.UtcNow };
        return StatusCode(status, error);
    }

    private ApiErrorResponse BuildFileRequiredError() => new()
    {
        ErrorCode = "VALIDATION.FAILED",
        Message = "A file is required.",
        CorrelationId = HttpContext.GetCorrelationId(),
        Timestamp = DateTime.UtcNow,
    };

    private ApiErrorResponse BuildValidationError(ValidationResult validation) => new()
    {
        ErrorCode = "VALIDATION.FAILED",
        Message = "One or more validation errors occurred.",
        ValidationErrors = validation.Errors.Select(e => new ValidationErrorItem { Field = e.PropertyName, Message = e.ErrorMessage }).ToList(),
        CorrelationId = HttpContext.GetCorrelationId(),
        Timestamp = DateTime.UtcNow,
    };
}
