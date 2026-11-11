using HMS.Modules.Patients.Contracts;

namespace HMS.Modules.Patients.Domain;

/// <summary>
/// A fully parsed, typed copy of a <see cref="PatientSyncSnapshot"/>, applied to the
/// aggregate by <see cref="Patient.CreateReplica"/> / <see cref="Patient.ApplyReplicaState"/>
/// when the offline-first sync (ADR-133) copies a patient from the local server to the cloud,
/// or from the cloud back to the local server when an admin keeps the cloud version of a
/// conflict. It carries the original ids, audit columns and soft-delete state, which ordinary
/// edits never set directly.
/// </summary>
internal sealed record PatientReplicaState(
    Guid Id,
    string Uhid,
    Title Title,
    string FirstName,
    string LastName,
    DateOnly DateOfBirth,
    Gender Gender,
    BloodGroup BloodGroup,
    MaritalStatus MaritalStatus,
    string PrimaryPhone,
    string? SecondaryPhone,
    string? Email,
    string? Profession,
    IdProofType? IdProofType,
    string? IdProofNumber,
    ModeOfArrivalSource ModeOfArrivalSource,
    string? ModeOfArrivalChannel,
    string? ModeOfArrivalSpecify,
    bool RequiresDataVerification,
    DateTime CreatedAt,
    Guid? CreatedBy,
    DateTime? UpdatedAt,
    Guid? UpdatedBy,
    bool IsDeleted,
    DateTime? DeletedAt,
    Guid? DeletedBy,
    AddressReplicaState Address,
    IReadOnlyList<AllergyReplicaState> Allergies,
    IReadOnlyList<EmergencyContactReplicaState> EmergencyContacts);

internal sealed record AddressReplicaState(
    string AddressLine1,
    string? AddressLine2,
    string? AddressLine3,
    Guid StateId,
    Guid DistrictId,
    string Pincode);

internal sealed record AllergyReplicaState(Guid Id, AllergyType AllergyType, string? Specify, AllergySeverity Severity);

internal sealed record EmergencyContactReplicaState(Guid Id, Relationship Relationship, string Name, string Phone);
