using HMS.Modules.Patients.Contracts;
using HMS.Shared.Kernel;

namespace HMS.Modules.Patients.Domain;

/// <summary>
/// A patient's permanent master/demographic record — Form Sections 1-6 and 8 of
/// docs/PatientRegistrationModule.md. One <see cref="Patient"/> exists per person; each
/// visit is a separate <see cref="PatientRegistration"/> (Form Section 7). Deliberately
/// carries no duplicate-detection, merge, or billing fields in this iteration — see
/// docs/DecisionLog.md's MVP-scope ADR.
/// </summary>
internal class Patient : Entity
{
    public string Uhid { get; private set; } = null!;

    public Title Title { get; private set; }
    public string FirstName { get; private set; } = null!;
    public string LastName { get; private set; } = null!;
    public DateOnly DateOfBirth { get; private set; }
    public Gender Gender { get; private set; }
    public BloodGroup? BloodGroup { get; private set; }

    public string AddressLine1 { get; private set; } = null!;
    public string? AddressLine2 { get; private set; }
    public string? AddressLine3 { get; private set; }
    public string District { get; private set; } = null!;
    public string State { get; private set; } = null!;
    public string Pincode { get; private set; } = null!;

    public string PrimaryPhone { get; private set; } = null!;
    public string? PrimaryPhoneRelation { get; private set; }
    public string? AlternatePhone { get; private set; }
    public string? Email { get; private set; }
    public string? Profession { get; private set; }

    public string EmergencyContactRelationship { get; private set; } = null!;
    public string EmergencyContactName { get; private set; } = null!;
    public string EmergencyContactPhone { get; private set; } = null!;

    public bool HasKnownAllergy { get; private set; }
    public string? AllergyType { get; private set; }
    public AllergySeverity? AllergySeverity { get; private set; }

    public string? PhotoPath { get; private set; }
    public IdProofType? IdProofType { get; private set; }
    public string? IdProofPath { get; private set; }

    private readonly List<PatientRegistration> _registrations = [];
    public IReadOnlyCollection<PatientRegistration> Registrations => _registrations.AsReadOnly();

    /// <summary>Age is always derived from <see cref="DateOfBirth"/>, never stored (BRA Entity List).</summary>
    public int Age => CalculateAge(DateOfBirth, DateOnly.FromDateTime(DateTime.UtcNow));

    // Required by EF Core materialization.
    private Patient()
    {
    }

    private Patient(
        Guid id,
        string uhid,
        Title title,
        string firstName,
        string lastName,
        DateOnly dateOfBirth,
        Gender gender,
        BloodGroup? bloodGroup,
        string addressLine1,
        string? addressLine2,
        string? addressLine3,
        string district,
        string state,
        string pincode,
        string primaryPhone,
        string? primaryPhoneRelation,
        string? alternatePhone,
        string? email,
        string? profession,
        string emergencyContactRelationship,
        string emergencyContactName,
        string emergencyContactPhone,
        bool hasKnownAllergy,
        string? allergyType,
        AllergySeverity? allergySeverity,
        Guid? createdBy)
        : base(id, createdBy)
    {
        Uhid = uhid;
        Title = title;
        FirstName = firstName;
        LastName = lastName;
        DateOfBirth = dateOfBirth;
        Gender = gender;
        BloodGroup = bloodGroup;
        AddressLine1 = addressLine1;
        AddressLine2 = addressLine2;
        AddressLine3 = addressLine3;
        District = district;
        State = state;
        Pincode = pincode;
        PrimaryPhone = primaryPhone;
        PrimaryPhoneRelation = primaryPhoneRelation;
        AlternatePhone = alternatePhone;
        Email = email;
        Profession = profession;
        EmergencyContactRelationship = emergencyContactRelationship;
        EmergencyContactName = emergencyContactName;
        EmergencyContactPhone = emergencyContactPhone;
        HasKnownAllergy = hasKnownAllergy;
        AllergyType = allergyType;
        AllergySeverity = allergySeverity;
    }

