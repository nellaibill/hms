using HMS.Shared.Kernel;

namespace HMS.Modules.Masters.Domain;

/// <summary>
/// Standalone lookup table seeded for future use — see the SaaS provisioning ADR in
/// docs/DecisionLog.md. Not referenced by Patient or any other entity yet; Patient keeps its
/// existing string-converted Gender enum unchanged in this branch.
/// </summary>
internal class Gender : Entity
{
    public string Code { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public bool IsActive { get; private set; } = true;

    // Required by EF Core materialization.
    private Gender()
    {
    }

    private Gender(Guid id, string code, string name, bool isActive, Guid? createdBy)
        : base(id, createdBy)
    {
        Code = code;
        Name = name;
        IsActive = isActive;
    }

    public static Gender Create(string code, string name, bool isActive, Guid? createdBy)
    {
        Guard.AgainstNullOrWhiteSpace(code, nameof(code));
        Guard.AgainstNullOrWhiteSpace(name, nameof(name));

        return new Gender(Guid.CreateVersion7(), code.Trim().ToUpperInvariant(), name.Trim(), isActive, createdBy);
    }
}
