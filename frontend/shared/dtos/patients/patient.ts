import type {
  AdmissionType,
  AllergySeverity,
  BloodGroup,
  EncounterType,
  Gender,
  IdProofType,
  ModeOfArrival,
  Title,
} from '../../enums/patients';

/** Mirrors HMS.Modules.Patients.Contracts.PatientRegistrationResponse. */
export interface PatientRegistration {
  id: string;
  registrationNumber: string;
  encounterType: EncounterType;
  modeOfArrival: ModeOfArrival;
  departmentId: string;
  consultantId: string;
  admissionType?: AdmissionType | null;
  referralSource?: string | null;
  category?: string | null;
  createdAt: string;
}

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
  bloodGroup?: BloodGroup | null;

  addressLine1: string;
  addressLine2?: string | null;
  addressLine3?: string | null;
  district: string;
  state: string;
  pincode: string;

  primaryPhone: string;
  primaryPhoneRelation?: string | null;
  alternatePhone?: string | null;
  alternatePhoneRelation?: string | null;
  alternatePhone2?: string | null;
  alternatePhone2Relation?: string | null;
  email?: string | null;
  profession?: string | null;

  emergencyContactRelationship: string;
  emergencyContactName: string;
  emergencyContactPhone: string;

  hasKnownAllergy: boolean;
  allergyType?: string | null;
  allergySeverity?: AllergySeverity | null;

  photoPath?: string | null;
  idProofType?: IdProofType | null;
  idProofPath?: string | null;

  currentRegistration?: PatientRegistration | null;

  createdAt: string;
  updatedAt?: string | null;
}

/** Mirrors HMS.Modules.Patients.Contracts.PatientRegistrationDetails. */
export interface PatientRegistrationDetailsRequest {
  encounterType: EncounterType;
  modeOfArrival: ModeOfArrival;
  departmentId: string;
  consultantId: string;
  admissionType?: AdmissionType | null;
  referralSource?: string | null;
  category?: string | null;
}

/** Mirrors HMS.Modules.Patients.Contracts.CreatePatientRequest. */
export interface CreatePatientRequest {
  title: Title;
  firstName: string;
  lastName: string;
  dateOfBirth: string;
  gender: Gender;
  bloodGroup?: BloodGroup | null;

  addressLine1: string;
  addressLine2?: string | null;
  addressLine3?: string | null;
  district: string;
  state: string;
  pincode: string;

  primaryPhone: string;
  primaryPhoneRelation?: string | null;
  alternatePhone?: string | null;
  alternatePhoneRelation?: string | null;
  alternatePhone2?: string | null;
  alternatePhone2Relation?: string | null;
  email?: string | null;
  profession?: string | null;

  emergencyContactRelationship: string;
  emergencyContactName: string;
  emergencyContactPhone: string;

  hasKnownAllergy: boolean;
  allergyType?: string | null;
  allergySeverity?: AllergySeverity | null;

  registration: PatientRegistrationDetailsRequest;
}

/** Mirrors HMS.Modules.Patients.Contracts.UpdatePatientRequest. */
export interface UpdatePatientRequest {
  title: Title;
  firstName: string;
  lastName: string;
  dateOfBirth: string;
  gender: Gender;
  bloodGroup?: BloodGroup | null;

  addressLine1: string;
  addressLine2?: string | null;
  addressLine3?: string | null;
  district: string;
  state: string;
  pincode: string;

  primaryPhone: string;
  primaryPhoneRelation?: string | null;
  alternatePhone?: string | null;
  alternatePhoneRelation?: string | null;
  alternatePhone2?: string | null;
  alternatePhone2Relation?: string | null;
  email?: string | null;
  profession?: string | null;

  emergencyContactRelationship: string;
  emergencyContactName: string;
  emergencyContactPhone: string;

  hasKnownAllergy: boolean;
  allergyType?: string | null;
  allergySeverity?: AllergySeverity | null;
}

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
