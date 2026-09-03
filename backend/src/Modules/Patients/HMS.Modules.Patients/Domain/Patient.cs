using HMS.Modules.Patients.Contracts;
using HMS.Shared.Kernel;

namespace HMS.Modules.Patients.Domain;

/// <summary>
/// A patient's master/demographic record — the aggregate root for this module. Address is a
/// true 1:1 child (always present, created together with the patient); Allergies and
/// EmergencyContacts are 1:many children ("Add another ..." on the frontend). All three are
/// only ever loaded/saved through this aggregate — see Infrastructure/Repositories/
/// PatientRepository.cs. Encounter/visit data (department, consultant, admission type) is
/// deliberately out of scope for this iteration — see the rebuild plan's Context section.
/// </summary>
internal class Patient : Entity
{
    public string Uhid { get; private set; } = null!;

    public Title Title { get; private set; }
    public string FirstName { get; private set; } = null!;
    public string LastName { get; private set; } = null!;
    public DateOnly DateOfBirth { get; private set; }
    public Gender Gender { get; private set; }
    public BloodGroup BloodGroup { get; private set; }
    public MaritalStatus MaritalStatus { get; private set; }

    public string PrimaryPhone { get; private set; } = null!;
    public string? SecondaryPhone { get; private set; }
    public string? Email { get; private set; }
    public string? Profession { get; private set; }

    public IdProofType? IdProofType { get; private set; }
    public string? IdProofNumber { get; private set; }

    public ModeOfArrivalSource ModeOfArrivalSource { get; private set; }
    public string? ModeOfArrivalChannel { get; private set; }
    public string? ModeOfArrivalSpecify { get; private set; }

    public Address Address { get; private set; } = null!;

    /// <summary>True when this patient was created with placeholder/default values for
    /// required fields (currently: bulk import — see PatientImportCommitBackgroundService),
    /// which are technically valid but not real data (e.g. a synthetic phone number, a
    /// sentinel date of birth). Front-end surfaces a "verify this patient's details" banner
    /// while true, cleared automatically the next time anyone saves an edit through
    /// UpdateAsync — matches how a receptionist naturally re-confirms details with the patient
    /// once they're back in front of them.</summary>
    public bool RequiresDataVerification { get; private set; }

    private readonly List<Allergy> _allergies = [];
    public IReadOnlyCollection<Allergy> Allergies => _allergies.AsReadOnly();

    private readonly List<EmergencyContact> _emergencyContacts = [];
    public IReadOnlyCollection<EmergencyContact> EmergencyContacts => _emergencyContacts.AsReadOnly();

