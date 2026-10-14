namespace HMS.Modules.Pharmacy.Application;

/// <summary>One line's money, split the way a GST bill needs it.</summary>
internal readonly record struct TaxedAmount(decimal Gross, decimal Discount, decimal Taxable, decimal Tax, decimal Total);

/// <summary>
/// GST arithmetic for the two directions a pharmacy trades in. Suppliers quote rates before
/// tax, so a purchase adds GST on top. MRP already includes tax, so a sale works backwards
/// from the price the customer pays to find the tax inside it.
/// </summary>
internal static class GstMath
{
    public static decimal Round2(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);

    /// <summary>Purchase side: <paramref name="gross"/> is before tax; discount comes off
    /// first, then GST is added.</summary>
    public static TaxedAmount Exclusive(decimal gross, decimal discountPercent, decimal gstRatePercent)
    {
        gross = Round2(gross);
        var discount = Round2(gross * discountPercent / 100m);
        var taxable = gross - discount;
        var tax = Round2(taxable * gstRatePercent / 100m);
        return new TaxedAmount(gross, discount, taxable, tax, taxable + tax);
    }

    /// <summary>Sale side: <paramref name="gross"/> is MRP × quantity, tax included. Discount
    /// comes off that, and the tax is whatever part of the remainder is GST.</summary>
    public static TaxedAmount Inclusive(decimal gross, decimal discountAmount, decimal gstRatePercent)
    {
        gross = Round2(gross);
        var discount = Math.Min(Round2(discountAmount), gross);
        var total = gross - discount;
        var taxable = Round2(total / (1m + gstRatePercent / 100m));
        return new TaxedAmount(gross, discount, taxable, total - taxable, total);
    }

    /// <summary>A bill is settled in whole rupees; the paise dropped or added are shown as
    /// round-off so the lines still add up to the total.</summary>
    public static (decimal RoundOff, decimal Net) RoundToRupee(decimal total)
    {
        var net = Math.Round(total, 0, MidpointRounding.AwayFromZero);
        return (net - total, net);
    }

    /// <summary>CGST and SGST halves of a tax amount. The odd paisa goes to CGST so the two
    /// always add back to the whole.</summary>
    public static (decimal Cgst, decimal Sgst) SplitIntraState(decimal tax)
    {
        var sgst = Math.Floor(tax * 100m / 2m) / 100m;
        return (tax - sgst, sgst);
    }
}
