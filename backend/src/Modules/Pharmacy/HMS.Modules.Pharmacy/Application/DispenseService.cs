using HMS.Modules.Pharmacy.Application.Abstractions;
using HMS.Modules.Pharmacy.Application.Mapping;
using HMS.Modules.Pharmacy.Contracts;
using HMS.Modules.Pharmacy.Domain;
using HMS.Modules.Billing.Application;
using HMS.Modules.Billing.Contracts;
using HMS.Modules.Patients.Application;
using HMS.Modules.Patients.Contracts;
using HMS.Modules.Products.Application;
using HMS.Modules.Products.Contracts;
using HMS.Shared.Kernel;
using Microsoft.EntityFrameworkCore;

namespace HMS.Modules.Pharmacy.Application;

/// <summary>
/// Public (not internal): DispensesController — which ASP.NET Core requires to be a public
/// class with a public constructor for controller discovery/DI activation — takes this as a
/// constructor dependency; a public constructor cannot have an internal parameter type
/// (CS0051). Mirrors HMS.Modules.IPD.Application.IAdmissionService.
/// </summary>
public interface IDispenseService
{
    Task<Result<DispenseResponse>> CreateAsync(CreateDispenseRequest request, Guid? actorId, CancellationToken cancellationToken);

    /// <summary>Dispenses several product/batch/quantity lines for one patient in a single
    /// checkout, billed as ONE invoice with N line items — see DispenseService.CreateCartAsync.</summary>
    Task<Result<DispenseCartResponse>> CreateCartAsync(CreateDispenseCartRequest request, Guid? actorId, CancellationToken cancellationToken);

    Task<Result<DispenseResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<PagedResult<DispenseResponse>> GetPagedAsync(DispenseListQuery query, CancellationToken cancellationToken);
}

internal class DispenseService : IDispenseService
{
    /// <summary>
    /// The lost-update race this guards against: two concurrent dispenses read the same
    /// QuantityOnHand, both pass the "enough stock" check, then both try to decrement — the
    /// loser's SaveChangesAsync throws DbUpdateConcurrencyException (the xmin token it read
    /// no longer matches). Instead of surfacing that as a raw 500, the loser re-fetches the
    /// now-fresh balance, re-validates quantity is no more than QuantityOnHand against it, and either
    /// succeeds against the winner's updated total or correctly fails InsufficientStock —
    /// bounded at 3 attempts so a pathological hot-batch can't retry forever.
    /// </summary>
    private const int MaxDispenseAttempts = 3;

    private readonly IPharmacyStockBalanceRepository _balanceRepository;
    private readonly IPharmacyStockTransactionRepository _transactionRepository;
    private readonly IProductService _productService;
    private readonly IProductBatchService _productBatchService;
    private readonly IPatientService _patientService;
    private readonly IInvoiceService _invoiceService;

    public DispenseService(
        IPharmacyStockBalanceRepository balanceRepository,
        IPharmacyStockTransactionRepository transactionRepository,
        IProductService productService,
        IProductBatchService productBatchService,
        IPatientService patientService,
        IInvoiceService invoiceService)
    {
        _balanceRepository = balanceRepository;
        _transactionRepository = transactionRepository;
        _productService = productService;
        _productBatchService = productBatchService;
        _patientService = patientService;
        _invoiceService = invoiceService;
    }

    public async Task<Result<DispenseResponse>> CreateAsync(CreateDispenseRequest request, Guid? actorId, CancellationToken cancellationToken)
    {
        var patientResult = await _patientService.GetByIdAsync(request.PatientId, cancellationToken);
        if (!patientResult.IsSuccess)
        {
            return Result<DispenseResponse>.Failure(PharmacyErrorCodes.InvalidPatient, $"Patient '{request.PatientId}' was not found.");
        }

        var lineRequest = new DispenseCartLineRequest
        {
            ProductId = request.ProductId,
            ProductBatchId = request.ProductBatchId,
            Quantity = request.Quantity,
            Remarks = request.Remarks,
        };

        var dispenseResult = await DispenseLinesAsync([lineRequest], request.PatientId, request.AdmissionId, actorId, cancellationToken);
        if (!dispenseResult.IsSuccess)
        {
            return Result<DispenseResponse>.Failure(dispenseResult.ErrorCode!, dispenseResult.Error!);
        }

        var patient = patientResult.Value!;
        var patientName = $"{patient.FirstName} {patient.LastName}";
        var dispensed = dispenseResult.Value![0];

        var (_, invoiceNumber, billingFailed, billingError, _) =
            await BillAsync(patient, request.PatientId, dispenseResult.Value, actorId, cancellationToken);

        return Result<DispenseResponse>.Success(
            dispensed.Transaction.ToDispenseResponse(dispensed.Product.ProductName, dispensed.Batch.BatchNo, patientName, invoiceNumber, billingFailed, billingError));
    }

