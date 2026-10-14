using System.Text;
using HMS.Modules.Masters.Application;
using HMS.Modules.Masters.Contracts;
using HMS.Modules.Pharmacy.Application.Abstractions;
using HMS.Modules.Pharmacy.Contracts;
using HMS.Modules.Products.Application;
using HMS.Modules.Products.Contracts;
using HMS.Shared.Kernel;

namespace HMS.Modules.Pharmacy.Application;

public interface IProductImportService
{
    Task<Result<ImportProductsResponse>> ImportAsync(ImportProductsRequest request, Guid? actorId, CancellationToken cancellationToken);
}

/// <summary>
/// Loads a pharmacy's existing product list and shelf stock from a spreadsheet — the job
/// that otherwise means keying several thousand products by hand before the first bill.
/// Rows are independent: one bad row is reported and skipped, the rest still go in. Running
/// the same file twice is safe — a product already in the catalog is matched, not duplicated,
/// and a batch that already has opening stock in the store is not stocked again.
/// </summary>
internal class ProductImportService : IProductImportService
{
    private const string DefaultCategory = "General";
    private const string DefaultManufacturer = "Unspecified";
    private const string DefaultBrand = "Unbranded";
    private const string DefaultUnit = "Nos";

    private readonly IProductService _productService;
    private readonly IProductBatchService _productBatchService;
    private readonly IBrandService _brandService;
    private readonly IManufacturerService _manufacturerService;
    private readonly IProductCategoryService _categoryService;
    private readonly IProductSubCategoryService _subCategoryService;
    private readonly IProductGroupService _groupService;
    private readonly IUnitOfMeasureService _uomService;
    private readonly IPharmacyStoreRepository _storeRepository;
    private readonly IPharmacyStockTransactionRepository _transactionRepository;
    private readonly IPharmacyUnitOfWork _unitOfWork;
    private readonly IStockPoster _stockPoster;

    // Name → id, per master, for the life of one request: a 100-row file names the same
    // dozen manufacturers over and over.
    private readonly Dictionary<string, Guid> _masterIds = new(StringComparer.OrdinalIgnoreCase);

    public ProductImportService(
        IProductService productService,
        IProductBatchService productBatchService,
        IBrandService brandService,
        IManufacturerService manufacturerService,
        IProductCategoryService categoryService,
        IProductSubCategoryService subCategoryService,
        IProductGroupService groupService,
        IUnitOfMeasureService uomService,
        IPharmacyStoreRepository storeRepository,
        IPharmacyStockTransactionRepository transactionRepository,
        IPharmacyUnitOfWork unitOfWork,
        IStockPoster stockPoster)
    {
        _productService = productService;
        _productBatchService = productBatchService;
        _brandService = brandService;
        _manufacturerService = manufacturerService;
        _categoryService = categoryService;
        _subCategoryService = subCategoryService;
        _groupService = groupService;
        _uomService = uomService;
        _storeRepository = storeRepository;
        _transactionRepository = transactionRepository;
        _unitOfWork = unitOfWork;
        _stockPoster = stockPoster;
    }

    public async Task<Result<ImportProductsResponse>> ImportAsync(ImportProductsRequest request, Guid? actorId, CancellationToken cancellationToken)
    {
        var store = await _storeRepository.ResolveAsync(request.StoreId, cancellationToken);
        if (store is null)
        {
            return Result<ImportProductsResponse>.Failure(PharmacyErrorCodes.InvalidStore, "The store was not found or is inactive.");
        }

        var results = new List<ImportRowResult>(request.Rows.Count);
        var seenBatches = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        for (var i = 0; i < request.Rows.Count; i++)
        {
            var row = request.Rows[i];
            var rowNumber = request.FirstRowNumber + i;
            ImportRowResult result;

            try
            {
                var problem = Check(row);
                result = problem is not null
                    ? Error(rowNumber, row, problem)
                    : await ImportRowAsync(row, rowNumber, store.Id, request.ValidateOnly, seenBatches, actorId, cancellationToken);
            }
            catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
            {
                // A domain rule the row breaks (a negative rate, a selling rate above MRP).
                result = Error(rowNumber, row, ex.Message);
            }

            results.Add(result);
        }

        return Result<ImportProductsResponse>.Success(new ImportProductsResponse
        {
            ValidateOnly = request.ValidateOnly,
            ProductsCreated = results.Count(r => r.Status == ImportRowStatus.Created),
            ProductsMatched = results.Count(r => r.Status == ImportRowStatus.Matched),
            BatchesStocked = results.Count(r => r.StockAdded > 0),
            Errors = results.Count(r => r.Status == ImportRowStatus.Error),
            Rows = results,
        });
    }

