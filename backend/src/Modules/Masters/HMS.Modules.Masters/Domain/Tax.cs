using HMS.Shared.Kernel;

namespace HMS.Modules.Masters.Domain;

/// <summary>Tax rate applied to purchases and sales (docs/03_Masters_ERD, "Units &amp; Tax"), e.g. GST, VAT.</summary>
internal class Tax : Entity
{
    public string TaxCode { get; private set; } = null!;
    public string TaxName { get; private set; } = null!;
    public string? TaxType { get; private set; }
    public decimal RatePercent { get; private set; }
    public bool IsInclusive { get; private set; }

    public bool IsActive { get; private set; } = true;

    private Tax()
    {
    }

    private Tax(Guid id, string taxCode, string taxName, string? taxType, decimal ratePercent, bool isInclusive, bool isActive, Guid? createdBy)
        : base(id, createdBy)
    {
        TaxCode = taxCode;
        TaxName = taxName;
        TaxType = taxType;
        RatePercent = ratePercent;
        IsInclusive = isInclusive;
        IsActive = isActive;
    }

    public static Tax Create(string taxCode, string taxName, string? taxType, decimal ratePercent, bool isInclusive, bool isActive, Guid? createdBy)
    {
        Guard.AgainstNullOrWhiteSpace(taxCode, nameof(taxCode));
        Guard.AgainstNullOrWhiteSpace(taxName, nameof(taxName));
        if (ratePercent <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(ratePercent), "Tax rate must be greater than 0.");
        }

        return new Tax(Guid.CreateVersion7(), taxCode.Trim().ToUpperInvariant(), taxName.Trim(), taxType?.Trim(), ratePercent, isInclusive, isActive, createdBy);
    }

    public void Update(string taxName, string? taxType, decimal ratePercent, bool isInclusive, bool isActive, Guid? updatedBy)
    {
        Guard.AgainstNullOrWhiteSpace(taxName, nameof(taxName));
        if (ratePercent <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(ratePercent), "Tax rate must be greater than 0.");
        }

        TaxName = taxName.Trim();
        TaxType = taxType?.Trim();
        RatePercent = ratePercent;
        IsInclusive = isInclusive;
        IsActive = isActive;
        MarkUpdated(updatedBy);
    }
}
