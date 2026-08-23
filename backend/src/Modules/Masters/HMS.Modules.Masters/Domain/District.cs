using HMS.Shared.Kernel;

namespace HMS.Modules.Masters.Domain;

/// <summary>
/// A district within an Indian state/union territory — read-only reference data for Patient
/// Registration's Address section, linked to its parent <see cref="State"/>. No admin CRUD
/// in this iteration: seeded once via
/// <see cref="Infrastructure.Configurations.DistrictConfiguration"/>'s HasData, same as
/// Gender/BloodGroup.
/// </summary>
internal class District : Entity
{
    public string Name { get; private set; } = null!;
    public Guid StateId { get; private set; }

    // Required by EF Core materialization.
    private District()
    {
    }

    private District(Guid id, string name, Guid stateId, Guid? createdBy)
        : base(id, createdBy)
    {
        Name = name;
        StateId = stateId;
    }

    public static District Create(string name, Guid stateId, Guid? createdBy)
    {
        Guard.AgainstNullOrWhiteSpace(name, nameof(name));

        return new District(Guid.CreateVersion7(), name.Trim(), stateId, createdBy);
    }
}
