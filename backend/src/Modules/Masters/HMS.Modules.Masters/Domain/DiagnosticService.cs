using HMS.Modules.Masters.Contracts;
using HMS.Shared.Kernel;

namespace HMS.Modules.Masters.Domain;

/// <summary>
/// A billable laboratory or radiology test (e.g. "Complete Blood Count", "Digital X-ray") with
/// its standard patient-facing price — the normalized replacement for the Laboratory/Radiology
/// half of the old flat DiagnosticTest entity. Procedure-type billing stays on DiagnosticTest;
/// <see cref="ServiceType"/> here is only ever Laboratory or Radiology (enforced by
/// CreateDiagnosticServiceRequestValidator/UpdateDiagnosticServiceRequestValidator, not by this
/// entity itself, which reuses DiagnosticTestServiceType rather than a third near-identical
/// enum). CategoryId/ProviderId are app-level references into DiagnosticCategory/
/// DiagnosticProvider — validated by DiagnosticServiceService, not enforced by a database
/// foreign key, same convention as DiagnosticTest's siblings elsewhere in this module.
/// </summary>
internal class DiagnosticService : Entity
{
    public string Code { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public Guid CategoryId { get; private set; }
    public DiagnosticTestServiceType ServiceType { get; private set; }
    public bool IsOutsourced { get; private set; }
    public Guid? ProviderId { get; private set; }
    public decimal Price { get; private set; }

    /// <summary>What it actually costs the hospital to run this test once (reagents,
    /// consumables, an allocated share of equipment/staff time) — as distinct from
    /// <see cref="Price"/>, what the patient is billed. Defaults to 0 (unknown/not yet costed)
    /// rather than being required, matching DiagnosticTest.CostPrice's reasoning.</summary>
    public decimal CostPrice { get; private set; }

    public bool IsActive { get; private set; } = true;

    /// <summary>The reporting doctor's share of <see cref="Price"/> (ADR-140) — Radiology only;
    /// always 0 for a Laboratory test, which has no doctor's share. Kept apart from
    /// <see cref="CostPrice"/> (the hospital's own running cost) so the price list and reports
    /// can show both. Profit = Price − DoctorCharge − CostPrice.</summary>
    public decimal DoctorCharge { get; private set; }

    /// <summary>What a bedside scan adds to the price (ADR-140) — null when the service isn't
    /// done at the bedside. Set together with <see cref="BedsideDoctorCharge"/>.</summary>
    public decimal? BedsidePrice { get; private set; }

    /// <summary>What a bedside scan adds to the doctor's share — null exactly when
    /// <see cref="BedsidePrice"/> is.</summary>
    public decimal? BedsideDoctorCharge { get; private set; }

    /// <summary>Named alternative rates billed instead of the standard one (Radiology only,
    /// ADR-140). Updated in place by id on every save (see <see cref="SetTariff"/>), so an option
    /// a bill line points at keeps its id.</summary>
    private readonly List<DiagnosticServiceRateOption> _rateOptions = [];
    public IReadOnlyCollection<DiagnosticServiceRateOption> RateOptions => _rateOptions;

    // Required by EF Core materialization.
    private DiagnosticService()
    {
    }

    private DiagnosticService(
        Guid id,
        string code,
        string name,
        Guid categoryId,
        DiagnosticTestServiceType serviceType,
        bool isOutsourced,
        Guid? providerId,
        decimal price,
        decimal costPrice,
        bool isActive,
        Guid? createdBy)
        : base(id, createdBy)
    {
        Code = code;
        Name = name;
        CategoryId = categoryId;
        ServiceType = serviceType;
        IsOutsourced = isOutsourced;
        ProviderId = providerId;
        Price = price;
        CostPrice = costPrice;
        IsActive = isActive;
    }

    public static DiagnosticService Create(
        string code,
        string name,
        Guid categoryId,
        DiagnosticTestServiceType serviceType,
        bool isOutsourced,
        Guid? providerId,
        decimal price,
        decimal costPrice,
        bool isActive,
        Guid? createdBy)
    {
        Guard.AgainstNullOrWhiteSpace(code, nameof(code));
        Guard.AgainstNullOrWhiteSpace(name, nameof(name));
        if (price < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(price), "Price cannot be negative.");
        }
        if (costPrice < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(costPrice), "Cost price cannot be negative.");
        }

        return new DiagnosticService(
            Guid.CreateVersion7(),
            code.Trim(),
            name.Trim(),
            categoryId,
            serviceType,
            isOutsourced,
            providerId,
            price,
            costPrice,
            isActive,
            createdBy);
    }

    public void Update(
        string code,
        string name,
        Guid categoryId,
        DiagnosticTestServiceType serviceType,
        bool isOutsourced,
        Guid? providerId,
        decimal price,
        decimal costPrice,
        bool isActive,
        Guid? updatedBy)
    {
        Guard.AgainstNullOrWhiteSpace(code, nameof(code));
        Guard.AgainstNullOrWhiteSpace(name, nameof(name));
        if (price < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(price), "Price cannot be negative.");
        }
        if (costPrice < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(costPrice), "Cost price cannot be negative.");
        }

        Code = code.Trim();
        Name = name.Trim();
        CategoryId = categoryId;
        ServiceType = serviceType;
        IsOutsourced = isOutsourced;
        ProviderId = providerId;
        Price = price;
        CostPrice = costPrice;
        IsActive = isActive;
        MarkUpdated(updatedBy);
    }

    /// <summary>
    /// Sets the Radiology-only tariff parts (ADR-140): the doctor's share, the bedside add-on and
    /// the rate options. A Laboratory test takes none of them. Rate options are matched by id —
    /// an existing option is updated in place, a new one (no id, or an id this service doesn't
    /// have) is added, and an option left out is removed.
    /// </summary>
    public void SetTariff(decimal doctorCharge, decimal? bedsidePrice, decimal? bedsideDoctorCharge, IReadOnlyList<RateOptionValues> rateOptions)
    {
        if (doctorCharge < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(doctorCharge), "Doctor's share cannot be negative.");
        }
        if (bedsidePrice is < 0 || bedsideDoctorCharge is < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(bedsidePrice), "The bedside add-on cannot be negative.");
        }
        if (bedsidePrice.HasValue != bedsideDoctorCharge.HasValue)
        {
            throw new ArgumentException("Set both parts of the bedside add-on, or neither.", nameof(bedsidePrice));
        }
        if (ServiceType == DiagnosticTestServiceType.Laboratory && (doctorCharge != 0 || bedsidePrice.HasValue || rateOptions.Count > 0))
        {
            throw new ArgumentException("A lab test has no doctor's share, bedside add-on or rate options.", nameof(doctorCharge));
        }

        DoctorCharge = doctorCharge;
        BedsidePrice = bedsidePrice;
        BedsideDoctorCharge = bedsideDoctorCharge;

        var kept = new HashSet<Guid>();
        for (var index = 0; index < rateOptions.Count; index++)
        {
            var values = rateOptions[index];
            var existing = values.Id.HasValue ? _rateOptions.FirstOrDefault(option => option.Id == values.Id.Value) : null;
            if (existing is not null)
            {
                existing.Update(values, index);
                kept.Add(existing.Id);
            }
            else
            {
                var option = DiagnosticServiceRateOption.Create(Id, values, index);
                _rateOptions.Add(option);
                kept.Add(option.Id);
            }
        }
        _rateOptions.RemoveAll(option => !kept.Contains(option.Id));
    }
}
