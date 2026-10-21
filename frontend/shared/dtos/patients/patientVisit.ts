import type { AdmissionType, VisitType } from '../../enums/patients';
import type { EmergencyType, ReferralColumnCategory } from '../../enums/patientRegistrationUi';

/** Mirrors HMS.Modules.Patients.Contracts.VisitCategory — a placeholder list until the
 * hospital's real visit categories are decided. */
export type VisitCategory = 'A' | 'B' | 'C';

/** A non-OP visit's admission details, as carried by both CreatePatientVisitRequest and
 * PatientVisitResponse — always absent/null on an OP visit. `admissionType` mirrors the
 * backend's MedicoLegalType, `referredBy` its ReferralSource. */
export interface VisitAdmissionDetails {
  /** ICU / Casualty / LR — Emergency visits only. */
  emergencyType?: EmergencyType | null;
  admissionType?: AdmissionType | null;
  /** Accident Register number — only with admissionType 'MLC'. */
  arNumber?: string | null;
  isReferred?: boolean | null;
  /** Only when isReferred is true. */
  referredBy?: ReferralColumnCategory | null;
  /** "Referral name" / "Referral mobile" — the referrer's name and 10-digit mobile, optional, and only
   * kept when isReferred is true. */
  referralName?: string | null;
  referralMobile?: string | null;
  category?: VisitCategory | null;
}

/** Mirrors HMS.Modules.Patients.Contracts.VisitConsultationRequest/VisitConsultationResponse. */
export interface VisitConsultation {
  departmentId: string;
  consultantId: string;
  consultationTypeId?: string | null;
}

/** Mirrors HMS.Modules.Patients.Contracts.PatientVisitResponse. All consultation lines
 * created together share one visitId — a later, separate visit gets a new one. */
export interface PatientVisit extends VisitAdmissionDetails {
  visitId: string;
  patientId: string;
  visitType: VisitType;
  /** The auto-generated OP No ("OP-…", OP visits) or IP No ("IP-…", every other visit type).
   * Missing while `visitNumberPending` — an OP visit until its consultation fee is paid
   * (ADR-103), an IP/Day Care/Observation visit until its admission's advance is paid (ADR-104);
   * only an Emergency visit is numbered as it's recorded — and on visits recorded before these
   * numbers existed. */
  visitNumber?: string | null;
  /** True while the visit is still waiting for its number: "Fee pending" (OP) or "Advance
   * pending" (IP) on the patient page. False on visits recorded before ADR-104. */
  visitNumberPending?: boolean;
  appointmentTypeId?: string | null;
  consultations: VisitConsultation[];
  createdAt: string;
}

/** Mirrors HMS.Modules.Patients.Contracts.CreatePatientVisitRequest. */
export interface CreatePatientVisitRequest extends VisitAdmissionDetails {
  visitType: VisitType;
  appointmentTypeId?: string | null;
  consultations: VisitConsultation[];
}

/** Mirrors HMS.Modules.Patients.Contracts.PatientVisitListQuery — the cross-patient,
 * reporting-only query (unlike CreatePatientVisitRequest/getVisits, which are scoped to one
 * patient). `from`/`to` are ISO date strings, inclusive, filtering on the visit's CreatedAt. */
export interface PatientVisitListQuery {
  page?: number;
  pageSize?: number;
  from?: string;
  to?: string;
}
