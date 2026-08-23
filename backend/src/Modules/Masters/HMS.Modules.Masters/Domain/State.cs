using HMS.Shared.Kernel;

namespace HMS.Modules.Masters.Domain;

/// <summary>
/// An Indian state/union territory — read-only reference data for Patient Registration's
/// Address section (see docs/DecisionLog.md). No admin CRUD in this iteration: seeded once
/// via <see cref="Infrastructure.Configurations.StateConfiguration"/>'s HasData, same as
/// Gender/BloodGroup.
/// </summary>
internal class State : Entity
{
    public string Name { get; private set; } = null!;

    // Required by EF Core materialization.
    private State()
    {
    }

    private State(Guid id, string name, Guid? createdBy)
        : base(id, createdBy)
    {
        Name = name;
    }

    public static State Create(string name, Guid? createdBy)
    {
        Guard.AgainstNullOrWhiteSpace(name, nameof(name));

        return new State(Guid.CreateVersion7(), name.Trim(), createdBy);
    }
}
