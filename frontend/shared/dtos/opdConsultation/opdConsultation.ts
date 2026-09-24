/** Mirrors HMS.Modules.OpdConsultation.Contracts.OpdConsultationNoteStatus. */
export const OPD_CONSULTATION_NOTE_STATUSES = ['Draft', 'Completed'] as const;
export type OpdConsultationNoteStatus = (typeof OPD_CONSULTATION_NOTE_STATUSES)[number];

/** Mirrors HMS.Modules.OpdConsultation.Contracts.OpdDiagnosisType. */
export const OPD_DIAGNOSIS_TYPES = ['Primary', 'Secondary'] as const;
export type OpdDiagnosisType = (typeof OPD_DIAGNOSIS_TYPES)[number];

/** Mirrors HMS.Modules.OpdConsultation.Contracts.OpdInvestigationDepartment. */
export const OPD_INVESTIGATION_DEPARTMENTS = ['Laboratory', 'Radiology'] as const;
export type OpdInvestigationDepartment = (typeof OPD_INVESTIGATION_DEPARTMENTS)[number];

/** Mirrors HMS.Modules.OpdConsultation.Contracts.OpdInvestigationPriority. */
export const OPD_INVESTIGATION_PRIORITIES = ['Routine', 'Urgent', 'Stat'] as const;
export type OpdInvestigationPriority = (typeof OPD_INVESTIGATION_PRIORITIES)[number];

/** Mirrors HMS.Modules.OpdConsultation.Contracts.OpdConsultationDiagnosisRequest/Response —
 * one shape for both, since the request has no fields the response doesn't (id is simply
 * absent/ignored on submit). */
export interface OpdConsultationDiagnosis {
  id?: string;
  diagnosisId: string;
  type: OpdDiagnosisType;
  /** Response-only — resolved server-side from the Diagnosis catalog so the UI never needs the
   * admin-only Masters endpoints to show a name (regression report OPD-03). */
  diagnosisName?: string | null;
  icdCode?: string | null;
}

/** Mirrors HMS.Modules.OpdConsultation.Contracts.OpdConsultationInvestigationRequest/Response. */
export interface OpdConsultationInvestigation {
  id?: string;
  name: string;
  department: OpdInvestigationDepartment;
  priority: OpdInvestigationPriority;
  /** Masters DiagnosticService id when picked from the catalog — OPD Billing Entry pre-adds
   * these lines (regression report OPD-01). Null/absent for a free-text line. */
  serviceId?: string | null;
}

/** Mirrors HMS.Modules.OpdConsultation.Contracts.OpdConsultationPrescriptionRequest/Response
 * (regression report OPD-02). */
export interface OpdConsultationPrescription {
  id?: string;
  drugName: string;
  dose?: string | null;
  route?: string | null;
  frequency?: string | null;
  durationDays?: number | null;
  instructions?: string | null;
}

/** Mirrors HMS.Modules.OpdConsultation.Contracts.OpdDiagnosisOptionResponse — one Diagnosis
 * catalog entry for the consultation form's picker (clinical-care, not admin, permission). */
export interface OpdDiagnosisOption {
  id: string;
  name: string;
  icdCode?: string | null;
}

/** Mirrors HMS.Modules.OpdConsultation.Contracts.CreateOpdDiagnosisRequest. */
export interface CreateOpdDiagnosisRequest {
  name: string;
  icdCode?: string | null;
}

/** Mirrors HMS.Modules.OpdConsultation.Contracts.OpdInvestigationServiceOptionResponse. */
export interface OpdInvestigationServiceOption {
  id: string;
  code: string;
  name: string;
  department: OpdInvestigationDepartment;
}

/** Mirrors HMS.Modules.OpdConsultation.Contracts.BillableInvestigationResponse — a catalog-
 * linked investigation the doctor ordered on a visit, for OPD Billing Entry to pre-add. */
export interface BillableInvestigation {
  consultationId: string;
  consultantId: string;
  serviceId: string;
  name: string;
  department: OpdInvestigationDepartment;
  priority: OpdInvestigationPriority;
}

/** Mirrors HMS.Modules.OpdConsultation.Contracts.SaveOpdConsultationRequest — the same
 * full-record shape submitted to both the draft and complete endpoints. */
export interface SaveOpdConsultationRequest {
  heightCm?: number | null;
  weightKg?: number | null;
  pulseRate?: number | null;
  bloodPressure?: string | null;
  temperatureF?: number | null;
  spO2Percent?: number | null;

  presentingComplaints?: string | null;
  clinicalHistory?: string | null;
  examinationFindings?: string | null;

  diagnoses: OpdConsultationDiagnosis[];
  investigations: OpdConsultationInvestigation[];
  prescriptions: OpdConsultationPrescription[];

  planOfManagement?: string | null;

  reviewDate?: string | null;
  followUpInstructions?: string | null;

  emergencyReviewInstructions?: string | null;

  referralDepartmentId?: string | null;
  referralConsultantId?: string | null;
  referralReason?: string | null;
}

/** Mirrors HMS.Modules.OpdConsultation.Contracts.OpdConsultationNoteResponse. */
export interface OpdConsultationNote extends SaveOpdConsultationRequest {
  id?: string | null;
  consultationId: string;
  status: OpdConsultationNoteStatus;
  createdAt: string;
  updatedAt?: string | null;
}

/** Mirrors HMS.Modules.OpdConsultation.Contracts.OpdConsultationHeader — the read-only patient/
 * appointment/consultant/department strip at the top of the form. */
export interface OpdConsultationHeader {
  consultationId: string;
  visitId: string;
  patientId: string;
  uhid: string;
  patientName: string;
  phoneNumber: string;
  age: number;
  gender: string;
  appointmentTime: string;
  departmentId: string;
  departmentName: string;
  consultantId: string;
  consultantName: string;
  /** The owning PatientVisitConsultation's own queue status (Waiting/CheckedIn/InConsultation/
   * Completed/Cancelled/NoShow) — distinct from OpdConsultationNote.status above. */
  consultationStatus: string;
}

/** Mirrors HMS.Modules.OpdConsultation.Contracts.OpdConsultationDetailResponse. */
export interface OpdConsultationDetail {
  header: OpdConsultationHeader;
  note: OpdConsultationNote;
}

/** Mirrors HMS.Modules.OpdConsultation.Contracts.StructureConsultationNoteRequest — a raw
 * dictation/typed transcript, never persisted by itself. */
export interface StructureConsultationNoteRequest {
  transcript: string;
}

/** Mirrors HMS.Modules.OpdConsultation.Contracts.StructuredConsultationNoteResponse — the
 * narrative fields the AI extracted from a transcript. The caller merges these into its own
 * form state and still submits through the normal saveDraft/complete calls; nothing here is
 * saved directly. */
export interface StructuredConsultationNoteFields {
  presentingComplaints?: string | null;
  clinicalHistory?: string | null;
  examinationFindings?: string | null;
  planOfManagement?: string | null;
  followUpInstructions?: string | null;
  emergencyReviewInstructions?: string | null;
}
