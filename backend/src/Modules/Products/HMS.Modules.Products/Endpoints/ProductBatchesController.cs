using FluentValidation;
using FluentValidation.Results;
using HMS.Modules.Products.Application;
using HMS.Modules.Products.Contracts;
using HMS.Shared.Infrastructure;
using HMS.Shared.Kernel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HMS.Modules.Products.Endpoints;

/// <summary>Batch/expiry CRUD, scoped to a parent product.</summary>
[ApiController]
[RequireFeature("products")]
[Route("api/v1/products/{productId:guid}/batches")]
public class ProductBatchesController : ControllerBase
{
    private readonly IProductBatchService _service;
    private readonly IValidator<CreateProductBatchRequest> _createValidator;
    private readonly IValidator<UpdateProductBatchRequest> _updateValidator;

    public ProductBatchesController(IProductBatchService service, IValidator<CreateProductBatchRequest> createValidator, IValidator<UpdateProductBatchRequest> updateValidator)
    {
        _service = service;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    /// <summary>Adds a batch to a product.</summary>
    [Authorize]
    [RequirePermission("pharmacy.create")]
    [HttpPost]
    public async Task<IActionResult> Create(Guid productId, [FromBody] CreateProductBatchRequest request, CancellationToken cancellationToken)
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

    /// <summary>Updates a product batch.</summary>
    [Authorize]
    [RequirePermission("pharmacy.edit")]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid productId, Guid id, [FromBody] UpdateProductBatchRequest request, CancellationToken cancellationToken)
    {
        var validation = await _updateValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(BuildValidationError(validation));
        }

        var result = await _service.UpdateAsync(productId, id, request, actorId: User.GetUserId(), cancellationToken);
        return result.IsSuccess ? Ok(Envelope(result.Value)) : MapFailure(result.ErrorCode!, result.Error!);
    }

    /// <summary>Gets a single batch by id.</summary>
    [Authorize]
    [RequirePermission("pharmacy.view")]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid productId, Guid id, CancellationToken cancellationToken)
    {
        var result = await _service.GetByIdAsync(productId, id, cancellationToken);
        return result.IsSuccess ? Ok(Envelope(result.Value)) : MapFailure(result.ErrorCode!, result.Error!);
    }

    /// <summary>Lists a product's batches with paging and active-status filtering.</summary>
    [Authorize]
    [RequirePermission("pharmacy.view")]
    [HttpGet]
    public async Task<IActionResult> GetPaged(Guid productId, [FromQuery] ProductBatchListQuery query, CancellationToken cancellationToken)
    {
        var paged = await _service.GetPagedAsync(productId, query, cancellationToken);
        var meta = new PaginationMeta { Page = paged.Page, PageSize = paged.PageSize, TotalCount = paged.TotalCount, TotalPages = paged.TotalPages };

        return Ok(new ApiResponse<IReadOnlyList<ProductBatchResponse>> { Data = paged.Items, Meta = meta });
    }

    private static ApiResponse<ProductBatchResponse> Envelope(ProductBatchResponse? data) => new() { Data = data };

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
