using HMS.Modules.Pharmacy.Application.Abstractions;
using HMS.Modules.Pharmacy.Contracts;
using HMS.Modules.Pharmacy.Domain;
using HMS.Modules.Products.Application;
using HMS.Modules.Products.Contracts;
using HMS.Shared.Kernel;

namespace HMS.Modules.Pharmacy.Application;

/// <summary>One batch's quantity in one store.</summary>
internal sealed record StoreStockLine(Guid StoreId, Guid ProductId, Guid ProductBatchId, decimal QuantityOnHand);

/// <summary>Adjustments, transfers and stock counts, and the reads the control screens need.
/// Saved through IPharmacyUnitOfWork.</summary>
internal interface IPharmacyStockControlRepository
{
    Task AddAdjustmentAsync(PharmacyStockAdjustment adjustment, CancellationToken cancellationToken);

    Task<PharmacyStockAdjustment?> GetAdjustmentAsync(Guid id, CancellationToken cancellationToken);

    Task<(IReadOnlyList<(PharmacyStockAdjustment Adjustment, int LineCount, decimal? Value)> Items, int TotalCount)> GetAdjustmentsPagedAsync(StockAdjustmentListQuery query, CancellationToken cancellationToken);

    Task AddTransferAsync(PharmacyStockTransfer transfer, CancellationToken cancellationToken);

    Task<PharmacyStockTransfer?> GetTransferAsync(Guid id, CancellationToken cancellationToken);

    Task<(IReadOnlyList<(PharmacyStockTransfer Transfer, int LineCount)> Items, int TotalCount)> GetTransfersPagedAsync(StockTransferListQuery query, CancellationToken cancellationToken);

    Task AddCountAsync(PharmacyStockCount count, CancellationToken cancellationToken);

    /// <summary>The count with its lines, tracked.</summary>
    Task<PharmacyStockCount?> GetCountAsync(Guid id, CancellationToken cancellationToken);

    Task<bool> HasOpenCountAsync(Guid storeId, CancellationToken cancellationToken);

    Task<(IReadOnlyList<(PharmacyStockCount Count, int LineCount, int CountedLines)> Items, int TotalCount)> GetCountsPagedAsync(StockCountListQuery query, CancellationToken cancellationToken);

    /// <summary>Every batch with stock, in one store or in all of them.</summary>
    Task<IReadOnlyList<StoreStockLine>> GetStockLinesAsync(Guid? storeId, CancellationToken cancellationToken);

    /// <summary>The supplier each batch last came in from, for batches that came in on a purchase.</summary>
    Task<IReadOnlyDictionary<Guid, (Guid SupplierId, string SupplierName)>> GetBatchSuppliersAsync(IReadOnlyCollection<Guid> productBatchIds, CancellationToken cancellationToken);

    Task<IReadOnlyList<ScheduleRegisterEntry>> GetScheduleRegisterAsync(DateOnly fromDate, DateOnly toDate, IReadOnlyCollection<DrugSchedule> schedules, Guid? storeId, CancellationToken cancellationToken);
}

public interface IStockControlService
{
    Task<Result<StockAdjustmentResponse>> CreateAdjustmentAsync(CreateStockAdjustmentRequest request, Guid? actorId, bool includeCost, CancellationToken cancellationToken);

    Task<Result<StockAdjustmentResponse>> GetAdjustmentAsync(Guid id, bool includeCost, CancellationToken cancellationToken);

    Task<PagedResult<StockAdjustmentResponse>> GetAdjustmentsPagedAsync(StockAdjustmentListQuery query, bool includeCost, CancellationToken cancellationToken);

    Task<Result<StockTransferResponse>> CreateTransferAsync(CreateStockTransferRequest request, Guid? actorId, CancellationToken cancellationToken);

    Task<Result<StockTransferResponse>> GetTransferAsync(Guid id, CancellationToken cancellationToken);

    Task<PagedResult<StockTransferResponse>> GetTransfersPagedAsync(StockTransferListQuery query, CancellationToken cancellationToken);

    Task<Result<StockCountResponse>> StartCountAsync(StartStockCountRequest request, Guid? actorId, CancellationToken cancellationToken);

    Task<Result<StockCountResponse>> SaveCountAsync(Guid id, SaveStockCountRequest request, Guid? actorId, CancellationToken cancellationToken);

