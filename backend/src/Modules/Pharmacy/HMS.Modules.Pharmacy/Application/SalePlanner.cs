using HMS.Modules.Pharmacy.Contracts;
using HMS.Modules.Products.Contracts;
using HMS.Shared.Kernel;

namespace HMS.Modules.Pharmacy.Application;

/// <summary>One bill line as it will be saved: a single batch, priced and taxed.</summary>
internal sealed record PlannedSaleLine(
    ProductResponse Product,
    ProductBatchResponse Batch,
    decimal Quantity,
    decimal UnitPrice,
    decimal? UnitCost,
    decimal DiscountPercent,
    decimal GstRatePercent,
    TaxedAmount Amount);

/// <summary>A payment as it will be recorded — after change has been taken off the cash.</summary>
internal sealed record PlannedPayment(SalePaymentMethod Method, decimal Amount, string? ReferenceNo);

internal sealed record SalePlan(
    IReadOnlyList<PlannedSaleLine> Lines,
    decimal NetAmount,
    IReadOnlyList<PlannedPayment> Payments,
    decimal ChangeGiven,
    decimal BalanceDue);

/// <summary>
/// Works a bill out before anything is saved: which batches each line comes from, what each
/// line costs after discount and how much of it is GST, and what the money tendered settles.
/// Pure — it reads no database — so every rule of pricing a bill can be tested on its own.
/// </summary>
internal static class SalePlanner
{
    /// <summary>
    /// Turns requested lines into single-batch lines. A line that names a batch is taken
    /// from it; one that does not is filled first-expiring-first, splitting across batches
    /// when the first cannot cover it. Stock promised to an earlier line of the same bill is
    /// not offered to a later one.
    /// </summary>
    public static Result<IReadOnlyList<PlannedSaleLine>> PlanLines(
        IReadOnlyList<SaleLineRequest> lines,
        IReadOnlyDictionary<Guid, SellableItem> stock,
        decimal billDiscountPercent)
    {
        var remaining = stock.Values
            .SelectMany(item => item.Batches)
            .ToDictionary(b => b.Batch.Id, b => b.Quantity);

        var planned = new List<PlannedSaleLine>();

        for (var i = 0; i < lines.Count; i++)
        {
            var line = lines[i];
            var prefix = lines.Count > 1 ? $"Line {i + 1}: " : string.Empty;

            if (!stock.TryGetValue(line.ProductId, out var item))
            {
                return Fail(PharmacyErrorCodes.InvalidProduct, $"{prefix}the product was not found.");
            }

            var product = item.Product;
            if (!product.IsActive)
            {
                return Fail(PharmacyErrorCodes.ProductInactive, $"{prefix}{product.ProductName} is inactive and can't be sold.");
            }

            var candidates = line.ProductBatchId is { } batchId
                ? item.Batches.Where(b => b.Batch.Id == batchId).ToList()
                : item.Batches.ToList();

            if (line.ProductBatchId is not null && candidates.Count == 0)
            {
                return Fail(PharmacyErrorCodes.InvalidBatch, $"{prefix}that batch of {product.ProductName} has no sellable stock in this store.");
            }

            var discountPercent = line.DiscountPercent ?? billDiscountPercent;
            var gstRate = product.GstRatePercent ?? 0m;
            var needed = line.Quantity;

            foreach (var candidate in candidates)
            {
                if (needed <= 0)
                {
                    break;
                }

                var take = Math.Min(needed, remaining[candidate.Batch.Id]);
                if (take <= 0)
                {
                    continue;
                }

                remaining[candidate.Batch.Id] -= take;
                needed -= take;

                var gross = take * candidate.UnitPrice;
                var amount = GstMath.Inclusive(gross, gross * discountPercent / 100m, gstRate);
                planned.Add(new PlannedSaleLine(product, candidate.Batch, take, candidate.UnitPrice, candidate.UnitCost, discountPercent, gstRate, amount));
            }

            if (needed > 0)
            {
                var available = line.Quantity - needed;
                return Fail(
                    PharmacyErrorCodes.InsufficientStock,
                    $"{prefix}only {available:0.####} of {product.ProductName} available — {line.Quantity:0.####} needed.");
            }
        }

        return Result<IReadOnlyList<PlannedSaleLine>>.Success(planned);
    }

    /// <summary>
    /// Settles what was tendered against the bill. Cash over the amount due is change and is
    /// not recorded; card, UPI and bank payments are exact by nature, so they may never add up
    /// to more than the bill. Anything short is a balance due, which the caller allows or not.
    /// </summary>
    public static Result<(IReadOnlyList<PlannedPayment> Payments, decimal Change, decimal Due)> SettlePayments(
        decimal netAmount, IReadOnlyList<SalePaymentRequest> tendered)
    {
        var nonCash = tendered.Where(p => p.Method != SalePaymentMethod.Cash).Sum(p => p.Amount);
        if (nonCash > netAmount)
        {
            return Result<(IReadOnlyList<PlannedPayment>, decimal, decimal)>.Failure(
                PharmacyErrorCodes.InvalidPayment,
                $"Card, UPI and bank payments add up to {nonCash:0.00}, more than the bill of {netAmount:0.00}.");
        }

        var cashNeeded = netAmount - nonCash;
        var cashTendered = tendered.Where(p => p.Method == SalePaymentMethod.Cash).Sum(p => p.Amount);
        var cashKept = Math.Min(cashTendered, cashNeeded);

        var payments = tendered
            .Where(p => p.Method != SalePaymentMethod.Cash && p.Amount > 0)
            .Select(p => new PlannedPayment(p.Method, p.Amount, p.ReferenceNo))
            .ToList();

        if (cashKept > 0)
        {
            payments.Add(new PlannedPayment(SalePaymentMethod.Cash, cashKept, null));
        }

        return Result<(IReadOnlyList<PlannedPayment>, decimal, decimal)>.Success(
            (payments, cashTendered - cashKept, netAmount - nonCash - cashKept));
    }

    /// <summary>The bill total: every line's amount, settled in whole rupees.</summary>
    public static decimal NetAmount(IReadOnlyList<PlannedSaleLine> lines)
        => GstMath.RoundToRupee(lines.Sum(l => l.Amount.Total)).Net;

    private static Result<IReadOnlyList<PlannedSaleLine>> Fail(string code, string message)
        => Result<IReadOnlyList<PlannedSaleLine>>.Failure(code, char.ToUpperInvariant(message[0]) + message[1..]);
}