    /// <summary>The first thing wrong with a row, or null when it can go in.</summary>
    internal static string? Check(ImportProductRow row)
    {
        if (string.IsNullOrWhiteSpace(row.ProductName)) return "Product name is missing.";
        if (row.ProductName.Trim().Length > 200) return "Product name is longer than 200 characters.";
        if (row.Mrp < 0 || row.PurchaseRate < 0 || row.SellingRate < 0) return "Prices cannot be negative.";
        if (row.Mrp is not null && row.SellingRate > row.Mrp) return "Selling rate is above MRP.";
        if (row.GstRatePercent is < 0 or > 100) return "GST % must be between 0 and 100.";
        if (row.UnitsPerPack is <= 0) return "Units per pack must be more than zero.";
        if (row.ReorderLevel < 0) return "Reorder level cannot be negative.";
        if (row.OpeningQuantity < 0) return "Opening quantity cannot be negative.";

        if (row.OpeningQuantity > 0)
        {
            if (string.IsNullOrWhiteSpace(row.BatchNo)) return "Opening stock needs a batch number.";
            if (row.ExpiryDate is null) return "Opening stock needs an expiry date.";
            if (row.ExpiryDate < PharmacyClock.Today()) return "This batch has already expired, so it was not stocked.";
        }

        return null;
    }

    private async Task<ImportRowResult> ImportRowAsync(
        ImportProductRow row, int rowNumber, Guid storeId, bool validateOnly, HashSet<string> seenBatches, Guid? actorId, CancellationToken cancellationToken)
    {
        var notes = new List<string>();
        var product = await FindProductAsync(row, cancellationToken);
        var status = product is null ? ImportRowStatus.Created : ImportRowStatus.Matched;
        var hasStock = row.OpeningQuantity > 0;

        if (hasStock && !seenBatches.Add($"{row.ProductName.Trim()}|{row.BatchNo!.Trim()}"))
        {
            return Error(rowNumber, row, $"Batch {row.BatchNo!.Trim()} of this product appears twice in the file.");
        }

        if (validateOnly)
        {
            return new ImportRowResult
            {
                RowNumber = rowNumber,
                ProductName = row.ProductName.Trim(),
                Status = status,
                StockAdded = hasStock ? row.OpeningQuantity!.Value : 0m,
            };
        }

        if (product is null)
        {
            var created = await CreateProductAsync(row, notes, actorId, cancellationToken);
            if (!created.IsSuccess)
            {
                return Error(rowNumber, row, created.Error!);
            }

            product = created.Value!;
        }

        var stockAdded = 0m;
        if (hasStock)
        {
            var batchResult = await _productBatchService.UpsertAsync(
                product.Id,
                new UpsertProductBatchRequest
                {
                    BatchNo = row.BatchNo!,
                    ExpiryDate = row.ExpiryDate!.Value,
                    Mrp = row.Mrp,
                    PurchaseRate = row.PurchaseRate,
                    SellingRate = row.SellingRate,
                },
                actorId,
                cancellationToken);

            if (!batchResult.IsSuccess)
            {
                return Error(rowNumber, row, batchResult.Error!);
            }

            var batch = batchResult.Value!;
            if (await _transactionRepository.ExistsAsync(storeId, batch.Id, TransactionType.OpeningStock, cancellationToken))
            {
                notes.Add($"Batch {batch.BatchNo} already has opening stock, so it was not added again.");
            }
            else
            {
                var posted = await _unitOfWork.ExecuteAsync(
                    async () =>
                    {
                        var movement = await _stockPoster.PostAsync(
                            new StockMovement(
                                TransactionType.OpeningStock, storeId, product.Id, batch.Id, row.OpeningQuantity!.Value,
                                UnitCost: PharmacyPricing.UnitCost(product, batch),
                                SourceType: StockSourceType.Import,
                                Remarks: "Opening stock"),
                            actorId,
                            cancellationToken);

                        return movement.IsSuccess ? Result<decimal>.Success(row.OpeningQuantity.Value) : Result<decimal>.Failure(movement.ErrorCode!, movement.Error!);
                    },
                    cancellationToken);

                if (!posted.IsSuccess)
                {
                    return Error(rowNumber, row, posted.Error!);
                }

                stockAdded = posted.Value;
            }
        }

        return new ImportRowResult
        {
            RowNumber = rowNumber,
            ProductName = product.ProductName,
            Status = status,
            StockAdded = stockAdded,
            Message = notes.Count > 0 ? string.Join(" ", notes) : null,
        };
    }

