using HMS.Modules.Masters.Application;
using HMS.Modules.Pharmacy.Application.Abstractions;
using HMS.Modules.Pharmacy.Contracts;
using HMS.Modules.Pharmacy.Domain;
using HMS.Modules.Products.Application;
using HMS.Shared.Kernel;

namespace HMS.Modules.Pharmacy.Application;

public interface IPurchaseReturnService
{
    Task<Result<PurchaseReturnResponse>> CreateAsync(CreatePurchaseReturnRequest request, Guid? actorId, CancellationToken cancellationToken);

    Task<Result<PurchaseReturnResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<PagedResult<PurchaseReturnResponse>> GetPagedAsync(PurchaseReturnListQuery query, CancellationToken cancellationToken);

    /// <summary>The batches of a product a store holds — expired ones included, since
    /// those are what most returns are for.</summary>
    Task<IReadOnlyList<ReturnableBatchResponse>> GetReturnableBatchesAsync(Guid productId, Guid? storeId, bool includeCost, CancellationToken cancellationToken);
}

/// <summary>
/// Sends stock back to a supplier. This is the proper way out for expired and damaged goods
/// that a supplier will credit: the stock leaves the store, and the supplier's account is
/// reduced by its value.
/// </summary>
internal class PurchaseReturnService : IPurchaseReturnService
{
    private readonly IPharmacyProcurementRepository _repository;
    private readonly IPharmacyGoodsReceiptRepository _goodsReceiptRepository;
    private readonly IPharmacyStockBalanceRepository _balanceRepository;
    private readonly IPharmacyStoreRepository _storeRepository;
    private readonly IPharmacyUnitOfWork _unitOfWork;
    private readonly IStockPoster _stockPoster;
    private readonly IProductService _productService;
    private readonly IProductBatchService _productBatchService;
    private readonly ISupplierService _supplierService;

    public PurchaseReturnService(
        IPharmacyProcurementRepository repository,
        IPharmacyGoodsReceiptRepository goodsReceiptRepository,
        IPharmacyStockBalanceRepository balanceRepository,
        IPharmacyStoreRepository storeRepository,
        IPharmacyUnitOfWork unitOfWork,
        IStockPoster stockPoster,
        IProductService productService,
        IProductBatchService productBatchService,
        ISupplierService supplierService)
    {
        _repository = repository;
        _goodsReceiptRepository = goodsReceiptRepository;
        _balanceRepository = balanceRepository;
        _storeRepository = storeRepository;
        _unitOfWork = unitOfWork;
        _stockPoster = stockPoster;
        _productService = productService;
        _productBatchService = productBatchService;
        _supplierService = supplierService;
    }

