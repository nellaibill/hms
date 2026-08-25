/** Mirrors HMS.Modules.Patients.Contracts.PatientEnums — serialized as strings (JsonStringEnumConverter). */
export const TITLES = ['Mr', 'Mrs', 'Ms', 'Miss', 'Dr', 'Master', 'Baby'] as const;
export type Title = (typeof TITLES)[number];

// Matches PATIENT_GENDERS in patientRegistrationUi.ts exactly — the UI form and the wire
// format use the same four values, so bridging.ts's toBackendGender/fromBackendGender are a
// lossless 1:1 mapping.
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

export const ID_PROOF_TYPES = ['Aadhaar', 'Passport', 'DrivingLicense', 'VoterId', 'Other'] as const;
export type IdProofType = (typeof ID_PROOF_TYPES)[number];

// These four re-export the *values* already defined in patientRegistrationUi.ts (the UI-first
// vocabulary file) under their wire-contract names — the backend's PatientEnums.cs doc
// comments confirm each already matches its UI counterpart value-for-value
// (MaritalStatus/MARITAL_STATUSES, AllergyType/ALLERGY_CATEGORIES, Relationship/RELATIONSHIPS,
// ModeOfArrivalSource/ARRIVAL_SOURCE_CATEGORIES), so there's one definition, not two that could drift.
export { MARITAL_STATUSES, type MaritalStatus } from './patientRegistrationUi';
export { ALLERGY_CATEGORIES as ALLERGY_TYPES, type AllergyCategory as AllergyType } from './patientRegistrationUi';
export { RELATIONSHIPS, type Relationship } from './patientRegistrationUi';
export { ARRIVAL_SOURCE_CATEGORIES as MODE_OF_ARRIVAL_SOURCES, type ArrivalSourceCategory as ModeOfArrivalSource } from './patientRegistrationUi';

export const ALLERGY_SEVERITIES = ['Mild', 'Moderate', 'Severe'] as const;
export type AllergySeverity = (typeof ALLERGY_SEVERITIES)[number];

// Still used by the Registration Details tab's (unwired — see PatientRegistrationCreatePage's
// toRequest doc comment) Admission Type field — kept here even though CreatePatientRequest no
// longer has a `registration` block. Named plainly `AdmissionType` for this module; IPD's own,
// unrelated admission concept is deliberately named IpdAdmissionType (see enums/ipd.ts) to
// avoid colliding with this export in the shared barrel.
export const ADMISSION_TYPES = ['MLC', 'NMLC'] as const;
export type AdmissionType = (typeof ADMISSION_TYPES)[number];