    public static Patient Create(
        string uhid,
        Title title,
        string firstName,
        string lastName,
        DateOnly dateOfBirth,
        Gender gender,
        BloodGroup? bloodGroup,
        string addressLine1,
        string? addressLine2,
        string? addressLine3,
        string district,
        string state,
        string pincode,
        string primaryPhone,
        string? primaryPhoneRelation,
        string? alternatePhone,
        string? email,
        string? profession,
        string emergencyContactRelationship,
        string emergencyContactName,
        string emergencyContactPhone,
        bool hasKnownAllergy,
        string? allergyType,
        AllergySeverity? allergySeverity,
        Guid? createdBy)
    {
        Guard.AgainstNullOrWhiteSpace(uhid, nameof(uhid));
        Guard.AgainstNullOrWhiteSpace(firstName, nameof(firstName));
        Guard.AgainstNullOrWhiteSpace(lastName, nameof(lastName));
        Guard.AgainstNullOrWhiteSpace(addressLine1, nameof(addressLine1));
        Guard.AgainstNullOrWhiteSpace(district, nameof(district));
        Guard.AgainstNullOrWhiteSpace(state, nameof(state));
        Guard.AgainstNullOrWhiteSpace(pincode, nameof(pincode));
        Guard.AgainstNullOrWhiteSpace(primaryPhone, nameof(primaryPhone));
        Guard.AgainstNullOrWhiteSpace(emergencyContactRelationship, nameof(emergencyContactRelationship));
        Guard.AgainstNullOrWhiteSpace(emergencyContactName, nameof(emergencyContactName));
        Guard.AgainstNullOrWhiteSpace(emergencyContactPhone, nameof(emergencyContactPhone));

        return new Patient(
            Guid.CreateVersion7(),
            uhid.Trim(),
            title,
            firstName.Trim(),
            lastName.Trim(),
            dateOfBirth,
            gender,
            bloodGroup,
            addressLine1.Trim(),
            addressLine2?.Trim(),
            addressLine3?.Trim(),
            district.Trim(),
            state.Trim(),
            pincode.Trim(),
            primaryPhone.Trim(),
            primaryPhoneRelation?.Trim(),
            alternatePhone?.Trim(),
            email?.Trim().ToLowerInvariant(),
            profession?.Trim(),
            emergencyContactRelationship.Trim(),
            emergencyContactName.Trim(),
            emergencyContactPhone.Trim(),
            hasKnownAllergy,
            allergyType?.Trim(),
            allergySeverity,
            createdBy);
    }

    public void UpdateDemographics(Title title, string firstName, string lastName, DateOnly dateOfBirth, Gender gender, BloodGroup? bloodGroup, Guid? updatedBy)
    {
        Guard.AgainstNullOrWhiteSpace(firstName, nameof(firstName));
        Guard.AgainstNullOrWhiteSpace(lastName, nameof(lastName));

        Title = title;
        FirstName = firstName.Trim();
        LastName = lastName.Trim();
        DateOfBirth = dateOfBirth;
        Gender = gender;
        BloodGroup = bloodGroup;
        MarkUpdated(updatedBy);
    }

    public void UpdateAddress(string addressLine1, string? addressLine2, string? addressLine3, string district, string state, string pincode, Guid? updatedBy)
    {
        Guard.AgainstNullOrWhiteSpace(addressLine1, nameof(addressLine1));
        Guard.AgainstNullOrWhiteSpace(district, nameof(district));
        Guard.AgainstNullOrWhiteSpace(state, nameof(state));
        Guard.AgainstNullOrWhiteSpace(pincode, nameof(pincode));

        AddressLine1 = addressLine1.Trim();
        AddressLine2 = addressLine2?.Trim();
        AddressLine3 = addressLine3?.Trim();
        District = district.Trim();
        State = state.Trim();
        Pincode = pincode.Trim();
        MarkUpdated(updatedBy);
    }

    public void UpdateContact(string primaryPhone, string? primaryPhoneRelation, string? alternatePhone, string? email, string? profession, Guid? updatedBy)
    {
        Guard.AgainstNullOrWhiteSpace(primaryPhone, nameof(primaryPhone));

        PrimaryPhone = primaryPhone.Trim();
        PrimaryPhoneRelation = primaryPhoneRelation?.Trim();
        AlternatePhone = alternatePhone?.Trim();
        Email = email?.Trim().ToLowerInvariant();
        Profession = profession?.Trim();
        MarkUpdated(updatedBy);
    }

    public void UpdateEmergencyContact(string relationship, string name, string phone, Guid? updatedBy)
    {
        Guard.AgainstNullOrWhiteSpace(relationship, nameof(relationship));
        Guard.AgainstNullOrWhiteSpace(name, nameof(name));
        Guard.AgainstNullOrWhiteSpace(phone, nameof(phone));

        EmergencyContactRelationship = relationship.Trim();
        EmergencyContactName = name.Trim();
        EmergencyContactPhone = phone.Trim();
        MarkUpdated(updatedBy);
    }

    public void UpdateAllergyDetails(bool hasKnownAllergy, string? allergyType, AllergySeverity? allergySeverity, Guid? updatedBy)
    {
        HasKnownAllergy = hasKnownAllergy;
        AllergyType = hasKnownAllergy ? allergyType?.Trim() : null;
        AllergySeverity = hasKnownAllergy ? allergySeverity : null;
        MarkUpdated(updatedBy);
    }

    public void SetPhoto(string photoPath, Guid? updatedBy)
    {
        Guard.AgainstNullOrWhiteSpace(photoPath, nameof(photoPath));
        PhotoPath = photoPath;
        MarkUpdated(updatedBy);
    }

    public void SetIdProof(IdProofType idProofType, string idProofPath, Guid? updatedBy)
    {
        Guard.AgainstNullOrWhiteSpace(idProofPath, nameof(idProofPath));
        IdProofType = idProofType;
        IdProofPath = idProofPath;
        MarkUpdated(updatedBy);
    }

    /// <summary>Called only by <see cref="PatientRegistration"/> construction in the same use case — see PatientService.CreateAsync.</summary>
    public void AddRegistration(PatientRegistration registration) => _registrations.Add(registration);

    private static int CalculateAge(DateOnly dateOfBirth, DateOnly asOf)
    {
        var age = asOf.Year - dateOfBirth.Year;
        if (dateOfBirth > asOf.AddYears(-age))
        {
            age--;
        }

        return age;
    }
}
