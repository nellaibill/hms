/**
 * Settings → Patient Registration Fields (ADR-135): which registration fields the hospital
 * makes mandatory. Mirrors HMS.Modules.Patients.Contracts.PatientFieldRequirements — the
 * GET/PUT body of /api/v1/patients/field-settings — and is applied by
 * buildPatientRegistrationUiSchema/buildPatientEditUiSchema (the forms) as well as by the
 * backend validators. Only these fields are switchable; everything else on the form is
 * always mandatory, and format checks still apply whenever a value is entered.
 */
export interface PatientFieldRequirements {
  lastName: boolean;
  /** When mandatory the dropdown starts empty and has to be picked ("Unknown" is still a
   * choice); when optional it starts on "Unknown". Form-only — the API can't tell the two apart. */
  bloodGroup: boolean;
  /** When optional, a patient 18 or older may stay "N/A". Under-18s must be "N/A" either way. */
  maritalStatus: boolean;
  addressLine1: boolean;
  state: boolean;
  /** A mandatory District needs a mandatory State — the district list comes from the state. */
  district: boolean;
  pincode: boolean;
  /** The first emergency contact. When optional it can be left blank, but a contact that is
   * started still needs both a name and a phone. */
  emergencyContact: boolean;
  /** Referring department / referral source / advertisement channel under Mode of arrival.
   * "Please specify" after an "Other" pick stays required either way. */
  modeOfArrivalDetail: boolean;
  idProofNumber: boolean;
}

export type PatientFieldKey = keyof PatientFieldRequirements;

/** A hospital that hasn't saved the settings yet — how the form has always behaved. Kept in
 * sync with PatientFieldRequirements.Default on the backend. */
export const DEFAULT_PATIENT_FIELD_REQUIREMENTS: PatientFieldRequirements = {
  lastName: true,
  bloodGroup: false,
  maritalStatus: true,
  addressLine1: true,
  state: true,
  district: true,
  pincode: true,
  emergencyContact: true,
  modeOfArrivalDetail: true,
  idProofNumber: true,
};
