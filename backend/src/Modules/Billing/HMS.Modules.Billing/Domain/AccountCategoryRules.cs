using System.Linq.Expressions;
using HMS.Modules.Billing.Contracts;
using VisitType = HMS.Modules.Patients.Contracts.VisitType;

namespace HMS.Modules.Billing.Domain;

/// <summary>
/// Which bills each Accounts and Finance category lists (ADR-134). OP and IP split every bill by
/// its saved encounter type (<see cref="Invoice.EncounterType"/>), so a bill is in exactly one of
/// them: IP holds IP, Day Care and Observation visits (the ones that get an IP No, ADR-104) and
/// IPD final bills, which are saved as IP; OP holds OP and Emergency visits and bills with no
/// encounter type. Lab cuts across both: every bill with at least one Laboratory line.
/// Written as plain comparisons so EF Core turns them into SQL.
/// </summary>
internal static class AccountCategoryRules
{
    public static Expression<Func<Invoice, bool>> Predicate(AccountCategory category) => category switch
    {
        AccountCategory.Ip => invoice =>
            invoice.EncounterType == VisitType.IP
            || invoice.EncounterType == VisitType.DayCare
            || invoice.EncounterType == VisitType.Observation,
        AccountCategory.Op => invoice =>
            invoice.EncounterType == null
            || invoice.EncounterType == VisitType.OP
            || invoice.EncounterType == VisitType.Emergency,
        AccountCategory.Lab => invoice => invoice.Items.Any(line => line.BillingType == BillingType.Laboratory),
        _ => throw new ArgumentOutOfRangeException(nameof(category), category, "Unknown account category."),
    };

    /// <summary>The encounter type a new bill is saved with: its visit's type when it has one,
    /// IP for an IPD final bill (it has no visit of its own and carries the patient's id instead,
    /// see CreateInvoiceRequest.VisitId), and null otherwise.</summary>
    public static VisitType? EncounterTypeFor(VisitType? visitType, IEnumerable<BillingType> lineTypes)
        => visitType ?? (lineTypes.Any(type => type == BillingType.InpatientCharge) ? VisitType.IP : null);
}
