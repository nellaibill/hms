using HMS.Shared.Kernel;

namespace HMS.Modules.Masters.Domain;

/// <summary>
/// An external organization a <see cref="DiagnosticService"/> can be outsourced to (e.g.
/// "Q-LAB") — part of the normalized replacement for the old flat DiagnosticTest.ReferenceLab
/// free-text string. Named generically ("Provider", not "Reference Lab") because Radiology can
/// outsource to an external imaging center just as Laboratory outsources to an external
/// pathology lab — this one entity covers both.
/// </summary>
internal class DiagnosticProvider : Entity
{
    public string Code { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public string? ContactDetails { get; private set; }
    public bool IsActive { get; private set; } = true;

    // Required by EF Core materialization.
    private DiagnosticProvider()
    {
    }

    private DiagnosticProvider(
        Guid id,
        string code,
        string name,
        string? contactDetails,
        bool isActive,
        Guid? createdBy)
        : base(id, createdBy)
    {
        Code = code;
        Name = name;
        ContactDetails = contactDetails;
        IsActive = isActive;
    }

    public static DiagnosticProvider Create(
        string code,
        string name,
        string? contactDetails,
        bool isActive,
        Guid? createdBy)
    {
        Guard.AgainstNullOrWhiteSpace(code, nameof(code));
        Guard.AgainstNullOrWhiteSpace(name, nameof(name));

        return new DiagnosticProvider(
            Guid.CreateVersion7(),
            code.Trim(),
            name.Trim(),
            contactDetails?.Trim(),
            isActive,
            createdBy);
    }

    public void Update(
        string code,
        string name,
        string? contactDetails,
        bool isActive,
        Guid? updatedBy)
    {
        Guard.AgainstNullOrWhiteSpace(code, nameof(code));
        Guard.AgainstNullOrWhiteSpace(name, nameof(name));

        Code = code.Trim();
        Name = name.Trim();
        ContactDetails = contactDetails?.Trim();
        IsActive = isActive;
        MarkUpdated(updatedBy);
    }
}
