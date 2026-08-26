import type { VisitType } from '../../enums/patients';

/** Mirrors HMS.Modules.Patients.Contracts.VisitConsultationRequest/VisitConsultationResponse. */
export interface VisitConsultation {
  departmentId: string;
  consultantId: string;
  consultationTypeId?: string | null;
}

/** Mirrors HMS.Modules.Patients.Contracts.PatientVisitResponse. All consultation lines
 * created together share one visitId — a later, separate visit gets a new one. */
export interface PatientVisit {
  visitId: string;
  patientId: string;
  visitType: VisitType;
  appointmentTypeId?: string | null;
  consultations: VisitConsultation[];
  createdAt: string;
}

/** Mirrors HMS.Modules.Patients.Contracts.CreatePatientVisitRequest. */
export interface CreatePatientVisitRequest {
  visitType: VisitType;
  appointmentTypeId?: string | null;
  consultations: VisitConsultation[];
}
