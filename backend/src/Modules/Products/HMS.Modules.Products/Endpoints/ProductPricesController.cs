using FluentValidation;
using FluentValidation.Results;
using HMS.Modules.Products.Application;
using HMS.Modules.Products.Contracts;
using HMS.Shared.Infrastructure;
using HMS.Shared.Kernel;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HMS.Modules.Products.Endpoints;

/// <summary>Price CRUD, scoped to a parent product.</summary>
[ApiController]
[Route("api/v1/products/{productId:guid}/prices")]
public class ProductPricesController : ControllerBase
{
    private readonly IProductPriceService _service;
    private readonly IValidator<CreateProductPriceRequest> _createValidator;
    private readonly IValidator<UpdateProductPriceRequest> _updateValidator;

    public ProductPricesController(IProductPriceService service, IValidator<CreateProductPriceRequest> createValidator, IValidator<UpdateProductPriceRequest> updateValidator)
    {
        _service = service;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    /// <summary>Adds a price entry to a product.</summary>
    [HttpPost]
    public async Task<IActionResult> Create(Guid productId, [FromBody] CreateProductPriceRequest request, CancellationToken cancellationToken)
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

    /// <summary>Updates a product price entry.</summary>
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid productId, Guid id, [FromBody] UpdateProductPriceRequest request, CancellationToken cancellationToken)
    {
        var validation = await _updateValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(BuildValidationError(validation));
        }

        var result = await _service.UpdateAsync(productId, id, request, actorId: User.GetUserId(), cancellationToken);
        return result.IsSuccess ? Ok(Envelope(result.Value)) : MapFailure(result.ErrorCode!, result.Error!);
    }

    /// <summary>Gets a single price entry by id.</summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid productId, Guid id, CancellationToken cancellationToken)
    {
        var result = await _service.GetByIdAsync(productId, id, cancellationToken);
        return result.IsSuccess ? Ok(Envelope(result.Value)) : MapFailure(result.ErrorCode!, result.Error!);
    }

    /// <summary>Lists a product's price history with paging, price-type, and active-status filtering.</summary>
    [HttpGet]
    public async Task<IActionResult> GetPaged(Guid productId, [FromQuery] ProductPriceListQuery query, CancellationToken cancellationToken)
    {
        var paged = await _service.GetPagedAsync(productId, query, cancellationToken);
        var meta = new PaginationMeta { Page = paged.Page, PageSize = paged.PageSize, TotalCount = paged.TotalCount, TotalPages = paged.TotalPages };

        return Ok(new ApiResponse<IReadOnlyList<ProductPriceResponse>> { Data = paged.Items, Meta = meta });
    }

    private static ApiResponse<ProductPriceResponse> Envelope(ProductPriceResponse? data) => new() { Data = data };

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
