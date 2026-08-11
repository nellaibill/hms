/** Mirrors HMS.Modules.Patients.Contracts.PatientEnums — serialized as strings (JsonStringEnumConverter). */
export const TITLES = ['Mr', 'Mrs', 'Ms', 'Miss', 'Dr', 'Master', 'Baby'] as const;
export type Title = (typeof TITLES)[number];

// Matches PATIENT_GENDERS in patientRegistrationUi.ts exactly — the UI form and the wire
// format use the same four values now, so bridging.ts's toBackendGender/fromBackendGender
// are a lossless 1:1 mapping (previously this only had Male/Female/Other, so Transgender and
// "prefer not to say" both collapsed into "Other" and were indistinguishable again on edit).
export const GENDERS = ['Male', 'Female', 'Transgender', 'NA'] as const;
export type Gender = (typeof GENDERS)[number];

export const BLOOD_GROUPS = [
  'APositive',
  'ANegative',
  'BPositive',
  'BNegative',
  'ABPositive',
  'ABNegative',
  'OPositive',
  'ONegative',
  'Unknown',
] as const;
export type BloodGroup = (typeof BLOOD_GROUPS)[number];

export const ALLERGY_SEVERITIES = ['Mild', 'Moderate', 'Severe'] as const;
export type AllergySeverity = (typeof ALLERGY_SEVERITIES)[number];

export const ID_PROOF_TYPES = ['Aadhaar', 'Passport', 'DrivingLicense', 'VoterId', 'Other'] as const;
export type IdProofType = (typeof ID_PROOF_TYPES)[number];

export const ENCOUNTER_TYPES = ['OP', 'IP', 'Emergency', 'DayCare'] as const;
export type EncounterType = (typeof ENCOUNTER_TYPES)[number];

export const MODES_OF_ARRIVAL = ['WalkIn', 'Ambulance', 'Referred'] as const;
export type ModeOfArrival = (typeof MODES_OF_ARRIVAL)[number];

export const ADMISSION_TYPES = ['MLC', 'NMLC'] as const;
export type AdmissionType = (typeof ADMISSION_TYPES)[number];