    private async Task<ProductResponse?> FindProductAsync(ImportProductRow row, CancellationToken cancellationToken)
    {
        var name = row.ProductName.Trim();
        var sku = string.IsNullOrWhiteSpace(row.Sku) ? null : row.Sku.Trim();

        var byName = await _productService.GetPagedAsync(new ProductListQuery { Search = name, Page = 1, PageSize = 50 }, cancellationToken);
        var match = byName.Items.FirstOrDefault(p => string.Equals(p.ProductName, name, StringComparison.OrdinalIgnoreCase));
        if (match is not null || sku is null)
        {
            return match;
        }

        var bySku = await _productService.GetPagedAsync(new ProductListQuery { Search = sku, Page = 1, PageSize = 50 }, cancellationToken);
        return bySku.Items.FirstOrDefault(p => string.Equals(p.Sku, sku, StringComparison.OrdinalIgnoreCase));
    }

    private async Task<Result<ProductResponse>> CreateProductAsync(ImportProductRow row, List<string> notes, Guid? actorId, CancellationToken cancellationToken)
    {
        var categoryName = Clean(row.Category) ?? DefaultCategory;

        var categoryId = await ResolveAsync("category", categoryName, cancellationToken,
            find: async name => (await _categoryService.GetPagedAsync(new ProductCategoryListQuery { Search = name, Page = 1, PageSize = 50 }, cancellationToken))
                .Items.FirstOrDefault(c => Same(c.CategoryName, name))?.Id,
            create: async name => (await _categoryService.CreateAsync(new CreateProductCategoryRequest { CategoryCode = Code(name, 20), CategoryName = name }, actorId, cancellationToken)).Value?.Id);

        // The catalog is three levels deep (category → sub-category → group); a spreadsheet
        // has one. The lower two are filled with a "General" under whatever the level above is.
        var subCategoryId = categoryId is null ? null : await ResolveAsync($"subcategory:{categoryId}", DefaultCategory, cancellationToken,
            find: async name => (await _subCategoryService.GetPagedAsync(new ProductSubCategoryListQuery { CategoryId = categoryId, Search = name, Page = 1, PageSize = 50 }, cancellationToken))
                .Items.FirstOrDefault(s => Same(s.SubCategoryName, name))?.Id,
            create: async name => (await _subCategoryService.CreateAsync(
                new CreateProductSubCategoryRequest { SubCategoryCode = Code($"{categoryName} {name}", 30), SubCategoryName = name, CategoryId = categoryId.Value }, actorId, cancellationToken)).Value?.Id);

        var groupId = subCategoryId is null ? null : await ResolveAsync($"group:{subCategoryId}", DefaultCategory, cancellationToken,
            find: async name => (await _groupService.GetPagedAsync(new ProductGroupListQuery { SubCategoryId = subCategoryId, Search = name, Page = 1, PageSize = 50 }, cancellationToken))
                .Items.FirstOrDefault(g => Same(g.GroupName, name))?.Id,
            create: async name => (await _groupService.CreateAsync(
                new CreateProductGroupRequest { GroupCode = Code($"{categoryName} {name} grp", 30), GroupName = name, SubCategoryId = subCategoryId.Value }, actorId, cancellationToken)).Value?.Id);

        var manufacturerId = await ResolveAsync("manufacturer", Clean(row.Manufacturer) ?? DefaultManufacturer, cancellationToken,
            find: async name => (await _manufacturerService.GetPagedAsync(new ManufacturerListQuery { Search = name, Page = 1, PageSize = 50 }, cancellationToken))
                .Items.FirstOrDefault(m => Same(m.ManufacturerName, name))?.Id,
            create: async name => (await _manufacturerService.CreateAsync(new CreateManufacturerRequest { ManufacturerCode = Code(name, 30), ManufacturerName = name }, actorId, cancellationToken)).Value?.Id);

        var brandId = await ResolveAsync("brand", Clean(row.Brand) ?? DefaultBrand, cancellationToken,
            find: async name => (await _brandService.GetPagedAsync(new BrandListQuery { Search = name, Page = 1, PageSize = 50 }, cancellationToken))
                .Items.FirstOrDefault(b => Same(b.BrandName, name))?.Id,
            create: async name => (await _brandService.CreateAsync(new CreateBrandRequest { BrandCode = Code(name, 30), BrandName = name }, actorId, cancellationToken)).Value?.Id);

        var uomId = await ResolveAsync("uom", Clean(row.Unit) ?? DefaultUnit, cancellationToken,
            find: async name => (await _uomService.GetPagedAsync(new UnitOfMeasureListQuery { Search = name, Page = 1, PageSize = 50 }, cancellationToken))
                .Items.FirstOrDefault(u => Same(u.UomName, name) || Same(u.UomCode, name))?.Id,
            create: async name => (await _uomService.CreateAsync(new CreateUnitOfMeasureRequest { UomCode = Code(name, 20), UomName = name, IsBaseUnit = true }, actorId, cancellationToken)).Value?.Id);

        if (categoryId is null || subCategoryId is null || groupId is null || manufacturerId is null || brandId is null || uomId is null)
        {
            return Result<ProductResponse>.Failure(PharmacyErrorCodes.InvalidProduct, "Could not set up this product's category, manufacturer, brand or unit.");
        }

        var dosageForm = ParseEnum<DosageForm>(row.DosageForm);
        if (dosageForm is null && Clean(row.DosageForm) is { } unknownForm)
        {
            notes.Add($"Dosage form '{unknownForm}' is not one the system knows, so it was left blank.");
        }

        var schedule = ParseSchedule(row.Schedule);
        if (schedule is null && Clean(row.Schedule) is { } unknownSchedule)
        {
            notes.Add($"Schedule '{unknownSchedule}' is not one the system knows, so the product was saved as unscheduled.");
        }

        var name = row.ProductName.Trim();
        var sku = string.IsNullOrWhiteSpace(row.Sku) ? Code(name, 50) : row.Sku.Trim();
        var mrp = row.Mrp ?? 0m;

        return await _productService.CreateAsync(
            new CreateProductRequest
            {
                Sku = sku,
                ProductCode = sku,
                ProductName = name,
                GenericName = Clean(row.GenericName),
                BrandId = brandId.Value,
                ManufacturerId = manufacturerId.Value,
                CategoryId = categoryId.Value,
                SubCategoryId = subCategoryId.Value,
                GroupId = groupId.Value,
                UomId = uomId.Value,
                BaseUomId = uomId.Value,
                IsBatchTracked = true,
                ReorderLevel = row.ReorderLevel ?? 0m,
                Mrp = mrp,
                CostPrice = row.PurchaseRate ?? 0m,
                SellingPrice = row.SellingRate ?? mrp,
                HsnCode = Clean(row.HsnCode),
                DosageForm = dosageForm,
                Strength = Clean(row.Strength),
                UnitsPerPack = row.UnitsPerPack ?? 1m,
                Schedule = schedule ?? DrugSchedule.None,
                RackLocation = Clean(row.RackLocation),
                GstRatePercent = row.GstRatePercent,
            },
            actorId,
            cancellationToken);
    }