    public async Task<Result<PurchaseReturnResponse>> CreateAsync(CreatePurchaseReturnRequest request, Guid? actorId, CancellationToken cancellationToken)
    {
        var store = await _storeRepository.ResolveAsync(request.StoreId, cancellationToken);
        if (store is null)
        {
            return Fail(PharmacyErrorCodes.InvalidStore, "The store was not found or is inactive.");
        }

        var supplierResult = await _supplierService.GetByIdAsync(request.SupplierId, cancellationToken);
        if (!supplierResult.IsSuccess)
        {
            return Fail(PharmacyErrorCodes.InvalidSupplier, "The supplier was not found.");
        }

        var supplier = supplierResult.Value!;
        var products = await _productService.GetLookupAsync(request.Lines.Select(l => l.ProductId), cancellationToken);
        var batches = await _productBatchService.GetLookupAsync(request.Lines.Select(l => l.ProductBatchId), cancellationToken);

        for (var i = 0; i < request.Lines.Count; i++)
        {
            var line = request.Lines[i];
            if (!products.ContainsKey(line.ProductId))
            {
                return Fail(PharmacyErrorCodes.InvalidProduct, $"Line {i + 1}: the product was not found.");
            }

            if (!batches.TryGetValue(line.ProductBatchId, out var batch) || batch.ProductId != line.ProductId)
            {
                return Fail(PharmacyErrorCodes.InvalidBatch, $"Line {i + 1}: the batch was not found for that product.");
            }
        }

        var returnNo = await _unitOfWork.NextNumberAsync(DocumentSeries.PurchaseReturn, cancellationToken);

        var result = await _unitOfWork.ExecuteAsync(
            async () =>
            {
                var purchaseReturn = PharmacyPurchaseReturn.Create(
                    returnNo, store.Id, supplier.Id, supplier.SupplierName, request.ReturnDate ?? PharmacyClock.Today(), request.Reason, request.CreditNoteNo, request.Remarks, actorId);

                foreach (var line in request.Lines)
                {
                    var product = products[line.ProductId];
                    var batch = batches[line.ProductBatchId];
                    var unitRate = line.UnitRate ?? PharmacyPricing.UnitCost(product, batch) ?? 0m;
                    var gstRate = product.GstRatePercent ?? 0m;
                    var amount = GstMath.Exclusive(line.Quantity * unitRate, 0m, gstRate);

                    purchaseReturn.AddLine(product.Id, product.ProductName, batch.Id, batch.BatchNo, batch.ExpiryDate, line.Quantity, unitRate, gstRate, amount.Taxable, amount.Tax);

                    var posted = await _stockPoster.PostAsync(
                        new StockMovement(
                            TransactionType.PurchaseReturn, store.Id, product.Id, batch.Id, line.Quantity,
                            UnitCost: unitRate,
                            SourceType: StockSourceType.PurchaseReturn, SourceId: purchaseReturn.Id, SourceNo: returnNo,
                            Remarks: $"{supplier.SupplierName} · {request.Reason}",
                            Label: $"{product.ProductName} (batch {batch.BatchNo})"),
                        actorId,
                        cancellationToken);

                    if (!posted.IsSuccess)
                    {
                        return Result<PharmacyPurchaseReturn>.Failure(posted.ErrorCode!, posted.Error!);
                    }

                    // Note the return on the receipt lines the batch came in on, newest first,
                    // so a purchase shows how much of it went back. Best-effort bookkeeping:
                    // stock that arrived another way (opening stock) simply has no line to mark.
                    var remaining = line.Quantity;
                    foreach (var receiptLine in await _goodsReceiptRepository.GetLinesForBatchAsync(batch.Id, cancellationToken))
                    {
                        var take = Math.Min(remaining, receiptLine.ReceivedUnits - receiptLine.ReturnedUnits);
                        if (take <= 0)
                        {
                            continue;
                        }

                        receiptLine.RecordReturn(take, actorId);
                        remaining -= take;
                        if (remaining <= 0)
                        {
                            break;
                        }
                    }
                }

                purchaseReturn.Close();
                await _repository.AddReturnAsync(purchaseReturn, cancellationToken);
                return Result<PharmacyPurchaseReturn>.Success(purchaseReturn);
            },
            cancellationToken);

        return result.IsSuccess
            ? Result<PurchaseReturnResponse>.Success(ToResponse(result.Value!, store.Name, result.Value!.Lines.Count, includeLines: true))
            : Fail(result.ErrorCode!, result.Error!);
    }

    public async Task<Result<PurchaseReturnResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var purchaseReturn = await _repository.GetReturnAsync(id, cancellationToken);
        if (purchaseReturn is null)
        {
            return Fail(PharmacyErrorCodes.NotFound, "The purchase return was not found.");
        }

