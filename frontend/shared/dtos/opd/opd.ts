import type { Gender } from '../../enums';
import type { VisitConsultation } from '../patients/patientVisit';

/** Mirrors HMS.Modules.Patients.Contracts.OpdConsultationStatus — serialized as a string
 * (JsonStringEnumConverter). The lifecycle of one OPD consultation, from queued through
 * done (or abandoned). */
export const OPD_CONSULTATION_STATUSES = ['Waiting', 'CheckedIn', 'InConsultation', 'Completed', 'Cancelled', 'NoShow'] as const;
export type OpdConsultationStatus = (typeof OPD_CONSULTATION_STATUSES)[number];

/** Mirrors HMS.Modules.Patients.Contracts.OpdPatientListItem — one row of the OPD Patient
 * List tab. Denormalizes patient/appointment-type/department/consultant display fields so
 * the list doesn't need extra round-trips. */
export interface OpdPatientListItem {
  consultationId: string;
  visitId: string;
  patientId: string;
  uhid: string;
  patientName: string;
  phoneNumber: string;
  age: number;
  gender: Gender;
  appointmentTime: string;
  appointmentTypeId?: string | null;
  appointmentTypeName?: string | null;
  departmentId: string;
  departmentName: string;
  consultantId: string;
  consultantName: string;
  status: OpdConsultationStatus;
}

/** Mirrors HMS.Modules.Patients.Contracts.OpdPatientListQuery. */
export interface OpdPatientListQuery {
  page?: number;
  pageSize?: number;
  sort?: string;
  search?: string;
  from?: string;
  to?: string;
  departmentId?: string;
  consultantId?: string;
  status?: OpdConsultationStatus;
}

/** Mirrors HMS.Modules.Patients.Contracts.OpdConsultationSummaryItem — one row of the OPD
 * Consultation List tab, aggregated per consultant/department for the given date range. Not
 * paginated — the backend returns a plain list (one row per consultant with any activity). */
export interface OpdConsultationSummaryItem {
  consultantId: string;
  consultantName: string;
  departmentId: string;
  departmentName: string;
  totalPatients: number;
  waiting: number;
  inConsultation: number;
  completed: number;
  /** Denormalized from Masters' Consultant.availableDays/visitStartTime/visitEndTime — empty/
   * null when the consultant hasn't had this set yet. */
  availableDays: string[];
  visitStartTime?: string | null;
  visitEndTime?: string | null;
  /** Optional second visiting session on the same days. */
  visitStartTime2?: string | null;
  visitEndTime2?: string | null;
}

/** Mirrors HMS.Modules.Patients.Contracts.OpdConsultationSummaryQuery. */
export interface OpdConsultationSummaryQuery {
  from?: string;
  to?: string;
  departmentId?: string;
  consultantId?: string;
}

/** Mirrors HMS.Modules.Patients.Contracts.VisitConsultationResponse as returned by the OPD
 * consultation transition endpoints (check-in/start-consultation/complete/cancel/no-show) —
 * the same VisitConsultation shape plus the appointment time and current status those
 * endpoints' response carries (fields a plain visit-creation consultation line doesn't have). */
export interface VisitConsultationResponse extends VisitConsultation {
  appointmentTime: string;
  status: OpdConsultationStatus;
}