    /// <summary>Turns every difference between what was counted and what the books hold
    /// into one stock adjustment, and closes the count.</summary>
    Task<Result<StockCountResponse>> PostCountAsync(Guid id, Guid? actorId, CancellationToken cancellationToken);

    Task<Result<StockCountResponse>> CancelCountAsync(Guid id, Guid? actorId, CancellationToken cancellationToken);

    Task<Result<StockCountResponse>> GetCountAsync(Guid id, CancellationToken cancellationToken);

    Task<PagedResult<StockCountResponse>> GetCountsPagedAsync(StockCountListQuery query, CancellationToken cancellationToken);

    /// <summary>Batches on the shelf that have expired, or will within <paramref name="days"/>
    /// (the pharmacy's own alert window when omitted), soonest first.</summary>
    Task<IReadOnlyList<ExpiringBatchResponse>> GetExpiringAsync(Guid? storeId, int? days, bool includeCost, CancellationToken cancellationToken);

    Task<IReadOnlyList<ScheduleRegisterEntry>> GetScheduleRegisterAsync(DateOnly fromDate, DateOnly toDate, IReadOnlyCollection<DrugSchedule> schedules, Guid? storeId, CancellationToken cancellationToken);
}

/// <summary>
/// Everything that keeps the books honest: corrections with a reason, moves between stores,
/// physical counts, the watch on expiry, and the registers the drug inspector asks for.
/// </summary>
internal class StockControlService : IStockControlService
{
    private readonly IPharmacyStockControlRepository _repository;
    private readonly IPharmacyStoreRepository _storeRepository;
    private readonly IPharmacySettingsRepository _settingsRepository;
    private readonly IPharmacyUnitOfWork _unitOfWork;
    private readonly IStockPoster _stockPoster;
    private readonly IProductService _productService;
    private readonly IProductBatchService _productBatchService;

    public StockControlService(
        IPharmacyStockControlRepository repository,
        IPharmacyStoreRepository storeRepository,
        IPharmacySettingsRepository settingsRepository,
        IPharmacyUnitOfWork unitOfWork,
        IStockPoster stockPoster,
        IProductService productService,
        IProductBatchService productBatchService)
    {
        _repository = repository;
        _storeRepository = storeRepository;
        _settingsRepository = settingsRepository;
        _unitOfWork = unitOfWork;
        _stockPoster = stockPoster;
        _productService = productService;
        _productBatchService = productBatchService;
    }

    // ---- Adjustments -------------------------------------------------------------------

    public async Task<Result<StockAdjustmentResponse>> CreateAdjustmentAsync(CreateStockAdjustmentRequest request, Guid? actorId, bool includeCost, CancellationToken cancellationToken)
    {
        var store = await _storeRepository.ResolveAsync(request.StoreId, cancellationToken);
        if (store is null)
        {
            return Result<StockAdjustmentResponse>.Failure(PharmacyErrorCodes.InvalidStore, "The store was not found or is inactive.");
        }

        var lines = request.Lines.Select(l => (l.ProductId, l.ProductBatchId, l.Quantity)).ToList();
        var adjustmentNo = await _unitOfWork.NextNumberAsync(DocumentSeries.Adjustment, cancellationToken);
        var result = await PostAdjustmentAsync(adjustmentNo, store, request.Reason, stockCountId: null, request.Remarks, lines, actorId, onPosted: null, cancellationToken);

        return result.IsSuccess
            ? Result<StockAdjustmentResponse>.Success(ToResponse(result.Value!, store.Name, includeCost))
            : Result<StockAdjustmentResponse>.Failure(result.ErrorCode!, result.Error!);
    }

