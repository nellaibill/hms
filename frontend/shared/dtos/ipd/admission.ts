import type { AdmissionStatus, DischargeType, IpdAdmissionType } from '../../enums';
import type { PagedQuery } from '../../types';

/** Mirrors HMS.Modules.IPD.Contracts.CreateAdmissionRequest. */
export interface CreateAdmissionRequest {
  patientId: string;
  departmentId: string;
  consultantId: string;
  wardId: string;
  bedId: string;
  admissionDateTime?: string | null;
  admissionType: IpdAdmissionType;
  reasonForAdmission: string;
}

/** Mirrors HMS.Modules.IPD.Contracts.RequestAdmissionRequest — raises an admission with no
 * ward/bed yet (status Requested); a bed is assigned later via AssignBedRequest. Used by the
 * OPD Admissions List tab's "Request Admission" action. */
export interface RequestAdmissionRequest {
  patientId: string;
  departmentId: string;
  consultantId: string;
  admissionType: IpdAdmissionType;
  reasonForAdmission: string;
  requestedDateTime?: string | null;
}

/** Mirrors HMS.Modules.IPD.Contracts.AssignBedRequest — moves a Requested admission to
 * Admitted by assigning it a ward/bed. */
export interface AssignBedRequest {
  wardId: string;
  bedId: string;
  admissionDateTime?: string | null;
}

/** Mirrors HMS.Modules.IPD.Contracts.UpdateAdmissionRequest — PatientId/WardId/BedId are
 * intentionally absent, matching the backend (patient can't change after admission; ward/bed
 * moves go through the transfer-bed workflow instead). */
export interface UpdateAdmissionRequest {
  departmentId: string;
  consultantId: string;
  admissionType: IpdAdmissionType;
  reasonForAdmission: string;
}

/** Mirrors HMS.Modules.IPD.Contracts.TransferBedRequest. */
export interface TransferBedRequest {
  newWardId: string;
  newBedId: string;
  transferReason?: string | null;
}

/** Mirrors HMS.Modules.IPD.Contracts.DischargeAdmissionRequest. */
export interface DischargeAdmissionRequest {
  dischargeDateTime: string;
  dischargeType: DischargeType;
  finalDiagnosis?: string | null;
  dischargeNotes?: string | null;
  followUpAdvice?: string | null;
}

/** Mirrors HMS.Modules.IPD.Contracts.AdmissionResponse — denormalizes patient/ward/bed/
 * consultant display fields so list/dashboard consumers don't need extra round-trips. */
export interface Admission {
  id: string;
  admissionNumber: string;

  patientId: string;
  uhid: string;
  patientName: string;
  age: number;
  gender: string;

  departmentId: string;
  consultantId: string;
  consultantName: string;
  /** Unset while status is Requested — a ward/bed hasn't been assigned yet. */
  wardId?: string | null;
  wardName: string;
  /** Unset while status is Requested — a ward/bed hasn't been assigned yet. */
  bedId?: string | null;
  bedNumber: string;

  admissionDateTime: string;
  admissionType: IpdAdmissionType;
  reasonForAdmission: string;
  status: AdmissionStatus;

  dischargeDateTime?: string | null;
  dischargeType?: DischargeType | null;
  finalDiagnosis?: string | null;
  dischargeNotes?: string | null;
  followUpAdvice?: string | null;

  /** Set once IPDBillingService.GenerateFinalBillAsync has generated a real Invoice for this
   * admission — decides whether the Billing tab shows "Generate Final Bill" or "View Final
   * Bill". See ADR-066. */
  finalInvoiceId?: string | null;

  createdAt: string;
  updatedAt?: string | null;
}

/** Mirrors HMS.Modules.IPD.Contracts.AdmissionListQuery. */
export interface AdmissionListQuery extends PagedQuery {
  status?: AdmissionStatus;
  wardId?: string;
  departmentId?: string;
  consultantId?: string;
}

/** Mirrors HMS.Modules.IPD.Contracts.BedTransferHistoryResponse — denormalizes old/new
 * ward name + bed number so the frontend doesn't need extra round-trips per row. */
export interface BedTransferHistory {
  id: string;
  admissionId: string;

  oldWardId: string;
  oldWardName: string;
  oldBedId: string;
  oldBedNumber: string;

  newWardId: string;
  newWardName: string;
  newBedId: string;
  newBedNumber: string;

  transferReason?: string | null;
  transferredAt: string;
}

/** Mirrors HMS.Modules.IPD.Contracts.AdmissionBedStayResponse — denormalizes bed number +
 * ward name so the frontend doesn't need extra round-trips per row. */
export interface AdmissionBedStay {
  id: string;
  admissionId: string;
  bedId: string;
  bedNumber: string;
  wardId: string;
  wardName: string;
  fromDateTime: string;
  toDateTime?: string | null;
  dailyCharge: number;
}
