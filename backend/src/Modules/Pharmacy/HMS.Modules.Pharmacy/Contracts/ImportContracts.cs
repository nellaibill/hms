namespace HMS.Modules.Pharmacy.Contracts;

/// <summary>
/// One row of a go-live spreadsheet: a product, and optionally the stock of one of its
/// batches already on the shelf. Everything but the name is optional — category, maker, brand
/// and unit are matched by name and created when new, or defaulted when blank, so a pharmacy
/// can start from whatever list it already has.
/// </summary>
public record ImportProductRow
{
    public string ProductName { get; init; } = string.Empty;
    public string? GenericName { get; init; }
    public string? Sku { get; init; }
    public string? Category { get; init; }
    public string? Manufacturer { get; init; }
    public string? Brand { get; init; }
    public string? Unit { get; init; }
    public string? HsnCode { get; init; }
    public decimal? GstRatePercent { get; init; }
    public decimal? UnitsPerPack { get; init; }
    public string? DosageForm { get; init; }
    public string? Strength { get; init; }
    public string? Schedule { get; init; }
    public string? RackLocation { get; init; }
    public decimal? ReorderLevel { get; init; }

    /// <summary>Per pack.</summary>
    public decimal? Mrp { get; init; }
    public decimal? PurchaseRate { get; init; }
    public decimal? SellingRate { get; init; }

    public string? BatchNo { get; init; }
    public DateOnly? ExpiryDate { get; init; }

    /// <summary>Units on the shelf today (tablets, not strips).</summary>
    public decimal? OpeningQuantity { get; init; }
}

public record ImportProductsRequest
{
    /// <summary>The store opening stock goes into. Omitted, it is the default store.</summary>
    public Guid? StoreId { get; init; }

    /// <summary>Check the rows and report what would happen, without saving anything.</summary>
    public bool ValidateOnly { get; init; }

    /// <summary>The spreadsheet row number of the first row sent, so results can be reported
    /// against the row the user sees when a file is sent in several parts.</summary>
    public int FirstRowNumber { get; init; } = 2;
    public IReadOnlyList<ImportProductRow> Rows { get; init; } = [];
}

public enum ImportRowStatus
{
    /// <summary>A new product was (or would be) created.</summary>
    Created,

    /// <summary>The product was already in the catalog; nothing about it was changed.</summary>
    Matched,
    Error,
}

public record ImportRowResult
{
    public int RowNumber { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public ImportRowStatus Status { get; init; }

    /// <summary>Units of opening stock this row put (or would put) on the shelf.</summary>
    public decimal StockAdded { get; init; }

    /// <summary>Why the row failed, or anything worth knowing about a row that went in.</summary>
    public string? Message { get; init; }
}

public record ImportProductsResponse
{
    public bool ValidateOnly { get; init; }
    public int ProductsCreated { get; init; }
    public int ProductsMatched { get; init; }
    public int BatchesStocked { get; init; }
    public int Errors { get; init; }
    public IReadOnlyList<ImportRowResult> Rows { get; init; } = [];
}