    /// <summary>Builds an adjustment and posts its movements in one unit of work.
    /// <paramref name="onPosted"/> runs inside it, for a caller with more to save alongside.</summary>
    private async Task<Result<PharmacyStockAdjustment>> PostAdjustmentAsync(
        string adjustmentNo,
        PharmacyStore store,
        AdjustmentReason reason,
        Guid? stockCountId,
        string? remarks,
        IReadOnlyList<(Guid ProductId, Guid ProductBatchId, decimal Quantity)> lines,
        Guid? actorId,
        Func<PharmacyStockAdjustment, Task<Result<bool>>>? onPosted,
        CancellationToken cancellationToken)
    {
        var products = await _productService.GetLookupAsync(lines.Select(l => l.ProductId), cancellationToken);
        var batches = await _productBatchService.GetLookupAsync(lines.Select(l => l.ProductBatchId), cancellationToken);

        for (var i = 0; i < lines.Count; i++)
        {
            if (!products.ContainsKey(lines[i].ProductId))
            {
                return Result<PharmacyStockAdjustment>.Failure(PharmacyErrorCodes.InvalidProduct, $"Line {i + 1}: the product was not found.");
            }

            if (!batches.TryGetValue(lines[i].ProductBatchId, out var batch) || batch.ProductId != lines[i].ProductId)
            {
                return Result<PharmacyStockAdjustment>.Failure(PharmacyErrorCodes.InvalidBatch, $"Line {i + 1}: the batch was not found for that product.");
            }
        }

        return await _unitOfWork.ExecuteAsync(
            async () =>
            {
                var adjustment = PharmacyStockAdjustment.Create(adjustmentNo, store.Id, PharmacyClock.Today(), reason, stockCountId, remarks, actorId);

                foreach (var (productId, batchId, quantity) in lines)
                {
                    var product = products[productId];
                    var batch = batches[batchId];
                    var unitCost = PharmacyPricing.UnitCost(product, batch);
                    adjustment.AddLine(product.Id, product.ProductName, batch.Id, batch.BatchNo, batch.ExpiryDate, quantity, unitCost);

                    var posted = await _stockPoster.PostAsync(
                        new StockMovement(
                            quantity > 0 ? TransactionType.AdjustmentIn : TransactionType.AdjustmentOut,
                            store.Id, product.Id, batch.Id, Math.Abs(quantity),
                            UnitCost: unitCost,
                            SourceType: StockSourceType.Adjustment, SourceId: adjustment.Id, SourceNo: adjustmentNo,
                            Remarks: string.IsNullOrWhiteSpace(remarks) ? reason.ToString() : $"{reason}: {remarks.Trim()}",
                            Label: $"{product.ProductName} (batch {batch.BatchNo})"),
                        actorId,
                        cancellationToken);

                    if (!posted.IsSuccess)
                    {
                        return Result<PharmacyStockAdjustment>.Failure(posted.ErrorCode!, posted.Error!);
                    }
                }

                await _repository.AddAdjustmentAsync(adjustment, cancellationToken);

                if (onPosted is not null)
                {
                    var extra = await onPosted(adjustment);
                    if (!extra.IsSuccess)
                    {
                        return Result<PharmacyStockAdjustment>.Failure(extra.ErrorCode!, extra.Error!);
                    }
                }

                return Result<PharmacyStockAdjustment>.Success(adjustment);
            },
            cancellationToken);
    }

    public async Task<Result<StockAdjustmentResponse>> GetAdjustmentAsync(Guid id, bool includeCost, CancellationToken cancellationToken)
    {
        var adjustment = await _repository.GetAdjustmentAsync(id, cancellationToken);
        if (adjustment is null)
        {
            return Result<StockAdjustmentResponse>.Failure(PharmacyErrorCodes.NotFound, "The adjustment was not found.");
        }

        var store = await _storeRepository.GetByIdAsync(adjustment.StoreId, cancellationToken);
        return Result<StockAdjustmentResponse>.Success(ToResponse(adjustment, store?.Name ?? string.Empty, includeCost));
    }

    public async Task<PagedResult<StockAdjustmentResponse>> GetAdjustmentsPagedAsync(StockAdjustmentListQuery query, bool includeCost, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _repository.GetAdjustmentsPagedAsync(query, cancellationToken);
        var storeNames = await StoreNamesAsync(cancellationToken);

        var responses = items
            .Select(i => new StockAdjustmentResponse
            {
                Id = i.Adjustment.Id,
                AdjustmentNo = i.Adjustment.AdjustmentNo,
                StoreId = i.Adjustment.StoreId,
                StoreName = storeNames.GetValueOrDefault(i.Adjustment.StoreId) ?? string.Empty,
                AdjustmentDate = i.Adjustment.AdjustmentDate,
                Reason = i.Adjustment.Reason,
                StockCountId = i.Adjustment.StockCountId,
                Remarks = i.Adjustment.Remarks,
                LineCount = i.LineCount,
                TotalValue = includeCost && i.Value is { } value ? GstMath.Round2(value) : null,
                CreatedAt = i.Adjustment.CreatedAt,
            })
            .ToList();

        return new PagedResult<StockAdjustmentResponse>(responses, query.Page, query.PageSize, totalCount);
    }

