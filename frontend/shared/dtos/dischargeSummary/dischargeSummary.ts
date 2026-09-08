import type { DischargeSummaryStatus, FoodInstruction } from '../../enums';

/**
 * Mirrors HMS.Modules.DischargeSummary.Contracts.DischargeMedicationRequest — one discharge
 * medication line, as submitted on Update. The full list is replaced wholesale each time
 * (list-sync, no separate per-line CRUD endpoints).
 */
export interface DischargeMedicationRequest {
  sortOrder: number;
  drugName: string;
  dose: string;
  route: string;
  morningQty: number;
  noonQty: number;
  eveningQty: number;
  nightQty: number;
  durationDays: number;
  foodInstruction: FoodInstruction;
}

/** Mirrors HMS.Modules.DischargeSummary.Contracts.DischargeMedicationResponse (Id added, same other fields). */
export interface DischargeMedicationResponse extends DischargeMedicationRequest {
  id: string;
}

/**
 * Mirrors HMS.Modules.DischargeSummary.Contracts.UpdateDischargeSummaryRequest — every
 * clinical/examination/vitals/course/surgical/advice field, submitted together on every PUT
 * (no per-field PATCH). Only allowed while Status is Draft; rejected once Finalized.
 */
export interface UpdateDischargeSummaryRequest {
  /** Independently editable from its Create-time prefill (Admission.FinalDiagnosis). */
  finalDiagnosis?: string | null;

  // Clinical.
  chiefComplaints?: string | null;
  historyOfPresentingIllness?: string | null;
  pastMedicalHistory?: string | null;
  pastSurgicalHistory?: string | null;
  familyHistory?: string | null;
  personalHistory?: string | null;

  // Examination.
  generalExamination?: string | null;
  cvsFindings?: string | null;
  rsFindings?: string | null;
  paFindings?: string | null;
  cnsFindings?: string | null;
  localExamination?: string | null;
  gait?: string | null;

  // Vitals — a single snapshot, not a repeating observations table.
  heightCm?: number | null;
  weightKg?: number | null;
  pulseRate?: number | null;
  respiratoryRate?: number | null;
  temperatureF?: number | null;
  spO2Percent?: number | null;
  /** Free-text, e.g. "130/80". */
  bloodPressure?: string | null;

  courseInHospital?: string | null;

  // Surgical Details — plain manual fields (no OT module yet).
  procedureName?: string | null;
  procedureDateTime?: string | null;
  primarySurgeon?: string | null;
  assistantSurgeons?: string | null;
  anaesthetist?: string | null;
  anaesthesia?: string | null;
  surgicalPosition?: string | null;
  intraOperativeFindings?: string | null;
  operativeNotes?: string | null;

  // Discharge Advice.
  diet?: string | null;
  woundCare?: string | null;
  activity?: string | null;
  physiotherapy?: string | null;
  reviewInstructions?: string | null;
  emergencyInstructions?: string | null;
  conditionAtDischarge?: string | null;

  /** The full medication list — replaces whatever was there before in one call. An empty
   * list clears every medication line. */
  medications: DischargeMedicationRequest[];
}

/**
 * Mirrors HMS.Modules.DischargeSummary.Contracts.FinalizeDischargeSummaryRequest — sign-off
 * captured once, at Finalize, not a multi-step approval workflow. All three fields are
 * optional: a hospital that only ever fills in "Prepared By" still gets a valid, finalized
 * summary.
 */
export interface FinalizeDischargeSummaryRequest {
  preparedByUserId?: string | null;
  checkedByUserId?: string | null;
  consultantApprovedByUserId?: string | null;
}

/**
 * Mirrors HMS.Modules.DischargeSummary.Contracts.DischargeSummaryResponse — deliberately
 * carries only this aggregate's own authored content, no live-joined Patient/Admission
 * display fields (name, UHID, ward/bed, consultant, etc.). Per the approved plan, those are
 * fetched separately from GET /api/v1/patients/{id} and GET /api/v1/ipd/admissions/{id} at
 * render time, never duplicated here.
 */
export interface DischargeSummary {
  id: string;
  admissionId: string;
  patientId: string;
  status: DischargeSummaryStatus;

  /** Pre-filled from Admission.FinalDiagnosis at Create time, independently editable from that point. */
  finalDiagnosis?: string | null;

  // Clinical.
  chiefComplaints?: string | null;
  historyOfPresentingIllness?: string | null;
  pastMedicalHistory?: string | null;
  pastSurgicalHistory?: string | null;
  familyHistory?: string | null;
  personalHistory?: string | null;

  // Examination.
  generalExamination?: string | null;
  cvsFindings?: string | null;
  rsFindings?: string | null;
  paFindings?: string | null;
  cnsFindings?: string | null;
  localExamination?: string | null;
  gait?: string | null;

  // Vitals.
  heightCm?: number | null;
  weightKg?: number | null;
  pulseRate?: number | null;
  respiratoryRate?: number | null;
  temperatureF?: number | null;
  spO2Percent?: number | null;
  bloodPressure?: string | null;

  courseInHospital?: string | null;

  // Surgical Details.
  procedureName?: string | null;
  procedureDateTime?: string | null;
  primarySurgeon?: string | null;
  assistantSurgeons?: string | null;
  anaesthetist?: string | null;
  anaesthesia?: string | null;
  surgicalPosition?: string | null;
  intraOperativeFindings?: string | null;
  operativeNotes?: string | null;

  // Discharge Advice.
  diet?: string | null;
  woundCare?: string | null;
  activity?: string | null;
  physiotherapy?: string | null;
  reviewInstructions?: string | null;
  emergencyInstructions?: string | null;
  conditionAtDischarge?: string | null;

  medications: DischargeMedicationResponse[];

  preparedByUserId?: string | null;
  checkedByUserId?: string | null;
  consultantApprovedByUserId?: string | null;
  finalizedAt?: string | null;
  finalizedByUserId?: string | null;

  createdAt: string;
  updatedAt?: string | null;
}
