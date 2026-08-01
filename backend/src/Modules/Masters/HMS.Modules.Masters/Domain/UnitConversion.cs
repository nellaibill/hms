using HMS.Shared.Kernel;

namespace HMS.Modules.Masters.Domain;

/// <summary>
/// Conversion factor between two <see cref="UnitOfMeasure"/> records (docs/03_Masters_ERD,
/// "Units &amp; Tax"), e.g. 1 Box = 10 Strip. Has no natural code/name of its own — displayed
/// as "{FromUom} → {ToUom}" by callers, mirroring the frontend's getDisplayLabel.
/// </summary>
internal class UnitConversion : Entity
{
    public Guid FromUomId { get; private set; }
    public Guid ToUomId { get; private set; }
    public decimal ConversionFactor { get; private set; }

    public bool IsActive { get; private set; } = true;

    private UnitConversion()
    {
    }

    private UnitConversion(Guid id, Guid fromUomId, Guid toUomId, decimal conversionFactor, bool isActive, Guid? createdBy)
        : base(id, createdBy)
    {
        FromUomId = fromUomId;
        ToUomId = toUomId;
        ConversionFactor = conversionFactor;
        IsActive = isActive;
    }

    public static UnitConversion Create(Guid fromUomId, Guid toUomId, decimal conversionFactor, bool isActive, Guid? createdBy)
    {
        if (fromUomId == toUomId)
        {
            throw new ArgumentException("From Unit and To Unit cannot be the same.");
        }

        if (conversionFactor <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(conversionFactor), "Conversion factor must be greater than 0.");
        }

        return new UnitConversion(Guid.CreateVersion7(), fromUomId, toUomId, conversionFactor, isActive, createdBy);
    }

    public void Update(decimal conversionFactor, bool isActive, Guid? updatedBy)
    {
        if (conversionFactor <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(conversionFactor), "Conversion factor must be greater than 0.");
        }

        ConversionFactor = conversionFactor;
        IsActive = isActive;
        MarkUpdated(updatedBy);
    }
}
