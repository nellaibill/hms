namespace HMS.Modules.Patients.Contracts;

/// <summary>
/// Records one registration/encounter event for an existing patient — Encounter Type +
/// (for OP) Appointment Type, plus one or more consultation lines (primary consultant + any
/// "Add another Consultant" rows). All consultation lines created together share one VisitId;
/// a later, separate visit is a new POST with its own VisitId.
/// </summary>
public record CreatePatientVisitRequest
{
    public VisitType VisitType { get; init; }
    public Guid? AppointmentTypeId { get; init; }

    /// <summary>ICU / Casualty / LR — Emergency visits only, ignored for every other type.
    /// Optional on the wire for the same reason as the admission details below: the Add Visit
    /// form requires it, the New Patient Registration wizard doesn't collect it.</summary>
    public EmergencyType? EmergencyType { get; init; }

    public IReadOnlyList<VisitConsultationRequest> Consultations { get; init; } = [];

    // Admission details — recorded for every visit type except OP, where they're ignored.
    // All optional on the wire: the standalone Add Visit form sends (and itself requires)
    // them, but the New Patient Registration wizard records non-OP visits through this same
    // request without collecting them yet. See CreatePatientVisitRequestValidator.

    /// <summary>The form's "Admission type" / "Observation type" answer.</summary>
    public MedicoLegalType? AdmissionType { get; init; }

    /// <summary>Accident Register number — required when <see cref="AdmissionType"/> is MLC.</summary>
    public string? ArNumber { get; init; }

    public bool? IsReferred { get; init; }

    /// <summary>Required when <see cref="IsReferred"/> is true.</summary>
    public ReferralSource? ReferredBy { get; init; }

    /// <summary>Optional "Referral name" / "Referral mobile" — the referrer's name and 10-digit mobile number.
    /// Ignored unless <see cref="IsReferred"/> is true.</summary>
    public string? ReferralName { get; init; }
    public string? ReferralMobile { get; init; }

    public VisitCategory? Category { get; init; }
}

public record VisitConsultationRequest
{
    public Guid DepartmentId { get; init; }
    public Guid ConsultantId { get; init; }
    public Guid? ConsultationTypeId { get; init; }

    /// <summary>Optional — the service defaults to DateTime.UtcNow when omitted, matching
    /// today's walk-in-registration behavior unchanged.</summary>
    public DateTime? AppointmentTime { get; init; }
}

public record PatientVisitResponse
{
    public Guid VisitId { get; init; }
    public Guid PatientId { get; init; }
    public VisitType VisitType { get; init; }

    /// <summary>The auto-generated OP No (OP visits, "OP-…") or IP No (every other visit type,
    /// "IP-…"). Null only for visits recorded before these numbers existed.</summary>
    public string? VisitNumber { get; init; }

    public Guid? AppointmentTypeId { get; init; }
    public EmergencyType? EmergencyType { get; init; }
    public MedicoLegalType? AdmissionType { get; init; }
    public string? ArNumber { get; init; }
    public bool? IsReferred { get; init; }
    public ReferralSource? ReferredBy { get; init; }
    public string? ReferralName { get; init; }
    public string? ReferralMobile { get; init; }
    public VisitCategory? Category { get; init; }
    public IReadOnlyList<VisitConsultationResponse> Consultations { get; init; } = [];
    public DateTime CreatedAt { get; init; }
}

public record VisitConsultationResponse
{
    public Guid Id { get; init; }
    public Guid DepartmentId { get; init; }
    public Guid ConsultantId { get; init; }
    public Guid? ConsultationTypeId { get; init; }
    public DateTime AppointmentTime { get; init; }
    public OpdConsultationStatus Status { get; init; }
}
