using HMS.Modules.Masters.Application;
using HMS.Modules.Products.Application.Abstractions;
using HMS.Modules.Products.Application.Mapping;
using HMS.Modules.Products.Contracts;
using HMS.Modules.Products.Domain;
using HMS.Shared.Kernel;

namespace HMS.Modules.Products.Application;

public interface IProductService
{
    Task<Result<ProductResponse>> CreateAsync(CreateProductRequest request, Guid? actorId, CancellationToken cancellationToken);

    Task<Result<ProductResponse>> UpdateAsync(Guid id, UpdateProductRequest request, Guid? actorId, CancellationToken cancellationToken);

    Task<Result<ProductResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<PagedResult<ProductResponse>> GetPagedAsync(ProductListQuery query, CancellationToken cancellationToken);
}

/// <summary>
/// Validates classification/unit references (<see cref="CreateProductRequest.BrandId"/>,
/// CategoryId, UomId, ...) against the Masters module's public service seam before writing —
/// the cross-module analog of ProductCategoryService's own-schema ParentId check. Masters is
/// treated as already-committed data per docs/DatabaseArchitecture.md §10 (no cross-module
/// transaction), so this is a pre-flight existence check, not a distributed transaction.
/// </summary>
internal class ProductService : IProductService
{
    private readonly IProductRepository _repository;
    private readonly IBrandService _brandService;
    private readonly IManufacturerService _manufacturerService;
    private readonly IProductCategoryService _categoryService;
    private readonly IProductSubCategoryService _subCategoryService;
    private readonly IProductGroupService _groupService;
    private readonly IUnitOfMeasureService _uomService;

    public ProductService(
        IProductRepository repository,
        IBrandService brandService,
        IManufacturerService manufacturerService,
        IProductCategoryService categoryService,
        IProductSubCategoryService subCategoryService,
        IProductGroupService groupService,
        IUnitOfMeasureService uomService)
    {
        _repository = repository;
        _brandService = brandService;
        _manufacturerService = manufacturerService;
        _categoryService = categoryService;
        _subCategoryService = subCategoryService;
        _groupService = groupService;
        _uomService = uomService;
    }

    public async Task<Result<ProductResponse>> CreateAsync(CreateProductRequest request, Guid? actorId, CancellationToken cancellationToken)
    {
        if (await _repository.ExistsBySkuAsync(request.Sku.Trim().ToUpperInvariant(), excludingId: null, cancellationToken))
        {
            return Result<ProductResponse>.Failure(ProductsErrorCodes.DuplicateCode, $"SKU '{request.Sku}' is already in use.");
        }

        if (await _repository.ExistsByProductCodeAsync(request.ProductCode.Trim().ToUpperInvariant(), excludingId: null, cancellationToken))
        {
            return Result<ProductResponse>.Failure(ProductsErrorCodes.DuplicateCode, $"Product code '{request.ProductCode}' is already in use.");
        }

        var referenceError = await ValidateReferencesAsync(
            request.BrandId, request.ManufacturerId, request.CategoryId, request.SubCategoryId, request.GroupId, request.UomId, request.BaseUomId, cancellationToken);
        if (referenceError is not null)
        {
            return Result<ProductResponse>.Failure(ProductsErrorCodes.InvalidReference, referenceError);
        }

        var product = Product.Create(
            request.Sku, request.ProductCode, request.ProductName, request.GenericName, request.Description,
            request.BrandId, request.ManufacturerId, request.CategoryId, request.SubCategoryId, request.GroupId, request.UomId, request.BaseUomId,
            request.IsBatchTracked, request.IsSerialized, request.IsActive,
            request.ReorderLevel, request.MinStockLevel, request.MaxStockLevel, request.Mrp, request.CostPrice, request.SellingPrice,
            request.HsnCode, request.Weight, request.Volume, actorId);

        await _repository.AddAsync(product, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        return Result<ProductResponse>.Success(product.ToResponse());
    }

    public async Task<Result<ProductResponse>> UpdateAsync(Guid id, UpdateProductRequest request, Guid? actorId, CancellationToken cancellationToken)
    {
        var product = await _repository.GetByIdAsync(id, cancellationToken);
        if (product is null)
        {
            return Result<ProductResponse>.Failure(ProductsErrorCodes.NotFound, $"Product '{id}' was not found.");
        }

        var referenceError = await ValidateReferencesAsync(
            request.BrandId, request.ManufacturerId, request.CategoryId, request.SubCategoryId, request.GroupId, request.UomId, request.BaseUomId, cancellationToken);
        if (referenceError is not null)
        {
            return Result<ProductResponse>.Failure(ProductsErrorCodes.InvalidReference, referenceError);
        }

        product.Update(
            request.ProductName, request.GenericName, request.Description,
            request.BrandId, request.ManufacturerId, request.CategoryId, request.SubCategoryId, request.GroupId, request.UomId, request.BaseUomId,
            request.IsBatchTracked, request.IsSerialized, request.IsActive,
            request.ReorderLevel, request.MinStockLevel, request.MaxStockLevel, request.Mrp, request.CostPrice, request.SellingPrice,
            request.HsnCode, request.Weight, request.Volume, actorId);

        await _repository.SaveChangesAsync(cancellationToken);

        return Result<ProductResponse>.Success(product.ToResponse());
    }

    public async Task<Result<ProductResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var product = await _repository.GetByIdAsync(id, cancellationToken);
        return product is null
            ? Result<ProductResponse>.Failure(ProductsErrorCodes.NotFound, $"Product '{id}' was not found.")
            : Result<ProductResponse>.Success(product.ToResponse());
    }

    public async Task<PagedResult<ProductResponse>> GetPagedAsync(ProductListQuery query, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _repository.GetPagedAsync(query, cancellationToken);
        return new PagedResult<ProductResponse>(items.Select(p => p.ToResponse()).ToList(), query.Page, query.PageSize, totalCount);
    }

    private async Task<string?> ValidateReferencesAsync(
        Guid brandId, Guid manufacturerId, Guid categoryId, Guid subCategoryId, Guid groupId, Guid uomId, Guid baseUomId, CancellationToken cancellationToken)
    {
        if (!(await _brandService.GetByIdAsync(brandId, cancellationToken)).IsSuccess)
        {
            return $"Brand '{brandId}' was not found.";
        }

        if (!(await _manufacturerService.GetByIdAsync(manufacturerId, cancellationToken)).IsSuccess)
        {
            return $"Manufacturer '{manufacturerId}' was not found.";
        }

        if (!(await _categoryService.GetByIdAsync(categoryId, cancellationToken)).IsSuccess)
        {
            return $"Product category '{categoryId}' was not found.";
        }

        if (!(await _subCategoryService.GetByIdAsync(subCategoryId, cancellationToken)).IsSuccess)
        {
            return $"Product sub-category '{subCategoryId}' was not found.";
        }

        if (!(await _groupService.GetByIdAsync(groupId, cancellationToken)).IsSuccess)
        {
            return $"Product group '{groupId}' was not found.";
        }

        if (!(await _uomService.GetByIdAsync(uomId, cancellationToken)).IsSuccess)
        {
            return $"Unit of measure '{uomId}' was not found.";
        }

        if (!(await _uomService.GetByIdAsync(baseUomId, cancellationToken)).IsSuccess)
        {
            return $"Base unit of measure '{baseUomId}' was not found.";
        }

        return null;
    }
}
