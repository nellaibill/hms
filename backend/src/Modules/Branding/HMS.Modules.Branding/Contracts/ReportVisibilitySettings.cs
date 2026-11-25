namespace HMS.Modules.Branding.Contracts;

/// <summary>
/// Which Accounts &amp; Finance report tabs the hospital shows (ADR-120). A report missing from
/// <see cref="VisibleReports"/> is hidden from everyone, Super Admins included. Empty, the
/// default, hides them all. OP Report and Collection Summary always show. Since ADR-151 the bill
/// lists and approval queues (Recent Bills, All Invoices, Cancellation Requests, Discount
/// Requests) are switched here too, so a hospital shows only the Accounts tabs it uses. Stored as
/// one jsonb column, like <see cref="PrintSettings"/>.
/// </summary>
public record ReportVisibilitySettings
{
    /// <summary><see cref="AccountsReportKeys"/> values.</summary>
    public List<string> VisibleReports { get; init; } = [];
}

/// <summary>The Accounts &amp; Finance reports a hospital can turn on. Kept in sync with
/// frontend/web/src/features/branding/types.ts's ACCOUNTS_REPORTS.</summary>
public static class AccountsReportKeys
{
    // ADR-151: the bill lists and approval queues, each a tab a hospital can show or hide.
    public const string RecentBills = "recentBills";
    public const string AllInvoices = "allInvoices";
    public const string CancellationRequests = "cancellationRequests";
    public const string DiscountRequests = "discountRequests";

    /// <summary>Patient Reports — registrations and visits for a period — moved into Accounts and
    /// Finance from its own sidebar menu (ADR-151).</summary>
    public const string PatientReports = "patientReports";

    public const string IncomeExpense = "incomeExpense";
    public const string Laboratory = "laboratory";
    public const string Radiology = "radiology";
    public const string Consultant = "consultant";
    public const string Pharmacy = "pharmacy";

    public static readonly IReadOnlyList<string> All =
        [RecentBills, AllInvoices, CancellationRequests, DiscountRequests, IncomeExpense, Laboratory, Radiology, Consultant, Pharmacy, PatientReports];

    public static bool IsValid(string? value) => value is not null && All.Contains(value);
}
