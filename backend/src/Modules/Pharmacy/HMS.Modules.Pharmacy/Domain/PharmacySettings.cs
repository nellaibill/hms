using HMS.Modules.Pharmacy.Contracts;
using HMS.Shared.Kernel;

namespace HMS.Modules.Pharmacy.Domain;

/// <summary>
/// One row per hospital: the particulars printed on every pharmacy bill, and the handful of
/// rules the counter enforces. Kept in the pharmacy rather than in hospital branding because
/// a hospital pharmacy commonly trades under its own GSTIN and always under its own drug
/// licence.
/// </summary>
internal class PharmacySettings : Entity
{
    /// <summary>Printed at the head of the bill. Null prints the hospital's name.</summary>
    public string? PharmacyName { get; private set; }
    public string? Gstin { get; private set; }
    public string? DrugLicenceNo { get; private set; }
    public string? Address { get; private set; }
    public string? Phone { get; private set; }
    public string? BillFooter { get; private set; }
    public BillPrintFormat PrintFormat { get; private set; } = BillPrintFormat.A5;

    /// <summary>The most discount the counter may give on a bill. 100 is "no limit".</summary>
    public decimal MaxDiscountPercent { get; private set; } = 100m;

    /// <summary>How many days ahead a batch counts as "expiring soon".</summary>
    public int ExpiryAlertDays { get; private set; } = PharmacySettingsDefaults.ExpiryAlertDays;

    // Required by EF Core materialization.
    private PharmacySettings()
    {
    }

    private PharmacySettings(Guid id, Guid? createdBy)
        : base(id, createdBy)
    {
    }

    public static PharmacySettings Create(Guid? createdBy) => new(Guid.CreateVersion7(), createdBy);

    public void Update(
        string? pharmacyName, string? gstin, string? drugLicenceNo, string? address, string? phone, string? billFooter,
        BillPrintFormat printFormat, decimal maxDiscountPercent, int expiryAlertDays, Guid? updatedBy)
    {
        if (maxDiscountPercent is < 0 or > 100) throw new ArgumentOutOfRangeException(nameof(maxDiscountPercent), "Discount limit must be between 0 and 100.");
        if (expiryAlertDays is < 1 or > 730) throw new ArgumentOutOfRangeException(nameof(expiryAlertDays), "Expiry alert must be between 1 and 730 days.");

        PharmacyName = Clean(pharmacyName);
        Gstin = Clean(gstin)?.ToUpperInvariant();
        DrugLicenceNo = Clean(drugLicenceNo);
        Address = Clean(address);
        Phone = Clean(phone);
        BillFooter = Clean(billFooter);
        PrintFormat = printFormat;
        MaxDiscountPercent = maxDiscountPercent;
        ExpiryAlertDays = expiryAlertDays;
        MarkUpdated(updatedBy);
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
