/**
 * UI-only vocabulary for the Patient Registration form, matching the source documents
 * exactly (LH Software.docx's Reception & Registration table, and the standalone
 * "Patient Mode of Arrival Form"). Deliberately separate from ./patients.ts, which
 * mirrors the *current* backend Contracts wire format — these values do not yet have a
 * backend field to persist into. See the adapter in
 * frontend/web/src/pages/patients/*.tsx (toRequest) for how each bridges to the existing
 * API shape until the backend catches up in Phase 2.
 */

/** Source: LH Software.docx, "Gender – Male, Female, Transgender, NA (Dropdown list)". */
export const PATIENT_GENDERS = ['Male', 'Female', 'Transgender', 'NA'] as const;
export type PatientGenderUi = (typeof PATIENT_GENDERS)[number];

/**
 * Source: LH Software.docx's Emergency Contact relationship dropdown, reused for phone
 * "self/relation" tagging per "Contact Number with self/relation".
 */
export const RELATIONSHIPS = [
  'Father',
  'Mother',
  'Son',
  'Daughter',
  'Sister',
  'Brother',
  'Spouse',
  'Grandson',
  'Granddaughter',
  'Grandfather',
  'Grandmother',
  'Cousin',
  'Friend',
  'FatherInLaw',
  'MotherInLaw',
  'SonInLaw',
  'DaughterInLaw',
  'SisterInLaw',
  'BrotherInLaw',
  // Catch-all for the closed dropdown: also what bridging.ts's dehumanize() falls back to
  // for any pre-existing free-text value (backend stores these as plain strings) that
  // doesn't match a known option, so an unrecognized relationship reads as "unrecognized"
  // rather than silently misrepresenting the record as a specific wrong one.
  'Other',
] as const;
export type Relationship = (typeof RELATIONSHIPS)[number];

export const PHONE_RELATIONS = ['Self', ...RELATIONSHIPS] as const;
export type PhoneRelation = (typeof PHONE_RELATIONS)[number];

/** Source: "Type – Food – Specify; Drug – Specify; Environmental – Specify; Contact – Specify; Others – Specify". */
export const ALLERGY_CATEGORIES = ['Food', 'Drug', 'Environmental', 'Contact', 'Others'] as const;
export type AllergyCategory = (typeof ALLERGY_CATEGORIES)[number];

/**
 * Source: "Referral Column – Ambulance/ Doctor/ Medicals/ Others (Details with contact
 * number in each)" — applies to IP/Emergency/Day-care encounters.
 */
export const REFERRAL_COLUMN_CATEGORIES = ['Ambulance', 'Doctor', 'Medicals', 'Others'] as const;
export type ReferralColumnCategory = (typeof REFERRAL_COLUMN_CATEGORIES)[number];

/** Source: "PATIENT MODE OF ARRIVAL FORM" — top-level source of how the patient found the hospital. */
export const ARRIVAL_SOURCE_CATEGORIES = [
  'DoctorReferral',
  'PatientOrRelativeReferral',
  'OnlineAdvertisement',
  'OfflineAdvertisement',
] as const;
export type ArrivalSourceCategory = (typeof ARRIVAL_SOURCE_CATEGORIES)[number];

/** Source form: "Patient/Relatives Referral: ☐ Same Department ☐ Others". */
export const PATIENT_RELATIVE_REFERRAL_SOURCES = ['SameDepartment', 'Other'] as const;
export type PatientRelativeReferralSource = (typeof PATIENT_RELATIVE_REFERRAL_SOURCES)[number];

/** Source form: "Search Engine: Google/Hospital Website/Others" + "Social Media: Facebook/Instagram/WhatsApp/Others" flattened into one channel picker. */
export const ONLINE_AD_CHANNELS = ['Google', 'HospitalWebsite', 'Facebook', 'Instagram', 'WhatsApp', 'Other'] as const;
export type OnlineAdChannel = (typeof ONLINE_AD_CHANNELS)[number];

/**
 * Source form: Transport Ads (Buses), Public Places Ads (Theatres/Banners/Barricades/
 * Roadside displays), Signages (Outside Name Boards/Pamphlets), Mass Media (TV News/FM
 * Ad/Newspapers), Gatherings (Health Camps/Awareness Programs) — flattened into one
 * channel picker for a clean single-select digital form.
 */
export const OFFLINE_AD_CHANNELS = [
  'Buses',
  'Theatres',
  'Banners',
  'Barricades',
  'RoadsideDisplays',
  'OutsideNameBoards',
  'Pamphlets',
  'TvNews',
  'FmAd',
  'Newspapers',
  'HealthCamps',
  'AwarenessPrograms',
  'Other',
] as const;
export type OfflineAdChannel = (typeof OFFLINE_AD_CHANNELS)[number];