    // ---- Transfers ---------------------------------------------------------------------

    public async Task<Result<StockTransferResponse>> CreateTransferAsync(CreateStockTransferRequest request, Guid? actorId, CancellationToken cancellationToken)
    {
        var from = await _storeRepository.GetByIdAsync(request.FromStoreId, cancellationToken);
        var to = await _storeRepository.GetByIdAsync(request.ToStoreId, cancellationToken);
        if (from is not { IsActive: true } || to is not { IsActive: true })
        {
            return Result<StockTransferResponse>.Failure(PharmacyErrorCodes.InvalidStore, "Choose two active stores.");
        }

        if (from.Id == to.Id)
        {
            return Result<StockTransferResponse>.Failure(PharmacyErrorCodes.InvalidStore, "The two stores must be different.");
        }

        var products = await _productService.GetLookupAsync(request.Lines.Select(l => l.ProductId), cancellationToken);
        var batches = await _productBatchService.GetLookupAsync(request.Lines.Select(l => l.ProductBatchId), cancellationToken);

        for (var i = 0; i < request.Lines.Count; i++)
        {
            var line = request.Lines[i];
            if (!products.ContainsKey(line.ProductId) || !batches.TryGetValue(line.ProductBatchId, out var batch) || batch.ProductId != line.ProductId)
            {
                return Result<StockTransferResponse>.Failure(PharmacyErrorCodes.InvalidBatch, $"Line {i + 1}: the product or batch was not found.");
            }
        }

        var transferNo = await _unitOfWork.NextNumberAsync(DocumentSeries.Transfer, cancellationToken);

        var result = await _unitOfWork.ExecuteAsync(
            async () =>
            {
                var transfer = PharmacyStockTransfer.Create(transferNo, from.Id, to.Id, PharmacyClock.Today(), request.Remarks, actorId);

                foreach (var line in request.Lines)
                {
                    var product = products[line.ProductId];
                    var batch = batches[line.ProductBatchId];
                    var unitCost = PharmacyPricing.UnitCost(product, batch);
                    transfer.AddLine(product.Id, product.ProductName, batch.Id, batch.BatchNo, batch.ExpiryDate, line.Quantity);

                    // Out of one store and into the other in the same save: the stock is never
                    // in neither, and never in both.
                    var sent = await _stockPoster.PostAsync(
                        new StockMovement(
                            TransactionType.TransferOut, from.Id, product.Id, batch.Id, line.Quantity, UnitCost: unitCost,
                            SourceType: StockSourceType.Transfer, SourceId: transfer.Id, SourceNo: transferNo,
                            Remarks: $"To {to.Name}", Label: $"{product.ProductName} (batch {batch.BatchNo})"),
                        actorId,
                        cancellationToken);

                    if (!sent.IsSuccess)
                    {
                        return Result<PharmacyStockTransfer>.Failure(sent.ErrorCode!, sent.Error!);
                    }

                    await _stockPoster.PostAsync(
                        new StockMovement(
                            TransactionType.TransferIn, to.Id, product.Id, batch.Id, line.Quantity, UnitCost: unitCost,
                            SourceType: StockSourceType.Transfer, SourceId: transfer.Id, SourceNo: transferNo,
                            Remarks: $"From {from.Name}"),
                        actorId,
                        cancellationToken);
                }

                await _repository.AddTransferAsync(transfer, cancellationToken);
                return Result<PharmacyStockTransfer>.Success(transfer);
            },
            cancellationToken);

        return result.IsSuccess
            ? Result<StockTransferResponse>.Success(ToResponse(result.Value!, from.Name, to.Name, result.Value!.Lines.Count, includeLines: true))
            : Result<StockTransferResponse>.Failure(result.ErrorCode!, result.Error!);
    }

