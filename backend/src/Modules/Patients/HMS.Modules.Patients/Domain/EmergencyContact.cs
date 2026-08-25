using HMS.Modules.Patients.Contracts;
using HMS.Shared.Kernel;

namespace HMS.Modules.Patients.Domain;

/// <summary>
/// One emergency contact — a patient can have more than one ("Add another Emergency
/// Contact" on the frontend), so this is a genuine 1:many child of <see cref="Patient"/>.
/// Every patient must have at least one (enforced by CreatePatientRequestValidator and, on
/// removal, by PatientService — a patient is never left with zero).
/// </summary>
internal class EmergencyContact
{
    public Guid Id { get; private set; }
    public Guid PatientId { get; private set; }

    public Relationship Relationship { get; private set; }
    public string Name { get; private set; } = null!;
    public string Phone { get; private set; } = null!;

    // Required by EF Core materialization.
    private EmergencyContact()
    {
    }

    private EmergencyContact(Guid id, Guid patientId, Relationship relationship, string name, string phone)
    {
        Id = id;
        PatientId = patientId;
        Relationship = relationship;
        Name = name;
        Phone = phone;
    }

    public static EmergencyContact Create(Guid patientId, Relationship relationship, string name, string phone)
    {
        Guard.AgainstNullOrWhiteSpace(name, nameof(name));
        Guard.AgainstNullOrWhiteSpace(phone, nameof(phone));

        return new EmergencyContact(Guid.CreateVersion7(), patientId, relationship, name.Trim(), phone.Trim());
    }
}
