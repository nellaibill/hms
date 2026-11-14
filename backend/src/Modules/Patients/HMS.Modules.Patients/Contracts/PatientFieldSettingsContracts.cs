namespace HMS.Modules.Patients.Contracts;

/// <summary>
/// Which registration fields this hospital makes mandatory (ADR-135) — set on Settings →
/// Patient Registration Fields, enforced by both the New Patient Registration / Edit Patient
/// forms and CreatePatientRequestValidator/UpdatePatientRequestValidator. Only these fields
/// are switchable; the rest of the form (first name, date of birth, gender, title, primary
/// phone, mode-of-arrival source, encounter type, department, consultant) is always
/// mandatory, and every format check (10-digit phone, 6-digit pincode, ID proof formats…)
/// still applies whenever a value is entered. Also the GET/PUT body of
/// <c>api/v1/patients/field-settings</c>. Kept in sync with
/// frontend/shared/validation/patients/patientFieldRequirements.ts.
/// </summary>
public record PatientFieldRequirements
{
    public bool LastName { get; init; } = true;

    /// <summary>Form-only: when mandatory, the Blood group dropdown starts empty and the
    /// receptionist has to pick one ("Unknown" is still a choice); when optional it starts on
    /// "Unknown". The API can't tell a picked "Unknown" from a defaulted one, so it never
    /// enforces this. False by default — that's how the form already behaved.</summary>
    public bool BloodGroup { get; init; }

    /// <summary>When optional, a patient 18 or older may stay "N/A". Under-18s must still be
    /// "N/A" either way — that's a consistency check, not a requirement.</summary>
    public bool MaritalStatus { get; init; } = true;

    public bool AddressLine1 { get; init; } = true;
    public bool State { get; init; } = true;

    /// <summary>A district belongs to a state, so a mandatory District needs a mandatory
    /// State too (UpdatePatientFieldSettingsRequestValidator).</summary>
    public bool District { get; init; } = true;

    public bool Pincode { get; init; } = true;

    /// <summary>The first emergency contact. When optional, a patient may have none — but a
    /// contact that is entered still needs both a name and a phone.</summary>
    public bool EmergencyContact { get; init; } = true;

    /// <summary>The detail under the mode-of-arrival source (referring department, referral
    /// source, advertisement channel). "Please specify" after an "Other" pick stays
    /// required either way.</summary>
    public bool ModeOfArrivalDetail { get; init; } = true;

    public bool IdProofNumber { get; init; } = true;

    /// <summary>A hospital that hasn't saved the settings yet: today's behaviour.</summary>
    public static PatientFieldRequirements Default { get; } = new();

    /// <summary>The rules the API applied before ADR-135 — every switchable field mandatory
    /// except the ID proof number, which the bulk import template leaves optional. Used by
    /// the bulk Excel import (it keeps its own fixed column rules, whatever the hospital
    /// chose) and by any validator run without a hospital setting loaded.</summary>
    public static PatientFieldRequirements BulkImport { get; } = new() { IdProofNumber = false };
}
