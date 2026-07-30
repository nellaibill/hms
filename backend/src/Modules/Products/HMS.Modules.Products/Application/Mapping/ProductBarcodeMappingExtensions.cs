using HMS.Modules.Products.Contracts;
using HMS.Modules.Products.Domain;

namespace HMS.Modules.Products.Application.Mapping;

internal static class ProductBarcodeMappingExtensions
{
    public static ProductBarcodeResponse ToResponse(this ProductBarcode barcode) => new()
    {
        Id = barcode.Id,
        ProductId = barcode.ProductId,
        BarcodeType = barcode.BarcodeType,
        BarcodeValue = barcode.BarcodeValue,
        IsPrimary = barcode.IsPrimary,
        IsActive = barcode.IsActive,
        Notes = barcode.Notes,
        CreatedAt = barcode.CreatedAt,
        UpdatedAt = barcode.UpdatedAt,
    };
}
