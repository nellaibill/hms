using HMS.Shared.Kernel;

namespace HMS.Modules.Masters.Domain;

/// <summary>
/// A patient registration's consultation category (e.g. "Doctor's Consultation (In-house) -
/// Regular") with its standard fee — shared reference data for Patients (Registration
/// Details' optional Consultation Type), matching the AppointmentType consolidation pattern.
/// Amount is nullable: some categories (e.g. "Others / On-call") have no fixed fee — it's
/// decided per-visit rather than published as a standard rate, so this stays unset rather
/// than being forced to a misleading 0.
/// </summary>
internal class ConsultationType : Entity
{
    public string Name { get; private set; } = null!;

    /// <summary>
    /// Optional short label for reports (e.g. "Regular", "Priority", "Emergency") — the full
    /// Name stays on bills and registration. Null when the hospital hasn't set one; reports then
    /// fall back to Name (ADR-127).
    /// </summary>
    public string? ShortName { get; private set; }

    public decimal? Amount { get; private set; }

    public bool IsActive { get; private set; } = true;

    // Required by EF Core materialization.
    private ConsultationType()
    {
    }

    private ConsultationType(
        Guid id,
        string name,
        string? shortName,
        decimal? amount,
        bool isActive,
        Guid? createdBy)
        : base(id, createdBy)
    {
        Name = name;
        ShortName = shortName;
        Amount = amount;
        IsActive = isActive;
    }

    public static ConsultationType Create(
        string name,
        string? shortName,
        decimal? amount,
        bool isActive,
        Guid? createdBy)
    {
        Guard.AgainstNullOrWhiteSpace(name, nameof(name));
        if (amount is < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "Amount cannot be negative.");
        }

        return new ConsultationType(
            Guid.CreateVersion7(),
            name.Trim(),
            NormalizeShortName(shortName),
            amount,
            isActive,
            createdBy);
    }

    public void Update(
        string name,
        string? shortName,
        decimal? amount,
        bool isActive,
        Guid? updatedBy)
    {
        Guard.AgainstNullOrWhiteSpace(name, nameof(name));
        if (amount is < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "Amount cannot be negative.");
        }

        Name = name.Trim();
        ShortName = NormalizeShortName(shortName);
        Amount = amount;
        IsActive = isActive;
        MarkUpdated(updatedBy);
    }

    private static string? NormalizeShortName(string? shortName) =>
        string.IsNullOrWhiteSpace(shortName) ? null : shortName.Trim();
}
