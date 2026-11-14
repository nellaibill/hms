namespace HMS.Modules.Patients.Contracts;

/// <summary>Constants describing the Patient aggregate as an offline-first sync stream
/// (ADR-133).</summary>
public static class PatientSyncAggregate
{
    /// <summary>The outbox <c>aggregate_type</c> for patient events.</summary>
    public const string AggregateType = "Patient";

    /// <summary>Version of <see cref="PatientSyncSnapshot"/> written by this build. Bump it when
    /// the snapshot shape changes, and deploy the cloud before the local servers.</summary>
    public const int CurrentPayloadSchemaVersion = 1;

    /// <summary>Payload versions this build can apply.</summary>
    public static readonly IReadOnlyList<int> SupportedPayloadSchemaVersions = [1];
}

/// <summary>
/// The full state of one patient (patient row, address, allergies, emergency contacts) as it is
/// carried in an outbox event. Every event carries the whole aggregate rather than a diff, so a
/// later event always supersedes an earlier one and a skipped event cannot leave the two sides
/// permanently different. Enum values travel as their names, exactly as they are stored.
/// Contains patient data: never log it.
/// </summary>
public sealed record PatientSyncSnapshot
{
    public Guid Id { get; init; }
    public string Uhid { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public DateOnly DateOfBirth { get; init; }
    public string Gender { get; init; } = string.Empty;
    public string BloodGroup { get; init; } = string.Empty;
    public string MaritalStatus { get; init; } = string.Empty;
    public string PrimaryPhone { get; init; } = string.Empty;
    public string? SecondaryPhone { get; init; }
    public string? Email { get; init; }
    public string? Profession { get; init; }
    public string? IdProofType { get; init; }
    public string? IdProofNumber { get; init; }
    public string ModeOfArrivalSource { get; init; } = string.Empty;
    public string? ModeOfArrivalChannel { get; init; }
    public string? ModeOfArrivalSpecify { get; init; }
    public bool RequiresDataVerification { get; init; }

    public DateTime CreatedAt { get; init; }
    public Guid? CreatedBy { get; init; }
    public DateTime? UpdatedAt { get; init; }
    public Guid? UpdatedBy { get; init; }
    public bool IsDeleted { get; init; }
    public DateTime? DeletedAt { get; init; }
    public Guid? DeletedBy { get; init; }

    public PatientSyncAddress? Address { get; init; }
    public IReadOnlyList<PatientSyncAllergy> Allergies { get; init; } = [];
    public IReadOnlyList<PatientSyncEmergencyContact> EmergencyContacts { get; init; } = [];
}

public sealed record PatientSyncAddress
{
    public string AddressLine1 { get; init; } = string.Empty;
    public string? AddressLine2 { get; init; }
    public string? AddressLine3 { get; init; }
    public Guid? StateId { get; init; }
    public Guid? DistrictId { get; init; }
    public string Pincode { get; init; } = string.Empty;
}

public sealed record PatientSyncAllergy
{
    public Guid Id { get; init; }
    public string AllergyType { get; init; } = string.Empty;
    public string? Specify { get; init; }
    public string Severity { get; init; } = string.Empty;
}

public sealed record PatientSyncEmergencyContact
{
    public Guid Id { get; init; }
    public string Relationship { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Phone { get; init; } = string.Empty;
}

/// <summary>Outcome of applying a snapshot. A failure is always permanent for that payload
/// (bad data, unknown reference, UHID clash); transient database errors are thrown instead.</summary>
public sealed record PatientSyncApplyResult(bool Succeeded, string? ErrorCode, string? Message)
{
    public static PatientSyncApplyResult Success() => new(true, null, null);

    public static PatientSyncApplyResult Rejected(string errorCode, string message) => new(false, errorCode, message);
}

/// <summary>What the Cloud Sync page may show about a patient (only to users who also hold
/// patient-management.view).</summary>
public sealed record PatientSyncDisplay(Guid PatientId, string Uhid, string FullName, bool IsDeleted);

/// <summary>Error codes a snapshot apply can return. Values only; never patient data.</summary>
public static class PatientSyncErrorCodes
{
    public const string UnsupportedPayloadVersion = "PATIENT_SYNC.UNSUPPORTED_PAYLOAD_VERSION";
    public const string InvalidPayload = "PATIENT_SYNC.INVALID_PAYLOAD";
    public const string InvalidState = "PATIENT_SYNC.UNKNOWN_STATE";
    public const string InvalidDistrict = "PATIENT_SYNC.UNKNOWN_DISTRICT";
    public const string UhidConflict = "PATIENT_SYNC.UHID_CONFLICT";
    public const string ValueTooLong = "PATIENT_SYNC.VALUE_TOO_LONG";
    public const string ConstraintViolation = "PATIENT_SYNC.CONSTRAINT_VIOLATION";
}
