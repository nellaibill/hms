namespace HMS.Modules.Documents.Contracts;

// Declared here (Contracts), not Domain: every one of these is exposed as a public DTO
// property (UploadDocumentRequest/DocumentResponse), and a public property can't have a
// less-visible type (CS0053) — mirrors HMS.Modules.Patients.Contracts.PatientEnums.cs, whose
// Title/Gender/BloodGroup enums are shared the same way between Patient (Domain) and
// CreatePatientRequest/PatientResponse (Contracts).

/// <summary>
/// The record kind a document is attached to. Kept as a closed enum (stored as its string
/// name, matching the rest of the platform's enum convention) rather than a lookup table —
/// see docs/modules/Documents/DocumentManagement.md for why, and for which of these values
/// have a real backend module to existence-check uploads against.
/// </summary>
public enum DocumentOwnerType
{
    Patient = 0,
    Staff = 1,
    Doctor = 2,
    Appointment = 3,
    Admission = 4,
    Lab = 5,
    Radiology = 6,
    Billing = 7,
    Asset = 8,
    Vendor = 9,
}

/// <summary>The kind of document within its owner's record, mirroring the frontend's
/// DOCUMENT_TYPES constant (frontend/web/src/features/documents/constants.ts).</summary>
public enum DocumentType
{
    IdProof = 0,
    Prescription = 1,
    Report = 2,
    Invoice = 3,
    ConsentForm = 4,
    Insurance = 5,
    Certificate = 6,
    Other = 7,
}

/// <summary>
/// Sensitivity of a document's content, independent of <see cref="DocumentOwnerType"/> or
/// <see cref="DocumentType"/> — it is what actually should drive access control (see
/// Application.Security.DocumentAccessPolicy), since a Vendor invoice and a patient consent
/// form have very different exposure even though neither is inherently more or less
/// "Patient-shaped" than the other.
/// </summary>
public enum DocumentClassification
{
    Public = 0,
    Internal = 1,
    Confidential = 2,
    Restricted = 3,
}

/// <summary>
/// Lifecycle state driven by the asynchronous scan pipeline (US-9). A document's content is
/// never downloadable until it reaches <see cref="Available"/>.
/// </summary>
public enum DocumentStatus
{
    /// <summary>Uploaded and stored; queued for virus scanning. Not downloadable yet.</summary>
    Pending = 0,

    /// <summary>Scanned clean. Downloadable, subject to <see cref="Application.Security.IDocumentAccessPolicy"/>.</summary>
    Available = 1,

    /// <summary>Scan flagged the content. Never downloadable; visible to administrators only for audit purposes.</summary>
    Quarantined = 2,
}