    private async Task<Guid?> ResolveAsync(string kind, string name, CancellationToken cancellationToken, Func<string, Task<Guid?>> find, Func<string, Task<Guid?>> create)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var key = $"{kind}|{name}";
        if (_masterIds.TryGetValue(key, out var known))
        {
            return known;
        }

        var id = await find(name) ?? await create(name);
        if (id is { } resolved)
        {
            _masterIds[key] = resolved;
        }

        return id;
    }

    private static ImportRowResult Error(int rowNumber, ImportProductRow row, string message) => new()
    {
        RowNumber = rowNumber,
        ProductName = row.ProductName?.Trim() ?? string.Empty,
        Status = ImportRowStatus.Error,
        Message = message,
    };

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static bool Same(string? a, string b) => string.Equals(a?.Trim(), b, StringComparison.OrdinalIgnoreCase);

    internal static TEnum? ParseEnum<TEnum>(string? value)
        where TEnum : struct, Enum
    {
        var cleaned = Clean(value)?.Replace(" ", string.Empty).Replace("-", string.Empty);
        return cleaned is not null && !int.TryParse(cleaned, out _) && Enum.TryParse<TEnum>(cleaned, ignoreCase: true, out var parsed) ? parsed : null;
    }

    /// <summary>Spreadsheets write schedules every which way: "H1", "Sch H1", "Schedule H", "NDPS".</summary>
    internal static DrugSchedule? ParseSchedule(string? value)
    {
        var cleaned = Clean(value)?.ToUpperInvariant().Replace("SCHEDULE", string.Empty).Replace("SCH", string.Empty).Replace(".", string.Empty).Replace(" ", string.Empty).Replace("-", string.Empty);
        return cleaned switch
        {
            null => null,
            "" or "NONE" or "OTC" or "NA" or "NIL" => DrugSchedule.None,
            "NDPS" or "NARCOTIC" or "NARCOTICS" => DrugSchedule.Narcotic,
            _ => ParseEnum<DrugSchedule>(cleaned),
        };
    }

    /// <summary>
    /// A code for something that only came with a name: its letters and digits, cut to fit,
    /// with a short tag worked out from the full name on the end — so two names that start
    /// the same get different codes, and the same name always gets the same one.
    /// </summary>
    internal static string Code(string name, int maxLength)
    {
        var slug = new StringBuilder();
        foreach (var ch in name.ToUpperInvariant())
        {
            if (char.IsAsciiLetterOrDigit(ch))
            {
                slug.Append(ch);
            }
        }

        // FNV-1a over the trimmed, upper-cased name: stable across runs and machines, which
        // string.GetHashCode is not.
        var hash = 2166136261u;
        foreach (var ch in name.Trim().ToUpperInvariant())
        {
            hash = (hash ^ ch) * 16777619u;
        }

        var tag = (hash & 0xFFFFF).ToString("X5");
        var head = slug.Length > maxLength - 6 ? slug.ToString(0, maxLength - 6) : slug.ToString();
        return head.Length == 0 ? $"X-{tag}" : $"{head}-{tag}";
    }
}
