import type {
  AllergySeverity,
  AllergyType,
  BloodGroup,
  Gender,
  IdProofType,
  MaritalStatus,
  ModeOfArrivalSource,
  Relationship,
  Title,
} from '../../enums/patients';

/** Mirrors HMS.Modules.Patients.Contracts.AddressRequest/AddressResponse. */
export interface Address {
  addressLine1: string;
  addressLine2?: string | null;
  addressLine3?: string | null;
  stateId: string;
  districtId: string;
  pincode: string;
}

/** Mirrors HMS.Modules.Patients.Contracts.AllergyResponse (id present) / AllergyRequest (id absent, see AllergyInput below). */
export interface Allergy {
  id: string;
  allergyType: AllergyType;
  specify?: string | null;
  severity: AllergySeverity;
}

export type AllergyInput = Omit<Allergy, 'id'>;

/** Mirrors HMS.Modules.Patients.Contracts.EmergencyContactResponse (id present) / EmergencyContactRequest (id absent, see EmergencyContactInput below). */
export interface EmergencyContact {
  id: string;
  relationship: Relationship;
  name: string;
  phone: string;
}

export type EmergencyContactInput = Omit<EmergencyContact, 'id'>;

/** Mirrors HMS.Modules.Patients.Contracts.PatientResponse. */
export interface Patient {
  id: string;
  uhid: string;

  title: Title;
  firstName: string;
  lastName: string;
  dateOfBirth: string;
  age: number;
  gender: Gender;
  bloodGroup: BloodGroup;
  maritalStatus: MaritalStatus;

  primaryPhone: string;
  secondaryPhone?: string | null;
  email?: string | null;
  profession?: string | null;

  idProofType?: IdProofType | null;
  idProofNumber?: string | null;

  modeOfArrivalSource: ModeOfArrivalSource;
  modeOfArrivalChannel?: string | null;
  modeOfArrivalSpecify?: string | null;

  address: Address;
  allergies: Allergy[];
  emergencyContacts: EmergencyContact[];

  /** Opaque optimistic-concurrency token (the row's Postgres xmin at read time) — echo this
   * back on UpdatePatientRequest.rowVersion so a save against stale data is rejected with a
   * clear conflict instead of silently overwriting someone else's edit. Always present on a
   * real API response (see PatientResponse.RowVersion); the mock store populates a fake one
   * too, so this is never actually undefined at runtime. */
  rowVersion: string;

  createdAt: string;
  updatedAt?: string | null;
}

/** Mirrors HMS.Modules.Patients.Contracts.CreatePatientRequest. */
export interface CreatePatientRequest {
  title: Title;
  firstName: string;
  lastName: string;
  dateOfBirth: string;
  gender: Gender;
  bloodGroup: BloodGroup;
  maritalStatus: MaritalStatus;

  primaryPhone: string;
  secondaryPhone?: string | null;
  email?: string | null;
  profession?: string | null;

  idProofType?: IdProofType | null;
  idProofNumber?: string | null;

  modeOfArrivalSource: ModeOfArrivalSource;
  modeOfArrivalChannel?: string | null;
  modeOfArrivalSpecify?: string | null;

  address: Address;
  allergies: AllergyInput[];
  emergencyContacts: EmergencyContactInput[];
}

/** Mirrors HMS.Modules.Patients.Contracts.UpdatePatientRequest — Allergies/EmergencyContacts
 * have their own add/remove endpoints (AddAllergyRequest/AddEmergencyContactRequest below)
 * rather than being replaced wholesale here. */
export interface UpdatePatientRequest {
  title: Title;
  firstName: string;
  lastName: string;
  dateOfBirth: string;
  gender: Gender;
  bloodGroup: BloodGroup;
  maritalStatus: MaritalStatus;

  primaryPhone: string;
  secondaryPhone?: string | null;
  email?: string | null;
  profession?: string | null;

  idProofType?: IdProofType | null;
  idProofNumber?: string | null;

  modeOfArrivalSource: ModeOfArrivalSource;
  modeOfArrivalChannel?: string | null;
  modeOfArrivalSpecify?: string | null;

  address: Address;

  /** Must be the RowVersion from the Patient this edit was loaded from — see Patient.rowVersion. */
  rowVersion: string;
}

/** Mirrors HMS.Modules.Patients.Contracts.AddAllergyRequest — not wired to any UI yet (Create
 * sends the full Allergies[] up front instead), kept here so the DTO file matches the backend
 * surface completely for the next pass that adds per-row add/remove to the Edit page. */
export type AddAllergyRequest = AllergyInput;

/** Mirrors HMS.Modules.Patients.Contracts.AddEmergencyContactRequest — same "not wired yet" note as AddAllergyRequest. */
export type AddEmergencyContactRequest = EmergencyContactInput;

/** Mirrors HMS.Modules.Patients.Contracts.PatientListQuery. */
export interface PatientListQuery {
  page?: number;
  pageSize?: number;
  sort?: string;
  /** @deprecated superseded by the separate name/age/uhid/phone fields below. */
  search?: string;
  name?: string;
  age?: number;
  uhid?: string;
  phone?: string;
}
