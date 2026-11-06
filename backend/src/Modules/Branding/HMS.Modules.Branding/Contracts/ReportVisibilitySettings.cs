namespace HMS.Modules.Branding.Contracts;

/// <summary>
/// Which Accounts &amp; Finance report tabs the hospital shows (ADR-120). A report missing from
/// <see cref="VisibleReports"/> is hidden from everyone, Super Admins included. Empty, the
/// default, hides them all. OP Report always shows, and the bill lists and approval queues
/// (Recent Bills, All Invoices, Revert Requests, Discount Requests) aren't reports, so none of
/// them can be listed here. Stored as one jsonb column, like <see cref="PrintSettings"/>.
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
    public const string IncomeExpense = "incomeExpense";
    public const string Laboratory = "laboratory";
    public const string Radiology = "radiology";
    public const string Consultant = "consultant";
    public const string Pharmacy = "pharmacy";

    public static readonly IReadOnlyList<string> All = [IncomeExpense, Laboratory, Radiology, Consultant, Pharmacy];

    public static bool IsValid(string? value) => value is not null && All.Contains(value);
}