        var store = await _storeRepository.GetByIdAsync(purchaseReturn.StoreId, cancellationToken);
        return Result<PurchaseReturnResponse>.Success(ToResponse(purchaseReturn, store?.Name ?? string.Empty, purchaseReturn.Lines.Count, includeLines: true));
    }

    public async Task<PagedResult<PurchaseReturnResponse>> GetPagedAsync(PurchaseReturnListQuery query, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _repository.GetReturnsPagedAsync(query, cancellationToken);
        var storeNames = (await _storeRepository.GetAllAsync(cancellationToken)).ToDictionary(s => s.Id, s => s.Name);

        var responses = items
            .Select(i => ToResponse(i.Return, storeNames.GetValueOrDefault(i.Return.StoreId) ?? string.Empty, i.LineCount, includeLines: false))
            .ToList();

        return new PagedResult<PurchaseReturnResponse>(responses, query.Page, query.PageSize, totalCount);
    }

    public async Task<IReadOnlyList<ReturnableBatchResponse>> GetReturnableBatchesAsync(Guid productId, Guid? storeId, bool includeCost, CancellationToken cancellationToken)
    {
        var store = await _storeRepository.ResolveAsync(storeId, cancellationToken);
        var product = (await _productService.GetByIdAsync(productId, cancellationToken)).Value;
        if (store is null || product is null)
        {
            return [];
        }

        var lines = (await _balanceRepository.GetLinesAsync([productId], store.Id, cancellationToken)).Where(l => l.QuantityOnHand > 0).ToList();
        var batches = await _productBatchService.GetLookupAsync(lines.Select(l => l.ProductBatchId), cancellationToken);

        return lines
            .Where(l => batches.ContainsKey(l.ProductBatchId))
            .Select(l =>
            {
                var batch = batches[l.ProductBatchId];
                return new ReturnableBatchResponse
                {
                    ProductId = product.Id,
                    ProductName = product.ProductName,
                    ProductBatchId = batch.Id,
                    BatchNo = batch.BatchNo,
                    ExpiryDate = batch.ExpiryDate,
                    QuantityOnHand = l.QuantityOnHand,
                    UnitsPerPack = product.UnitsPerPack > 0 ? product.UnitsPerPack : 1m,
                    UnitRate = includeCost ? PharmacyPricing.UnitCost(product, batch) : null,
                    GstRatePercent = product.GstRatePercent ?? 0m,
                };
            })
            .OrderBy(b => b.ExpiryDate)
            .ToList();
    }

    private static Result<PurchaseReturnResponse> Fail(string code, string message) => Result<PurchaseReturnResponse>.Failure(code, message);

    private static PurchaseReturnResponse ToResponse(PharmacyPurchaseReturn purchaseReturn, string storeName, int lineCount, bool includeLines) => new()
    {
        Id = purchaseReturn.Id,
        ReturnNo = purchaseReturn.ReturnNo,
        StoreId = purchaseReturn.StoreId,
        StoreName = storeName,
        SupplierId = purchaseReturn.SupplierId,
        SupplierName = purchaseReturn.SupplierName,
        ReturnDate = purchaseReturn.ReturnDate,
        Reason = purchaseReturn.Reason,
        CreditNoteNo = purchaseReturn.CreditNoteNo,
        TaxableAmount = purchaseReturn.TaxableAmount,
        TaxAmount = purchaseReturn.TaxAmount,
        RoundOff = purchaseReturn.RoundOff,
        NetAmount = purchaseReturn.NetAmount,
        Remarks = purchaseReturn.Remarks,
        LineCount = lineCount,
        CreatedAt = purchaseReturn.CreatedAt,
        Lines = includeLines
            ? purchaseReturn.Lines.Select(l => new PurchaseReturnLineResponse
            {
                Id = l.Id,
                LineNo = l.LineNo,
                ProductId = l.ProductId,
                ProductName = l.ProductName,
                ProductBatchId = l.ProductBatchId,
                BatchNo = l.BatchNo,
                ExpiryDate = l.ExpiryDate,
                Quantity = l.Quantity,
                UnitRate = l.UnitRate,
                GstRatePercent = l.GstRatePercent,
                TaxableAmount = l.TaxableAmount,
                TaxAmount = l.TaxAmount,
                LineTotal = l.LineTotal,
            }).ToList()
            : [],
    };
}

public interface ISupplierAccountService
{
    Task<IReadOnlyList<SupplierBalanceResponse>> GetBalancesAsync(CancellationToken cancellationToken);

    Task<Result<SupplierStatementResponse>> GetStatementAsync(Guid supplierId, DateOnly? fromDate, DateOnly? toDate, CancellationToken cancellationToken);

    Task<Result<SupplierPaymentResponse>> RecordPaymentAsync(Guid supplierId, CreateSupplierPaymentRequest request, Guid? actorId, CancellationToken cancellationToken);
}

/// <summary>
/// The pharmacy's side of each supplier's account: purchases add to what is owed, returns
/// and payments take off it. Kept as a running account rather than invoice-by-invoice
/// settlement, which is how a distributor's own statement is laid out and reconciled.
/// </summary>
internal class SupplierAccountService : ISupplierAccountService
{
    private readonly IPharmacyProcurementRepository _repository;
    private readonly IPharmacyUnitOfWork _unitOfWork;
    private readonly ISupplierService _supplierService;