    public async Task<Result<DispenseCartResponse>> CreateCartAsync(CreateDispenseCartRequest request, Guid? actorId, CancellationToken cancellationToken)
    {
        var patientResult = await _patientService.GetByIdAsync(request.PatientId, cancellationToken);
        if (!patientResult.IsSuccess)
        {
            return Result<DispenseCartResponse>.Failure(PharmacyErrorCodes.InvalidPatient, $"Patient '{request.PatientId}' was not found.");
        }

        var dispenseResult = await DispenseLinesAsync(request.Lines, request.PatientId, request.AdmissionId, actorId, cancellationToken);
        if (!dispenseResult.IsSuccess)
        {
            return Result<DispenseCartResponse>.Failure(dispenseResult.ErrorCode!, dispenseResult.Error!);
        }

        var patient = patientResult.Value!;
        var patientName = $"{patient.FirstName} {patient.LastName}";

        var (invoiceId, invoiceNumber, billingFailed, billingError, totalAmount) =
            await BillAsync(patient, request.PatientId, dispenseResult.Value!, actorId, cancellationToken);

        var lines = dispenseResult.Value!
            .Select(d => d.Transaction.ToDispenseResponse(d.Product.ProductName, d.Batch.BatchNo, patientName, invoiceNumber, billingFailed, billingError))
            .ToList();

        return Result<DispenseCartResponse>.Success(new DispenseCartResponse
        {
            Lines = lines,
            InvoiceId = invoiceId,
            InvoiceNumber = invoiceNumber,
            BillingFailed = billingFailed,
            BillingError = billingError,
            TotalAmount = totalAmount,
        });
    }

    private sealed record DispensedLine(PharmacyStockTransaction Transaction, ProductResponse Product, ProductBatchResponse Batch, decimal Quantity);

