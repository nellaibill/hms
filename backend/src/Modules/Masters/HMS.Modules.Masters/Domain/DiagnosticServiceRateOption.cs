using HMS.Shared.Kernel;

namespace HMS.Modules.Masters.Domain;

/// <summary>
/// One named alternative rate on a Radiology <see cref="DiagnosticService"/> (ADR-140) — e.g.
/// "LH" or "MR" beside the service's own standard rate, as the hospital's tariff sheet prices
/// "ANC - Growth Scan", "ANC - Growth Scan - LH" and "ANC - Growth Scan - MR". Billing staff pick
/// the option on the bill line; the line keeps its id and name, and reports read the option's
/// doctor's share and running cost instead of the service's own.
/// </summary>
internal class DiagnosticServiceRateOption
{
    public const int NameMaxLength = 40;

    public Guid Id { get; private set; }
    public Guid DiagnosticServiceId { get; private set; }
    public string Name { get; private set; } = null!;

    /// <summary>What the patient pays when this option is billed.</summary>
    public decimal Price { get; private set; }

    /// <summary>The reporting doctor's share of <see cref="Price"/>.</summary>
    public decimal DoctorCharge { get; private set; }

    /// <summary>The hospital's own running cost (films, gel, power…) for this option.</summary>
    public decimal CostPrice { get; private set; }

    /// <summary>Display order on the service's edit screen and the billing dropdown.</summary>
    public int SortOrder { get; private set; }

    // Required by EF Core materialization.
    private DiagnosticServiceRateOption()
    {
    }

    private DiagnosticServiceRateOption(Guid id, Guid diagnosticServiceId)
    {
        Id = id;
        DiagnosticServiceId = diagnosticServiceId;
    }

    internal static DiagnosticServiceRateOption Create(Guid diagnosticServiceId, RateOptionValues values, int sortOrder)
    {
        var option = new DiagnosticServiceRateOption(Guid.CreateVersion7(), diagnosticServiceId);
        option.Apply(values, sortOrder);
        return option;
    }

    internal void Update(RateOptionValues values, int sortOrder) => Apply(values, sortOrder);

    private void Apply(RateOptionValues values, int sortOrder)
    {
        Guard.AgainstNullOrWhiteSpace(values.Name, nameof(values.Name));
        if (values.Price < 0 || values.DoctorCharge < 0 || values.CostPrice < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(values), "A rate option's price, doctor's share and running cost cannot be negative.");
        }

        Name = values.Name.Trim();
        Price = values.Price;
        DoctorCharge = values.DoctorCharge;
        CostPrice = values.CostPrice;
        SortOrder = sortOrder;
    }
}

/// <summary>A rate option as sent on create/update. <see cref="Id"/> is set for an option that
/// already exists (kept, so bill lines that point at it still resolve) and null for a new one.</summary>
internal sealed record RateOptionValues(Guid? Id, string Name, decimal Price, decimal DoctorCharge, decimal CostPrice);