    public SupplierAccountService(IPharmacyProcurementRepository repository, IPharmacyUnitOfWork unitOfWork, ISupplierService supplierService)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _supplierService = supplierService;
    }

    public async Task<IReadOnlyList<SupplierBalanceResponse>> GetBalancesAsync(CancellationToken cancellationToken)
        => (await _repository.GetSupplierTotalsAsync(cancellationToken))
            .Select(t => new SupplierBalanceResponse
            {
                SupplierId = t.SupplierId,
                SupplierName = t.SupplierName,
                Purchases = t.Purchases,
                Returns = t.Returns,
                Payments = t.Payments,
                Outstanding = t.Purchases - t.Returns - t.Payments,
                LastPurchaseDate = t.LastPurchaseDate,
            })
            .ToList();

    public async Task<Result<SupplierStatementResponse>> GetStatementAsync(Guid supplierId, DateOnly? fromDate, DateOnly? toDate, CancellationToken cancellationToken)
    {
        var supplierResult = await _supplierService.GetByIdAsync(supplierId, cancellationToken);
        if (!supplierResult.IsSuccess)
        {
            return Result<SupplierStatementResponse>.Failure(PharmacyErrorCodes.NotFound, "The supplier was not found.");
        }

        var entries = await _repository.GetSupplierEntriesAsync(supplierId, cancellationToken);
        return Result<SupplierStatementResponse>.Success(BuildStatement(supplierId, supplierResult.Value!.SupplierName, entries, fromDate, toDate));
    }

    /// <summary>Running balances over a supplier's entries; anything before
    /// <paramref name="fromDate"/> is rolled into the opening balance.</summary>
    internal static SupplierStatementResponse BuildStatement(Guid supplierId, string supplierName, IReadOnlyList<SupplierEntry> entries, DateOnly? fromDate, DateOnly? toDate)
    {
        var opening = 0m;
        var balance = 0m;
        var shown = new List<SupplierStatementEntry>();

        foreach (var entry in entries)
        {
            if (toDate is { } to && entry.Date > to)
            {
                break;
            }

            var signed = entry.Type == SupplierEntryType.Purchase ? entry.Amount : -entry.Amount;
            balance += signed;

            if (fromDate is { } from && entry.Date < from)
            {
                opening = balance;
                continue;
            }

            shown.Add(new SupplierStatementEntry
            {
                Date = entry.Date,
                Type = entry.Type,
                DocumentId = entry.DocumentId,
                DocumentNo = entry.DocumentNo,
                Reference = entry.Reference,
                Credit = entry.Type == SupplierEntryType.Purchase ? entry.Amount : 0m,
                Debit = entry.Type == SupplierEntryType.Purchase ? 0m : entry.Amount,
                Balance = balance,
            });
        }

        return new SupplierStatementResponse
        {
            SupplierId = supplierId,
            SupplierName = supplierName,
            FromDate = fromDate,
            ToDate = toDate,
            OpeningBalance = opening,
            ClosingBalance = balance,
            Entries = shown,
        };
    }

    public async Task<Result<SupplierPaymentResponse>> RecordPaymentAsync(Guid supplierId, CreateSupplierPaymentRequest request, Guid? actorId, CancellationToken cancellationToken)
    {
        var supplierResult = await _supplierService.GetByIdAsync(supplierId, cancellationToken);
        if (!supplierResult.IsSuccess)
        {
            return Result<SupplierPaymentResponse>.Failure(PharmacyErrorCodes.NotFound, "The supplier was not found.");
        }

        var paymentNo = await _unitOfWork.NextNumberAsync(DocumentSeries.SupplierPayment, cancellationToken);
        var payment = PharmacySupplierPayment.Create(
            paymentNo, supplierId, supplierResult.Value!.SupplierName, request.PaymentDate ?? PharmacyClock.Today(), request.Amount, request.Method, request.ReferenceNo, request.Remarks, actorId);

        await _repository.AddPaymentAsync(payment, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<SupplierPaymentResponse>.Success(new SupplierPaymentResponse
        {
            Id = payment.Id,
            PaymentNo = payment.PaymentNo,
            SupplierId = payment.SupplierId,
            SupplierName = payment.SupplierName,
            PaymentDate = payment.PaymentDate,
            Amount = payment.Amount,
            Method = payment.Method,
            ReferenceNo = payment.ReferenceNo,
            Remarks = payment.Remarks,
            CreatedAt = payment.CreatedAt,
        });
    }
}
