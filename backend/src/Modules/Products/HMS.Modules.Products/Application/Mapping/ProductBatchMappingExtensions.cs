using HMS.Modules.Products.Contracts;
using HMS.Modules.Products.Domain;

namespace HMS.Modules.Products.Application.Mapping;

internal static class ProductBatchMappingExtensions
{
    public static ProductBatchResponse ToResponse(this ProductBatch batch) => new()
    {
        Id = batch.Id,
        ProductId = batch.ProductId,
        BatchNo = batch.BatchNo,
        ManufactureDate = batch.ManufactureDate,
        ExpiryDate = batch.ExpiryDate,
        IsActive = batch.IsActive,
        CreatedAt = batch.CreatedAt,
        UpdatedAt = batch.UpdatedAt,
    };
}
