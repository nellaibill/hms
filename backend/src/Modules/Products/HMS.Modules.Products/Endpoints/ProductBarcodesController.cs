using FluentValidation;
using FluentValidation.Results;
using HMS.Modules.Products.Application;
using HMS.Modules.Products.Contracts;
using HMS.Shared.Infrastructure;
using HMS.Shared.Kernel;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HMS.Modules.Products.Endpoints;

/// <summary>Barcode CRUD, scoped to a parent product.</summary>
[ApiController]
[Route("api/v1/products/{productId:guid}/barcodes")]
public class ProductBarcodesController : ControllerBase
{
    private readonly IProductBarcodeService _service;
    private readonly IValidator<CreateProductBarcodeRequest> _createValidator;
    private readonly IValidator<UpdateProductBarcodeRequest> _updateValidator;

    public ProductBarcodesController(IProductBarcodeService service, IValidator<CreateProductBarcodeRequest> createValidator, IValidator<UpdateProductBarcodeRequest> updateValidator)
    {
        _service = service;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    /// <summary>Adds a barcode to a product.</summary>
    [HttpPost]
    public async Task<IActionResult> Create(Guid productId, [FromBody] CreateProductBarcodeRequest request, CancellationToken cancellationToken)
    {
        var validation = await _createValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(BuildValidationError(validation));
        }

        var result = await _service.CreateAsync(productId, request, actorId: User.GetUserId(), cancellationToken);
        return !result.IsSuccess
            ? MapFailure(result.ErrorCode!, result.Error!)
            : CreatedAtAction(nameof(GetById), new { productId, id = result.Value!.Id }, Envelope(result.Value));
    }

    /// <summary>Updates a product barcode.</summary>
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid productId, Guid id, [FromBody] UpdateProductBarcodeRequest request, CancellationToken cancellationToken)
    {
        var validation = await _updateValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(BuildValidationError(validation));
        }

        var result = await _service.UpdateAsync(productId, id, request, actorId: User.GetUserId(), cancellationToken);
        return result.IsSuccess ? Ok(Envelope(result.Value)) : MapFailure(result.ErrorCode!, result.Error!);
    }

    /// <summary>Gets a single barcode by id.</summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid productId, Guid id, CancellationToken cancellationToken)
    {
        var result = await _service.GetByIdAsync(productId, id, cancellationToken);
        return result.IsSuccess ? Ok(Envelope(result.Value)) : MapFailure(result.ErrorCode!, result.Error!);
    }

    /// <summary>Lists a product's barcodes with paging and active-status filtering.</summary>
    [HttpGet]
    public async Task<IActionResult> GetPaged(Guid productId, [FromQuery] ProductBarcodeListQuery query, CancellationToken cancellationToken)
    {
        var paged = await _service.GetPagedAsync(productId, query, cancellationToken);
        var meta = new PaginationMeta { Page = paged.Page, PageSize = paged.PageSize, TotalCount = paged.TotalCount, TotalPages = paged.TotalPages };

        return Ok(new ApiResponse<IReadOnlyList<ProductBarcodeResponse>> { Data = paged.Items, Meta = meta });
    }

    private static ApiResponse<ProductBarcodeResponse> Envelope(ProductBarcodeResponse? data) => new() { Data = data };

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

    private ApiErrorResponse BuildValidationError(ValidationResult validation) => new()
    {
        ErrorCode = "VALIDATION.FAILED",
        Message = "One or more validation errors occurred.",
        ValidationErrors = validation.Errors.Select(e => new ValidationErrorItem { Field = e.PropertyName, Message = e.ErrorMessage }).ToList(),
        CorrelationId = HttpContext.GetCorrelationId(),
        Timestamp = DateTime.UtcNow,
    };
}