    /// <summary>
    /// Shared by CreateAsync (a 1-line cart) and CreateCartAsync (N lines): validates every
    /// line's product/batch/expiry once up front, then — within each concurrency-retry
    /// attempt — re-checks every line's stock (pass 1) before mutating ANY line (pass 2), so a
    /// mid-attempt insufficient-stock failure never leaves part of the cart dispensed. One
    /// SaveChangesAsync commits the whole attempt's balance decrements and ledger rows
    /// together — genuinely all-or-nothing for stock, since EF wraps each SaveChangesAsync call
    /// in its own implicit DB transaction.
    /// </summary>
    private async Task<Result<List<DispensedLine>>> DispenseLinesAsync(
        IReadOnlyList<DispenseCartLineRequest> lines,
        Guid patientId,
        Guid? admissionId,
        Guid? actorId,
        CancellationToken cancellationToken)
    {
        var multiLine = lines.Count > 1;
        var contexts = new List<(DispenseCartLineRequest Line, ProductResponse Product, ProductBatchResponse Batch)>(lines.Count);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        for (var i = 0; i < lines.Count; i++)
        {
            var line = lines[i];
            var linePrefix = multiLine ? $"Line {i + 1}: " : string.Empty;

            var productResult = await _productService.GetByIdAsync(line.ProductId, cancellationToken);
            if (!productResult.IsSuccess)
            {
                return Result<List<DispensedLine>>.Failure(PharmacyErrorCodes.InvalidProduct, $"{linePrefix}Product '{line.ProductId}' was not found.");
            }

            var batchResult = await _productBatchService.GetByIdAsync(line.ProductId, line.ProductBatchId, cancellationToken);
            if (!batchResult.IsSuccess)
            {
                return Result<List<DispensedLine>>.Failure(PharmacyErrorCodes.InvalidBatch, $"{linePrefix}Batch '{line.ProductBatchId}' was not found for product '{line.ProductId}'.");
            }

            if (batchResult.Value!.ExpiryDate < today)
            {
                return Result<List<DispensedLine>>.Failure(
                    PharmacyErrorCodes.BatchExpired,
                    $"{linePrefix}Batch '{batchResult.Value.BatchNo}' expired on {batchResult.Value.ExpiryDate:yyyy-MM-dd}.");
            }

            contexts.Add((line, productResult.Value!, batchResult.Value!));
        }

        for (var attempt = 1; attempt <= MaxDispenseAttempts; attempt++)
        {
            var balances = new List<PharmacyStockBalance>(contexts.Count);

            for (var i = 0; i < contexts.Count; i++)
            {
                var line = contexts[i].Line;
                var balance = await _balanceRepository.GetByProductAndBatchAsync(line.ProductId, line.ProductBatchId, cancellationToken);
                var quantityOnHand = balance?.QuantityOnHand ?? 0m;

                if (line.Quantity > quantityOnHand)
                {
                    var linePrefix = multiLine ? $"Line {i + 1}: " : string.Empty;
                    return Result<List<DispensedLine>>.Failure(
                        PharmacyErrorCodes.InsufficientStock,
                        $"{linePrefix}Cannot dispense {line.Quantity} — only {quantityOnHand} available for this product/batch.");
                }

                balances.Add(balance!);
            }

            var dispensed = new List<DispensedLine>(contexts.Count);
            for (var i = 0; i < contexts.Count; i++)
            {
                var (line, product, batch) = contexts[i];
                var balance = balances[i];

                balance.Dispense(line.Quantity, actorId);

                // Added alongside the balance mutation and saved together below so every
                // line's decrement and its ledger record commit as one atomic transaction.
                var transaction = PharmacyStockTransaction.CreateDispense(
                    line.ProductId, line.ProductBatchId, line.Quantity, balance.QuantityOnHand, patientId, admissionId, line.Remarks, actorId);

                await _transactionRepository.AddAsync(transaction, cancellationToken);
                dispensed.Add(new DispensedLine(transaction, product, batch, line.Quantity));
            }

            try
            {
                await _balanceRepository.SaveChangesAsync(cancellationToken);
                return Result<List<DispensedLine>>.Success(dispensed);
            }
            catch (DbUpdateConcurrencyException) when (attempt < MaxDispenseAttempts)
            {
                // A concurrent dispense against one of these batches committed first. Detach
                // every candidate row added this attempt so none are re-inserted on the next
                // one, then loop again: the next pass re-fetches (and reloads) every balance
                // and re-validates every line's quantity against the now-current values.
                foreach (var d in dispensed)
                {
                    _transactionRepository.Detach(d.Transaction);
                }
            }
        }

        // Unreachable: the loop above always returns (success or the final attempt's
        // uncaught DbUpdateConcurrencyException propagating) within MaxDispenseAttempts.
        throw new InvalidOperationException("Dispense retry loop exited without a result.");
    }

