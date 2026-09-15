using HMS.Shared.Kernel;

namespace HMS.Modules.Masters.Domain;

/// <summary>
/// A hospital-maintained diagnosis catalog entry (e.g. "Osteoarthritis of knee") for the OPD
/// Consultation form's Diagnosis section — deliberately not a real ICD-10 import (tens of
/// thousands of codes, a separate undertaking); this grows as staff use it, the same
/// admin-managed-catalog convention already used for ConsultationType/AppointmentType.
/// IcdCode is optional since a hospital may record a diagnosis before it's been coded, or never
/// code some at all.
/// </summary>
internal class Diagnosis : Entity
{
    public string Name { get; private set; } = null!;
    public string? IcdCode { get; private set; }
    public bool IsActive { get; private set; } = true;

    // Required by EF Core materialization.
    private Diagnosis()
    {
    }

    private Diagnosis(Guid id, string name, string? icdCode, bool isActive, Guid? createdBy)
        : base(id, createdBy)
    {
        Name = name;
        IcdCode = icdCode;
        IsActive = isActive;
    }

    public static Diagnosis Create(string name, string? icdCode, bool isActive, Guid? createdBy)
    {
        Guard.AgainstNullOrWhiteSpace(name, nameof(name));
        return new Diagnosis(Guid.CreateVersion7(), name.Trim(), NormalizeIcdCode(icdCode), isActive, createdBy);
    }

    public void Update(string name, string? icdCode, bool isActive, Guid? updatedBy)
    {
        Guard.AgainstNullOrWhiteSpace(name, nameof(name));
        Name = name.Trim();
        IcdCode = NormalizeIcdCode(icdCode);
        IsActive = isActive;
        MarkUpdated(updatedBy);
    }

    private static string? NormalizeIcdCode(string? icdCode)
        => string.IsNullOrWhiteSpace(icdCode) ? null : icdCode.Trim().ToUpperInvariant();
}
