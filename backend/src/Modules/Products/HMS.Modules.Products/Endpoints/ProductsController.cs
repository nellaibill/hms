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

/// <summary>Product/item master CRUD — the aggregate root every child entity in this module (barcodes, batches, prices, images, attribute values, tax mappings) hangs off of.</summary>
[ApiController]
[RequireFeature("products")]
[Route("api/v1/products")]
public class ProductsController : ControllerBase
{
    private readonly IProductService _service;
    private readonly IValidator<CreateProductRequest> _createValidator;
    private readonly IValidator<UpdateProductRequest> _updateValidator;

    public ProductsController(IProductService service, IValidator<CreateProductRequest> createValidator, IValidator<UpdateProductRequest> updateValidator)
    {
        _service = service;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    /// <summary>Creates a new product.</summary>
    [Authorize]
    [RequirePermission("pharmacy.create")]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateProductRequest request, CancellationToken cancellationToken)
    {
        var validation = await _createValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(BuildValidationError(validation));
        }

        var result = await _service.CreateAsync(request, actorId: User.GetUserId(), cancellationToken);
        return !result.IsSuccess
            ? MapFailure(result.ErrorCode!, result.Error!)
            : CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, Envelope(result.Value));
    }

    /// <summary>Updates a product.</summary>
    [Authorize]
    [RequirePermission("pharmacy.edit")]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateProductRequest request, CancellationToken cancellationToken)
    {
        var validation = await _updateValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(BuildValidationError(validation));
        }

        var result = await _service.UpdateAsync(id, request, actorId: User.GetUserId(), cancellationToken);
        return result.IsSuccess ? Ok(Envelope(result.Value)) : MapFailure(result.ErrorCode!, result.Error!);
    }

    /// <summary>Gets a single product by id.</summary>
    /// <remarks><c>costPrice</c> is redacted to 0 unless the caller also holds
    /// <c>pharmacy.view-cost</c> — see <see cref="RedactCostPrice"/>.</remarks>
    [Authorize]
    [RequirePermission("pharmacy.view")]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _service.GetByIdAsync(id, cancellationToken);
        return result.IsSuccess ? Ok(Envelope(result.Value is null ? null : RedactCostPrice(result.Value))) : MapFailure(result.ErrorCode!, result.Error!);
    }

    /// <summary>Lists products with paging, search, and category/brand/active-status filtering.</summary>
    /// <remarks><c>costPrice</c> is redacted to 0 unless the caller also holds
    /// <c>pharmacy.view-cost</c> — see <see cref="RedactCostPrice"/>.</remarks>
    [Authorize]
    [RequirePermission("pharmacy.view")]
    [HttpGet]
    public async Task<IActionResult> GetPaged([FromQuery] ProductListQuery query, CancellationToken cancellationToken)
    {
        var paged = await _service.GetPagedAsync(query, cancellationToken);
        var meta = new PaginationMeta { Page = paged.Page, PageSize = paged.PageSize, TotalCount = paged.TotalCount, TotalPages = paged.TotalPages };
        var items = paged.Items.Select(RedactCostPrice).ToList();

        return Ok(new ApiResponse<IReadOnlyList<ProductResponse>> { Data = items, Meta = meta });
    }

    /// <summary><c>pharmacy.view</c> alone (the gate on both actions above) grants list/detail
    /// visibility to every day-to-day dispensing user — <c>CostPrice</c> (procurement cost) is
    /// commercially sensitive margin data on top of that, so it's zeroed out here unless the
    /// caller separately holds <c>pharmacy.view-cost</c> (ADR-077). A plain 0 rather than a
    /// nullable field: <c>ProductResponse.CostPrice</c> is a non-nullable <c>decimal</c> used
    /// identically by both create/edit forms (which always have the real value) and this
    /// read path, and changing its nullability would be a wider contract change than this fix
    /// warrants — 0 is already how an uncosted product reads elsewhere in this codebase (see
    /// <c>update-diagnostic-cost-prices.ps1</c>'s identical "not yet costed" convention).</summary>
    private ProductResponse RedactCostPrice(ProductResponse response) =>
        User.HasPermission("pharmacy.view-cost") ? response : response with { CostPrice = 0 };

    private static ApiResponse<ProductResponse> Envelope(ProductResponse? data) => new() { Data = data };

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
