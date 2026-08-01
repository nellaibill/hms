using HMS.Modules.Masters.Application;
using HMS.Modules.Products.Application.Abstractions;
using HMS.Modules.Products.Application.Mapping;
using HMS.Modules.Products.Contracts;
using HMS.Modules.Products.Domain;
using HMS.Shared.Kernel;

namespace HMS.Modules.Products.Application;

public interface IProductPriceService
{
    Task<Result<ProductPriceResponse>> CreateAsync(Guid productId, CreateProductPriceRequest request, Guid? actorId, CancellationToken cancellationToken);

    Task<Result<ProductPriceResponse>> UpdateAsync(Guid productId, Guid id, UpdateProductPriceRequest request, Guid? actorId, CancellationToken cancellationToken);

    Task<Result<ProductPriceResponse>> GetByIdAsync(Guid productId, Guid id, CancellationToken cancellationToken);

    Task<PagedResult<ProductPriceResponse>> GetPagedAsync(Guid productId, ProductPriceListQuery query, CancellationToken cancellationToken);
}

internal class ProductPriceService : IProductPriceService
{
    private readonly IProductPriceRepository _repository;
    private readonly IProductRepository _productRepository;
    private readonly ICurrencyService _currencyService;

    public ProductPriceService(IProductPriceRepository repository, IProductRepository productRepository, ICurrencyService currencyService)
    {
        _repository = repository;
        _productRepository = productRepository;
        _currencyService = currencyService;
    }

    public async Task<Result<ProductPriceResponse>> CreateAsync(Guid productId, CreateProductPriceRequest request, Guid? actorId, CancellationToken cancellationToken)
    {
        if (!await _productRepository.ExistsAsync(productId, cancellationToken))
        {
            return Result<ProductPriceResponse>.Failure(ProductsErrorCodes.InvalidReference, $"Product '{productId}' was not found.");
        }

        if (!(await _currencyService.GetByIdAsync(request.CurrencyId, cancellationToken)).IsSuccess)
        {
            return Result<ProductPriceResponse>.Failure(ProductsErrorCodes.InvalidReference, $"Currency '{request.CurrencyId}' was not found.");
        }

        if (await _repository.ExistsAsync(productId, request.PriceType.Trim(), request.CurrencyId, request.EffectiveFrom, excludingId: null, cancellationToken))
        {
            return Result<ProductPriceResponse>.Failure(ProductsErrorCodes.DuplicateCode, "A price for this product, price type, currency, and effective date already exists.");
        }

        var price = ProductPrice.Create(productId, request.PriceType, request.CurrencyId, request.Price, request.EffectiveFrom, request.EffectiveTo, request.IsActive, actorId);

        await _repository.AddAsync(price, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        return Result<ProductPriceResponse>.Success(price.ToResponse());
    }

    public async Task<Result<ProductPriceResponse>> UpdateAsync(Guid productId, Guid id, UpdateProductPriceRequest request, Guid? actorId, CancellationToken cancellationToken)
    {
        var price = await _repository.GetByIdAsync(id, cancellationToken);
        if (price is null || price.ProductId != productId)
        {
            return Result<ProductPriceResponse>.Failure(ProductsErrorCodes.NotFound, $"Price '{id}' was not found for product '{productId}'.");
        }

        price.Update(request.Price, request.EffectiveFrom, request.EffectiveTo, request.IsActive, actorId);
        await _repository.SaveChangesAsync(cancellationToken);

        return Result<ProductPriceResponse>.Success(price.ToResponse());
    }

    public async Task<Result<ProductPriceResponse>> GetByIdAsync(Guid productId, Guid id, CancellationToken cancellationToken)
    {
        var price = await _repository.GetByIdAsync(id, cancellationToken);
        return price is null || price.ProductId != productId
            ? Result<ProductPriceResponse>.Failure(ProductsErrorCodes.NotFound, $"Price '{id}' was not found for product '{productId}'.")
            : Result<ProductPriceResponse>.Success(price.ToResponse());
    }

    public async Task<PagedResult<ProductPriceResponse>> GetPagedAsync(Guid productId, ProductPriceListQuery query, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _repository.GetPagedByProductAsync(productId, query, cancellationToken);
        return new PagedResult<ProductPriceResponse>(items.Select(p => p.ToResponse()).ToList(), query.Page, query.PageSize, totalCount);
    }
}