    /// <summary>
    /// Best-effort, deliberately separate from DispenseLinesAsync's SaveChangesAsync (ADR-028):
    /// the dispense — medicine has physically left the pharmacy, stock is already correctly
    /// decremented — is the authoritative fact and must not be rolled back just because the
    /// separate Billing module's write failed or Billing itself is unreachable. A failure here
    /// is surfaced to the caller (BillingFailed/BillingError) so staff can post the charge
    /// manually via the existing OPD Billing Entry screen; it never fails or reverts the
    /// dispense itself. One invoice covers every line passed in — a single dispense's 1-line
    /// cart, or a real cart's N lines — since Billing's CreateInvoiceRequest.Items already
    /// supports multiple line items in one call.
    /// </summary>
    private async Task<(Guid? InvoiceId, string? InvoiceNumber, bool BillingFailed, string? BillingError, decimal TotalAmount)> BillAsync(
        PatientResponse patient,
        Guid patientId,
        IReadOnlyList<DispensedLine> lines,
        Guid? actorId,
        CancellationToken cancellationToken)
    {
        var patientName = $"{patient.FirstName} {patient.LastName}";
        var items = new List<CreateInvoiceLineItemRequest>(lines.Count);
        var totalAmount = 0m;

        foreach (var line in lines)
        {
            var lineTotal = line.Quantity * line.Product.SellingPrice;
            totalAmount += lineTotal;
            items.Add(new CreateInvoiceLineItemRequest
            {
                BillingType = BillingType.Pharmacy,
                ServiceId = $"{line.Product.ProductName} (Batch {line.Batch.BatchNo}) × {line.Quantity}",
                // Quantity is fixed at 1: CreateInvoiceLineItemRequest.Quantity is an int
                // (every other billing category bills whole units), but a dispense's real
                // quantity is decimal (e.g. 150.5ml of a syrup) — so the full dispensed amount
                // is priced into UnitPrice as this one line's total, rather than losing
                // precision by rounding Quantity.
                Quantity = 1,
                UnitPrice = lineTotal,
                Discount = 0,
                DiscountApproved = false,
            });
        }

        try
        {
            var invoiceRequest = new CreateInvoiceRequest
            {
                PatientId = patientId,
                // Encounter/visit tracking (PatientResponse.CurrentRegistration) is out of
                // scope for the current Patients module design — falls back to patientId,
                // exactly like this already did whenever a patient had no current
                // registration.
                VisitId = patientId,
                PatientName = patientName,
                PatientUhid = patient.Uhid,
                Items = items,
            };

            var invoiceResult = await _invoiceService.CreateAsync(invoiceRequest, actorId, cancellationToken);
            if (invoiceResult.IsSuccess)
            {
                foreach (var line in lines)
                {
                    line.Transaction.SetInvoiceId(invoiceResult.Value!.Id, actorId);
                }

                await _balanceRepository.SaveChangesAsync(cancellationToken);
                return (invoiceResult.Value!.Id, invoiceResult.Value.InvoiceNumber, false, null, totalAmount);
            }

            return (null, null, true, invoiceResult.Error, totalAmount);
        }
        catch (Exception ex)
        {
            // Billing being unreachable/erroring is exactly the case this whole best-effort
            // design exists for — never let it propagate up as a 500 on an otherwise-successful
            // dispense.
            return (null, null, true, ex.Message, totalAmount);
        }
    }

    public async Task<Result<DispenseResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var transaction = await _transactionRepository.GetByIdAsync(id, cancellationToken);
        if (transaction is null || transaction.TransactionType != TransactionType.Dispense)
        {
            return Result<DispenseResponse>.Failure(PharmacyErrorCodes.NotFound, $"Dispense '{id}' was not found.");
        }

        return Result<DispenseResponse>.Success(await BuildResponseAsync(transaction, cancellationToken));
    }

    public async Task<PagedResult<DispenseResponse>> GetPagedAsync(DispenseListQuery query, CancellationToken cancellationToken)
    {
        var ledgerQuery = new StockLedgerListQuery
        {
            Page = query.Page,
            PageSize = query.PageSize,
            Sort = query.Sort,
            ProductId = query.ProductId,
            PatientId = query.PatientId,
            TransactionType = TransactionType.Dispense,
        };

        var (items, totalCount) = await _transactionRepository.GetPagedAsync(ledgerQuery, cancellationToken);

        // MVP-scale N+1 lookups (one Product/ProductBatch/Patient round-trip per row) to
        // denormalize display fields — same trade-off IPD's AdmissionService.GetPagedAsync
        // documents; revisit with a bulk lookup if this list grows large enough to matter.
        var responses = new List<DispenseResponse>(items.Count);
        foreach (var item in items)
        {
            responses.Add(await BuildResponseAsync(item, cancellationToken));
        }

        return new PagedResult<DispenseResponse>(responses, query.Page, query.PageSize, totalCount);
    }

    private async Task<DispenseResponse> BuildResponseAsync(PharmacyStockTransaction transaction, CancellationToken cancellationToken)
    {
        var productResult = await _productService.GetByIdAsync(transaction.ProductId, cancellationToken);
        var batchResult = await _productBatchService.GetByIdAsync(transaction.ProductId, transaction.ProductBatchId, cancellationToken);

        var patientName = string.Empty;
        if (transaction.PatientId.HasValue)
        {
            var patientResult = await _patientService.GetByIdAsync(transaction.PatientId.Value, cancellationToken);
            var patient = patientResult.Value;
            patientName = patient is null ? string.Empty : $"{patient.FirstName} {patient.LastName}";
        }

        return transaction.ToDispenseResponse(
            productResult.Value?.ProductName ?? string.Empty,
            batchResult.Value?.BatchNo ?? string.Empty,
            patientName);
    }
}
