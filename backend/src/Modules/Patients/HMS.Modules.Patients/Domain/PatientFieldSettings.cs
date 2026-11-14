using HMS.Shared.Kernel;

namespace HMS.Modules.Patients.Domain;

/// <summary>
/// One row per hospital database: which registration fields are mandatory (ADR-135). Null
/// until a Super Admin first saves Settings → Patient Registration Fields — the defaults
/// (Contracts.PatientFieldRequirements.Default, i.e. today's behaviour) stand in until then.
/// The choices are one serialized Contracts.PatientFieldRequirements in a jsonb column, so a
/// field added to the page later needs no migration.
/// </summary>
internal class PatientFieldSettings : Entity
{
    public string RequiredFieldsJson { get; private set; } = null!;

    // Required by EF Core materialization.
    private PatientFieldSettings()
    {
    }

    private PatientFieldSettings(Guid id, string requiredFieldsJson, Guid? createdBy)
        : base(id, createdBy)
    {
        RequiredFieldsJson = requiredFieldsJson;
    }

    public static PatientFieldSettings Create(string requiredFieldsJson, Guid? createdBy)
    {
        Guard.AgainstNullOrWhiteSpace(requiredFieldsJson, nameof(requiredFieldsJson));
        return new PatientFieldSettings(Guid.CreateVersion7(), requiredFieldsJson, createdBy);
    }

    public void Update(string requiredFieldsJson, Guid? updatedBy)
    {
        Guard.AgainstNullOrWhiteSpace(requiredFieldsJson, nameof(requiredFieldsJson));
        RequiredFieldsJson = requiredFieldsJson;
        MarkUpdated(updatedBy);
    }
}