    public async Task<Result<StockTransferResponse>> GetTransferAsync(Guid id, CancellationToken cancellationToken)
    {
        var transfer = await _repository.GetTransferAsync(id, cancellationToken);
        if (transfer is null)
        {
            return Result<StockTransferResponse>.Failure(PharmacyErrorCodes.NotFound, "The transfer was not found.");
        }

        var names = await StoreNamesAsync(cancellationToken);
        return Result<StockTransferResponse>.Success(ToResponse(
            transfer, names.GetValueOrDefault(transfer.FromStoreId) ?? string.Empty, names.GetValueOrDefault(transfer.ToStoreId) ?? string.Empty, transfer.Lines.Count, includeLines: true));
    }

    public async Task<PagedResult<StockTransferResponse>> GetTransfersPagedAsync(StockTransferListQuery query, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _repository.GetTransfersPagedAsync(query, cancellationToken);
        var names = await StoreNamesAsync(cancellationToken);

        var responses = items
            .Select(i => ToResponse(
                i.Transfer, names.GetValueOrDefault(i.Transfer.FromStoreId) ?? string.Empty, names.GetValueOrDefault(i.Transfer.ToStoreId) ?? string.Empty, i.LineCount, includeLines: false))
            .ToList();

        return new PagedResult<StockTransferResponse>(responses, query.Page, query.PageSize, totalCount);
    }

    // ---- Stock count -------------------------------------------------------------------