    /// <summary>Age is always derived from <see cref="DateOfBirth"/>, never stored.</summary>
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
        BloodGroup bloodGroup,
        MaritalStatus maritalStatus,
        string primaryPhone,
        string? secondaryPhone,
        string? email,
        string? profession,
        IdProofType? idProofType,
        string? idProofNumber,
        ModeOfArrivalSource modeOfArrivalSource,
        string? modeOfArrivalChannel,
        string? modeOfArrivalSpecify,
        bool requiresDataVerification,
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
        MaritalStatus = maritalStatus;
        PrimaryPhone = primaryPhone;
        SecondaryPhone = secondaryPhone;
        Email = email;
        Profession = profession;
        IdProofType = idProofType;
        IdProofNumber = idProofNumber;
        ModeOfArrivalSource = modeOfArrivalSource;
        ModeOfArrivalChannel = modeOfArrivalChannel;
        ModeOfArrivalSpecify = modeOfArrivalSpecify;
        RequiresDataVerification = requiresDataVerification;
    }

    public static Patient Create(
        string uhid,
        Title title,
        string firstName,
        string lastName,
        DateOnly dateOfBirth,
        Gender gender,
        BloodGroup bloodGroup,
        MaritalStatus maritalStatus,
        string primaryPhone,
        string? secondaryPhone,
        string? email,
        string? profession,
        IdProofType? idProofType,
        string? idProofNumber,
        ModeOfArrivalSource modeOfArrivalSource,
        string? modeOfArrivalChannel,
        string? modeOfArrivalSpecify,
        Guid? createdBy,
        bool requiresDataVerification = false)
    {
        Guard.AgainstNullOrWhiteSpace(uhid, nameof(uhid));
        Guard.AgainstNullOrWhiteSpace(firstName, nameof(firstName));
        Guard.AgainstNullOrWhiteSpace(lastName, nameof(lastName));
        Guard.AgainstNullOrWhiteSpace(primaryPhone, nameof(primaryPhone));

        return new Patient(
            Guid.CreateVersion7(),
            uhid.Trim(),
            title,
            firstName.Trim(),
            lastName.Trim(),
            dateOfBirth,
            gender,
            bloodGroup,
            maritalStatus,
            primaryPhone.Trim(),
            secondaryPhone?.Trim(),
            email?.Trim().ToLowerInvariant(),
            profession?.Trim(),
            idProofType,
            idProofNumber?.Trim(),
            modeOfArrivalSource,
            modeOfArrivalChannel?.Trim(),
            modeOfArrivalSpecify?.Trim(),
            requiresDataVerification,
            createdBy);
    }

    public void UpdateDemographics(Title title, string firstName, string lastName, DateOnly dateOfBirth, Gender gender, BloodGroup bloodGroup, MaritalStatus maritalStatus, Guid? updatedBy)
    {
        Guard.AgainstNullOrWhiteSpace(firstName, nameof(firstName));
        Guard.AgainstNullOrWhiteSpace(lastName, nameof(lastName));

        Title = title;
        FirstName = firstName.Trim();
        LastName = lastName.Trim();
        DateOfBirth = dateOfBirth;
        Gender = gender;
        BloodGroup = bloodGroup;
        MaritalStatus = maritalStatus;
        MarkUpdated(updatedBy);
    }

    public void UpdateContact(string primaryPhone, string? secondaryPhone, string? email, string? profession, Guid? updatedBy)
    {
        Guard.AgainstNullOrWhiteSpace(primaryPhone, nameof(primaryPhone));

        PrimaryPhone = primaryPhone.Trim();
        SecondaryPhone = secondaryPhone?.Trim();
        Email = email?.Trim().ToLowerInvariant();
        Profession = profession?.Trim();
        MarkUpdated(updatedBy);
    }

    public void UpdateIdProof(IdProofType? idProofType, string? idProofNumber, Guid? updatedBy)
    {
        IdProofType = idProofType;
        IdProofNumber = idProofNumber?.Trim();
        MarkUpdated(updatedBy);
    }

    public void UpdateModeOfArrival(ModeOfArrivalSource source, string? channel, string? specify, Guid? updatedBy)
    {
        ModeOfArrivalSource = source;
        ModeOfArrivalChannel = channel?.Trim();
        ModeOfArrivalSpecify = specify?.Trim();
        MarkUpdated(updatedBy);
    }

    /// <summary>Called once per PatientService.UpdateAsync call, after every field update —
    /// any full save through the Edit Patient screen counts as the receptionist having
    /// re-confirmed the patient's details, regardless of which specific fields they touched.
    /// A no-op (cheap) when the flag was already false.</summary>
    public void ClearDataVerificationFlag(Guid? updatedBy)
    {
        if (!RequiresDataVerification)
        {
            return;
        }

        RequiresDataVerification = false;
        MarkUpdated(updatedBy);
    }

    /// <summary>Set once, at creation — see PatientService.CreateAsync. Address always exists
    /// for a patient, so there's no separate "add" step the way Allergies/EmergencyContacts have.</summary>
    public void SetAddress(Address address) => Address = address;

    public void UpdateAddress(string addressLine1, string? addressLine2, string? addressLine3, Guid stateId, Guid districtId, string pincode, Guid? updatedBy)
    {
        Address.Update(addressLine1, addressLine2, addressLine3, stateId, districtId, pincode);
        MarkUpdated(updatedBy);
    }

    public void AddAllergy(Allergy allergy, Guid? updatedBy)
    {
        _allergies.Add(allergy);
        MarkUpdated(updatedBy);
    }

    /// <returns>false if no allergy with that id exists on this patient.</returns>
    public bool RemoveAllergy(Guid allergyId, Guid? updatedBy)
    {
        var allergy = _allergies.FirstOrDefault(a => a.Id == allergyId);
        if (allergy is null)
        {
            return false;
        }

        _allergies.Remove(allergy);
        MarkUpdated(updatedBy);
        return true;
    }

    public void AddEmergencyContact(EmergencyContact contact, Guid? updatedBy)
    {
        _emergencyContacts.Add(contact);
        MarkUpdated(updatedBy);
    }

    /// <returns>false if no contact with that id exists on this patient, or if it's the
    /// patient's only remaining emergency contact (every patient must have at least one —
    /// same rule CreatePatientRequestValidator enforces at registration time).</returns>
    public bool RemoveEmergencyContact(Guid emergencyContactId, Guid? updatedBy)
    {
        var contact = _emergencyContacts.FirstOrDefault(c => c.Id == emergencyContactId);
        if (contact is null || _emergencyContacts.Count <= 1)
        {
            return false;
        }

        _emergencyContacts.Remove(contact);
        MarkUpdated(updatedBy);
        return true;
    }

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
