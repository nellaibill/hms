namespace HMS.Modules.Pharmacy.Contracts;

/// <summary>What a hospital's pharmacy settings are until someone saves them. The settings
/// screen shows these, and everything that reads a setting falls back to the same value.</summary>
public static class PharmacySettingsDefaults
{
    /// <summary>How many days ahead a batch counts as "expiring soon".</summary>
    public const int ExpiryAlertDays = 90;
}

/// <summary>The paper a pharmacy bill is printed on.</summary>
public enum BillPrintFormat
{
    /// <summary>Half-sheet GST invoice, the usual printed bill.</summary>
    A5,

    /// <summary>Full-sheet GST invoice.</summary>
    A4,

    /// <summary>80 mm thermal roll receipt.</summary>
    Thermal80,
}

public record PharmacySettingsResponse
{
    /// <summary>Printed at the head of the bill. Null prints the hospital's name.</summary>
    public string? PharmacyName { get; init; }
    public string? Gstin { get; init; }
    public string? DrugLicenceNo { get; init; }
    public string? Address { get; init; }
    public string? Phone { get; init; }
    public string? BillFooter { get; init; }
    public BillPrintFormat PrintFormat { get; init; } = BillPrintFormat.A5;
    public decimal MaxDiscountPercent { get; init; } = 100m;
    public int ExpiryAlertDays { get; init; } = PharmacySettingsDefaults.ExpiryAlertDays;
}

public record UpdatePharmacySettingsRequest
{
    public string? PharmacyName { get; init; }
    public string? Gstin { get; init; }
    public string? DrugLicenceNo { get; init; }
    public string? Address { get; init; }
    public string? Phone { get; init; }
    public string? BillFooter { get; init; }
    public BillPrintFormat PrintFormat { get; init; } = BillPrintFormat.A5;
    public decimal MaxDiscountPercent { get; init; } = 100m;
    public int ExpiryAlertDays { get; init; } = PharmacySettingsDefaults.ExpiryAlertDays;
}
