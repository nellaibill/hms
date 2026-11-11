using HMS.Modules.Patients.Contracts;

namespace HMS.Modules.Patients.Domain;

/// <summary>
/// One allergy entry — a patient can have several ("Add another Allergy" on the frontend),
/// so this is a genuine 1:many child of <see cref="Patient"/>, not a flattened field.
/// </summary>
internal class Allergy
{
    public Guid Id { get; private set; }
    public Guid PatientId { get; private set; }

    public AllergyType AllergyType { get; private set; }
    public string? Specify { get; private set; }
    public AllergySeverity Severity { get; private set; }

    // Required by EF Core materialization.
    private Allergy()
    {
    }

    private Allergy(Guid id, Guid patientId, AllergyType allergyType, string? specify, AllergySeverity severity)
    {
        Id = id;
        PatientId = patientId;
        AllergyType = allergyType;
        Specify = specify;
        Severity = severity;
    }

    public static Allergy Create(Guid patientId, AllergyType allergyType, string? specify, AllergySeverity severity)
        => new(Guid.CreateVersion7(), patientId, allergyType, specify?.Trim(), severity);

    /// <summary>Offline-first sync (ADR-133) only: recreates an allergy with its original id.</summary>
    internal static Allergy CreateReplica(Guid id, Guid patientId, AllergyType allergyType, string? specify, AllergySeverity severity)
        => new(id, patientId, allergyType, specify, severity);

    /// <summary>Offline-first sync (ADR-133) only.</summary>
    internal void ApplyReplicaState(AllergyType allergyType, string? specify, AllergySeverity severity)
    {
        AllergyType = allergyType;
        Specify = specify;
        Severity = severity;
    }
}
