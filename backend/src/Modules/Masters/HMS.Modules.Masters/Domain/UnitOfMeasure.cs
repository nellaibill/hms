using HMS.Shared.Kernel;

namespace HMS.Modules.Masters.Domain;

/// <summary>
/// Base/derived unit used for stock quantities (docs/03_Masters_ERD, "Units &amp; Tax"), e.g.
/// Box, Strip, Piece. <see cref="UomType"/> is free text (Count/Weight/Volume/...), matching
/// the UI's static-select convenience list rather than a separate lookup table.
/// </summary>
internal class UnitOfMeasure : Entity
{
    public string UomCode { get; private set; } = null!;
    public string UomName { get; private set; } = null!;
    public string? UomType { get; private set; }
    public bool IsBaseUnit { get; private set; }

    public bool IsActive { get; private set; } = true;

    private UnitOfMeasure()
    {
    }

    private UnitOfMeasure(Guid id, string uomCode, string uomName, string? uomType, bool isBaseUnit, bool isActive, Guid? createdBy)
        : base(id, createdBy)
    {
        UomCode = uomCode;
        UomName = uomName;
        UomType = uomType;
        IsBaseUnit = isBaseUnit;
        IsActive = isActive;
    }

    public static UnitOfMeasure Create(string uomCode, string uomName, string? uomType, bool isBaseUnit, bool isActive, Guid? createdBy)
    {
        Guard.AgainstNullOrWhiteSpace(uomCode, nameof(uomCode));
        Guard.AgainstNullOrWhiteSpace(uomName, nameof(uomName));

        return new UnitOfMeasure(Guid.CreateVersion7(), uomCode.Trim().ToUpperInvariant(), uomName.Trim(), uomType?.Trim(), isBaseUnit, isActive, createdBy);
    }

    public void Update(string uomName, string? uomType, bool isBaseUnit, bool isActive, Guid? updatedBy)
    {
        Guard.AgainstNullOrWhiteSpace(uomName, nameof(uomName));

        UomName = uomName.Trim();
        UomType = uomType?.Trim();
        IsBaseUnit = isBaseUnit;
        IsActive = isActive;
        MarkUpdated(updatedBy);
    }
}