    public async Task<Result<StockCountResponse>> StartCountAsync(StartStockCountRequest request, Guid? actorId, CancellationToken cancellationToken)
    {
        var store = await _storeRepository.ResolveAsync(request.StoreId, cancellationToken);
        if (store is null)
        {
            return Result<StockCountResponse>.Failure(PharmacyErrorCodes.InvalidStore, "The store was not found or is inactive.");
        }

        if (await _repository.HasOpenCountAsync(store.Id, cancellationToken))
        {
            return Result<StockCountResponse>.Failure(PharmacyErrorCodes.InvalidState, $"{store.Name} already has a count open. Post or cancel it first.");
        }

        var stock = await _repository.GetStockLinesAsync(store.Id, cancellationToken);
        if (stock.Count == 0)
        {
            return Result<StockCountResponse>.Failure(PharmacyErrorCodes.InvalidState, $"{store.Name} holds no stock to count.");
        }

        var products = await _productService.GetLookupAsync(stock.Select(s => s.ProductId), cancellationToken);
        var batches = await _productBatchService.GetLookupAsync(stock.Select(s => s.ProductBatchId), cancellationToken);

        var countNo = await _unitOfWork.NextNumberAsync(DocumentSeries.StockCount, cancellationToken);
        var count = PharmacyStockCount.Start(countNo, store.Id, request.Remarks, actorId);

        // Listed the way the shelves are walked: by rack, then by name.
        foreach (var line in stock
            .Where(s => products.ContainsKey(s.ProductId) && batches.ContainsKey(s.ProductBatchId))
            .OrderBy(s => products[s.ProductId].RackLocation ?? "￿", StringComparer.OrdinalIgnoreCase)
            .ThenBy(s => products[s.ProductId].ProductName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(s => batches[s.ProductBatchId].ExpiryDate))
        {
            var batch = batches[line.ProductBatchId];
            count.AddLine(line.ProductId, products[line.ProductId].ProductName, batch.Id, batch.BatchNo, batch.ExpiryDate, line.QuantityOnHand);
        }

        await _repository.AddCountAsync(count, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<StockCountResponse>.Success(await ToResponseAsync(count, cancellationToken));
    }

    public async Task<Result<StockCountResponse>> SaveCountAsync(Guid id, SaveStockCountRequest request, Guid? actorId, CancellationToken cancellationToken)
    {
        var count = await _repository.GetCountAsync(id, cancellationToken);
        if (count is null)
        {
            return Result<StockCountResponse>.Failure(PharmacyErrorCodes.NotFound, "The stock count was not found.");
        }

        try
        {
            foreach (var entry in request.Entries)
            {
                count.RecordCount(entry.LineId, entry.CountedQuantity, actorId);
            }
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentOutOfRangeException)
        {
            return Result<StockCountResponse>.Failure(PharmacyErrorCodes.InvalidState, ex.Message);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<StockCountResponse>.Success(await ToResponseAsync(count, cancellationToken));
    }

    public async Task<Result<StockCountResponse>> PostCountAsync(Guid id, Guid? actorId, CancellationToken cancellationToken)
    {
        var existing = await _repository.GetCountAsync(id, cancellationToken);
        if (existing is null)
        {
            return Result<StockCountResponse>.Failure(PharmacyErrorCodes.NotFound, "The stock count was not found.");
        }

        if (existing.Status != StockCountStatus.Open)
        {
            return Result<StockCountResponse>.Failure(PharmacyErrorCodes.InvalidState, $"Count {existing.CountNo} is already {existing.Status.ToString().ToLowerInvariant()}.");
        }

        if (existing.Lines.All(l => l.CountedQuantity is null))
        {
            return Result<StockCountResponse>.Failure(PharmacyErrorCodes.InvalidState, "Nothing has been counted yet.");
        }

        var store = await _storeRepository.GetByIdAsync(existing.StoreId, cancellationToken);
        if (store is null)
        {
            return Result<StockCountResponse>.Failure(PharmacyErrorCodes.InvalidStore, "The store was not found.");
        }

        // A line's difference is taken against what the books hold now, not what they held
        // when the count started: stock sold since then has already left the books, and
        // adjusting against the old figure would take it off a second time. Lines never
        // counted are left alone.
        var current = (await _repository.GetStockLinesAsync(store.Id, cancellationToken)).ToDictionary(s => s.ProductBatchId, s => s.QuantityOnHand);
        var variances = existing.Lines
            .Where(l => l.CountedQuantity is not null)
            .Select(l => (l.ProductId, l.ProductBatchId, Quantity: l.CountedQuantity!.Value - current.GetValueOrDefault(l.ProductBatchId)))
            .Where(v => v.Quantity != 0)
            .ToList();

        if (variances.Count == 0)
        {
            existing.MarkPosted(adjustmentId: null, actorId);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return Result<StockCountResponse>.Success(await ToResponseAsync(existing, cancellationToken));
        }

        var adjustmentNo = await _unitOfWork.NextNumberAsync(DocumentSeries.Adjustment, cancellationToken);
        var posted = await PostAdjustmentAsync(
            adjustmentNo, store, AdjustmentReason.StockCount, existing.Id, $"Stock count {existing.CountNo}", variances, actorId,
            onPosted: async adjustment =>
            {
                // Read again inside the unit of work: on a retry the tracked count is gone.
                var count = await _repository.GetCountAsync(id, cancellationToken);
                if (count is not { Status: StockCountStatus.Open })
                {
                    return Result<bool>.Failure(PharmacyErrorCodes.InvalidState, "The stock count is no longer open.");
                }

                count.MarkPosted(adjustment.Id, actorId);
                return Result<bool>.Success(true);
            },
            cancellationToken);

        if (!posted.IsSuccess)
        {
            return Result<StockCountResponse>.Failure(posted.ErrorCode!, posted.Error!);
        }

        return await GetCountAsync(id, cancellationToken);
    }

    public async Task<Result<StockCountResponse>> CancelCountAsync(Guid id, Guid? actorId, CancellationToken cancellationToken)
    {
        var count = await _repository.GetCountAsync(id, cancellationToken);
        if (count is null)
        {
            return Result<StockCountResponse>.Failure(PharmacyErrorCodes.NotFound, "The stock count was not found.");
        }

        if (count.Status != StockCountStatus.Open)
        {
            return Result<StockCountResponse>.Failure(PharmacyErrorCodes.InvalidState, $"Count {count.CountNo} is already {count.Status.ToString().ToLowerInvariant()}.");
        }

        count.Cancel(actorId);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<StockCountResponse>.Success(await ToResponseAsync(count, cancellationToken));
    }

    public async Task<Result<StockCountResponse>> GetCountAsync(Guid id, CancellationToken cancellationToken)
    {
        var count = await _repository.GetCountAsync(id, cancellationToken);
        return count is null
            ? Result<StockCountResponse>.Failure(PharmacyErrorCodes.NotFound, "The stock count was not found.")
            : Result<StockCountResponse>.Success(await ToResponseAsync(count, cancellationToken));
    }

    public async Task<PagedResult<StockCountResponse>> GetCountsPagedAsync(StockCountListQuery query, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _repository.GetCountsPagedAsync(query, cancellationToken);
        var names = await StoreNamesAsync(cancellationToken);

        var responses = items
            .Select(i => new StockCountResponse
            {
                Id = i.Count.Id,
                CountNo = i.Count.CountNo,
                StoreId = i.Count.StoreId,
                StoreName = names.GetValueOrDefault(i.Count.StoreId) ?? string.Empty,
                StartedAt = i.Count.StartedAt,
                Status = i.Count.Status,
                PostedAt = i.Count.PostedAt,
                AdjustmentId = i.Count.AdjustmentId,
                Remarks = i.Count.Remarks,
                LineCount = i.LineCount,
                CountedLines = i.CountedLines,
            })
            .ToList();

        return new PagedResult<StockCountResponse>(responses, query.Page, query.PageSize, totalCount);
    }

    private async Task<StockCountResponse> ToResponseAsync(PharmacyStockCount count, CancellationToken cancellationToken)
    {
        var store = await _storeRepository.GetByIdAsync(count.StoreId, cancellationToken);
        var products = await _productService.GetLookupAsync(count.Lines.Select(l => l.ProductId), cancellationToken);

        // Once posted, the books already include the adjustment, so "current" would always
        // equal "counted"; the snapshot is what a posted count is read against.
        var current = count.Status == StockCountStatus.Open
            ? (await _repository.GetStockLinesAsync(count.StoreId, cancellationToken)).ToDictionary(s => s.ProductBatchId, s => s.QuantityOnHand)
            : null;

        var lines = count.Lines
            .Select(l =>
            {
                var book = current is null ? l.BookQuantity : current.GetValueOrDefault(l.ProductBatchId);
                return new StockCountLineResponse
                {
                    Id = l.Id,
                    ProductId = l.ProductId,
                    ProductName = l.ProductName,
                    RackLocation = products.GetValueOrDefault(l.ProductId)?.RackLocation,
                    ProductBatchId = l.ProductBatchId,
                    BatchNo = l.BatchNo,
                    ExpiryDate = l.ExpiryDate,
                    BookQuantity = l.BookQuantity,
                    CurrentQuantity = book,
                    CountedQuantity = l.CountedQuantity,
                    Variance = l.CountedQuantity - book,
                };
            })
            .OrderBy(l => l.RackLocation ?? "￿", StringComparer.OrdinalIgnoreCase)
            .ThenBy(l => l.ProductName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(l => l.ExpiryDate)
            .ToList();

        return new StockCountResponse
        {
            Id = count.Id,
            CountNo = count.CountNo,
            StoreId = count.StoreId,
            StoreName = store?.Name ?? string.Empty,
            StartedAt = count.StartedAt,
            Status = count.Status,
            PostedAt = count.PostedAt,
            AdjustmentId = count.AdjustmentId,
            Remarks = count.Remarks,
            LineCount = lines.Count,
            CountedLines = lines.Count(l => l.CountedQuantity is not null),
            VarianceLines = lines.Count(l => l.Variance is not null and not 0),
            Lines = lines,
        };
    }

    // ---- Expiry and registers ----------------------------------------------------------

    public async Task<IReadOnlyList<ExpiringBatchResponse>> GetExpiringAsync(Guid? storeId, int? days, bool includeCost, CancellationToken cancellationToken)
    {
        var window = days ?? await _settingsRepository.ExpiryAlertDaysAsync(cancellationToken);
        var today = PharmacyClock.Today();
        var cutoff = today.AddDays(Math.Clamp(window, 0, 730));

        var stock = await _repository.GetStockLinesAsync(storeId, cancellationToken);
        var batches = await _productBatchService.GetLookupAsync(stock.Select(s => s.ProductBatchId), cancellationToken);

        var expiring = stock
            .Where(s => batches.TryGetValue(s.ProductBatchId, out var batch) && batch.ExpiryDate <= cutoff)
            .ToList();
        if (expiring.Count == 0)
        {
            return [];
        }

        var products = await _productService.GetLookupAsync(expiring.Select(s => s.ProductId), cancellationToken);
        var suppliers = await _repository.GetBatchSuppliersAsync(expiring.Select(s => s.ProductBatchId).ToList(), cancellationToken);
        var storeNames = await StoreNamesAsync(cancellationToken);

        return expiring
            .Where(s => products.ContainsKey(s.ProductId))
            .Select(s =>
            {
                var product = products[s.ProductId];
                var batch = batches[s.ProductBatchId];
                var unitCost = PharmacyPricing.UnitCost(product, batch);
                var supplier = suppliers.TryGetValue(batch.Id, out var found) ? found : ((Guid SupplierId, string SupplierName)?)null;

                return new ExpiringBatchResponse
                {
                    StoreId = s.StoreId,
                    StoreName = storeNames.GetValueOrDefault(s.StoreId) ?? string.Empty,
                    ProductId = product.Id,
                    ProductName = product.ProductName,
                    RackLocation = product.RackLocation,
                    ProductBatchId = batch.Id,
                    BatchNo = batch.BatchNo,
                    ExpiryDate = batch.ExpiryDate,
                    DaysLeft = batch.ExpiryDate.DayNumber - today.DayNumber,
                    QuantityOnHand = s.QuantityOnHand,
                    Value = includeCost && unitCost is { } cost ? GstMath.Round2(cost * s.QuantityOnHand) : null,
                    SupplierId = supplier?.SupplierId,
                    SupplierName = supplier?.SupplierName,
                };
            })
            .OrderBy(e => e.ExpiryDate).ThenBy(e => e.ProductName, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public Task<IReadOnlyList<ScheduleRegisterEntry>> GetScheduleRegisterAsync(DateOnly fromDate, DateOnly toDate, IReadOnlyCollection<DrugSchedule> schedules, Guid? storeId, CancellationToken cancellationToken)
        => _repository.GetScheduleRegisterAsync(fromDate, toDate, schedules, storeId, cancellationToken);

    // ---- Mapping -----------------------------------------------------------------------

    private async Task<Dictionary<Guid, string>> StoreNamesAsync(CancellationToken cancellationToken)
        => (await _storeRepository.GetAllAsync(cancellationToken)).ToDictionary(s => s.Id, s => s.Name);

    private static StockAdjustmentResponse ToResponse(PharmacyStockAdjustment adjustment, string storeName, bool includeCost)
    {
        var lines = adjustment.Lines
            .Select(l => new StockAdjustmentLineResponse
            {
                Id = l.Id,
                LineNo = l.LineNo,
                ProductId = l.ProductId,
                ProductName = l.ProductName,
                ProductBatchId = l.ProductBatchId,
                BatchNo = l.BatchNo,
                ExpiryDate = l.ExpiryDate,
                Quantity = l.Quantity,
                Value = includeCost && l.UnitCost is { } cost ? GstMath.Round2(cost * l.Quantity) : null,
            })
            .ToList();

        return new StockAdjustmentResponse
        {
            Id = adjustment.Id,
            AdjustmentNo = adjustment.AdjustmentNo,
            StoreId = adjustment.StoreId,
            StoreName = storeName,
            AdjustmentDate = adjustment.AdjustmentDate,
            Reason = adjustment.Reason,
            StockCountId = adjustment.StockCountId,
            Remarks = adjustment.Remarks,
            LineCount = lines.Count,
            TotalValue = includeCost && lines.Any(l => l.Value is not null) ? lines.Sum(l => l.Value ?? 0m) : null,
            CreatedAt = adjustment.CreatedAt,
            Lines = lines,
        };
    }

    private static StockTransferResponse ToResponse(PharmacyStockTransfer transfer, string fromName, string toName, int lineCount, bool includeLines) => new()
    {
        Id = transfer.Id,
        TransferNo = transfer.TransferNo,
        FromStoreId = transfer.FromStoreId,
        FromStoreName = fromName,
        ToStoreId = transfer.ToStoreId,
        ToStoreName = toName,
        TransferDate = transfer.TransferDate,
        Remarks = transfer.Remarks,
        LineCount = lineCount,
        CreatedAt = transfer.CreatedAt,
        Lines = includeLines
            ? transfer.Lines.Select(l => new StockTransferLineResponse
            {
                Id = l.Id,
                LineNo = l.LineNo,
                ProductId = l.ProductId,
                ProductName = l.ProductName,
                ProductBatchId = l.ProductBatchId,
                BatchNo = l.BatchNo,
                ExpiryDate = l.ExpiryDate,
                Quantity = l.Quantity,
            }).ToList()
            : [],
    };
}
