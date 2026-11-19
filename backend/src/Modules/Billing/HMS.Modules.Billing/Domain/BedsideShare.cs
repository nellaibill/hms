namespace HMS.Modules.Billing.Domain;

/// <summary>
/// How a bedside scan's add-on is shared (ADR-144). Masters sets the bedside price and the
/// doctor's part of it; the rest is the hospital's share. The counter may charge more than the
/// bedside price, but never less than the hospital's share, and everything above that share
/// goes to the consultant. With a bedside price of 500 and a doctor's part of 200, the hospital
/// keeps 300: charging 500 gives the consultant 200, charging 600 gives them 300.
/// </summary>
internal static class BedsideShare
{
    /// <summary>The hospital's part of the bedside add-on: the bedside price less the doctor's
    /// part, never below 0.</summary>
    public static decimal HospitalShare(decimal bedsidePrice, decimal bedsideDoctorCharge)
        => Math.Max(bedsidePrice - bedsideDoctorCharge, 0m);

    /// <summary>The consultant's part of <paramref name="amount"/>, or null when the amount is
    /// below the hospital's share.</summary>
    public static decimal? DoctorCharge(decimal amount, decimal hospitalShare)
        => amount < hospitalShare ? null : amount - hospitalShare;
}
