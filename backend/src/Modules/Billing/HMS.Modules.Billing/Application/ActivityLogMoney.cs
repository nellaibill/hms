using System.Globalization;

namespace HMS.Modules.Billing.Application;

/// <summary>Amounts in activity log descriptions read "₹1,300.00", whatever the server's culture.</summary>
internal static class ActivityLogMoney
{
    public static string Format(decimal amount) => "₹" + amount.ToString("#,##0.00", CultureInfo.InvariantCulture);
}
