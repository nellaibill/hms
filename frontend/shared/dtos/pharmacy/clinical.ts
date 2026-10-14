import type { IndentStatus } from '../../enums';
import type { PagedQuery } from '../../types';

/** One drug line as the doctor wrote it — free text, not a catalog item. */
export interface PrescribedDrugResponse {
  drugName: string;
  dose?: string | null;
  route?: string | null;
  frequency?: string | null;
  durationDays?: number | null;
  instructions?: string | null;
  /** Units the course works out to, when frequency and duration can be read. */
  suggestedQuantity?: number | null;
}

export interface PrescriptionBillResponse {
  saleId: string;
  billNo: string;
  netAmount: number;
}

/** Mirrors HMS.Modules.Pharmacy.Contracts.PrescriptionQueueItemResponse — a completed OPD
 * consultation with medicines prescribed, and any bills already made against it. */
export interface PrescriptionQueueItemResponse {
  consultationId: string;
  patientId: string;
  uhid: string;
  patientName: string;
  phoneNumber: string;
  doctorName: string;
  departmentName: string;
  prescribedAt: string;
  drugs: PrescribedDrugResponse[];
  /** Empty means it is still waiting. */
  bills: PrescriptionBillResponse[];
}

export interface IndentLineRequest {
  productId: string;
  /** Units. */
  quantity: number;
  instructions?: string;
}

/** Mirrors HMS.Modules.Pharmacy.Contracts.CreateIndentRequest. */
export interface CreateIndentRequest {
  storeId?: string;
  admissionId: string;
  isUrgent: boolean;
  remarks?: string;
  lines: IndentLineRequest[];
}

export interface IndentLineResponse {
  id: string;
  lineNo: number;
  productId: string;
  productName: string;
  quantity: number;
  issuedQuantity: number;
  pendingQuantity: number;
  instructions?: string | null;
}

/** Mirrors HMS.Modules.Pharmacy.Contracts.IndentResponse — a ward's request for an inpatient's medicines. */
export interface IndentResponse {
  id: string;
  indentNo: string;
  storeId: string;
  admissionId: string;
  admissionNumber: string;
  patientId: string;
  patientName: string;
  patientUhid?: string | null;
  location?: string | null;
  doctorName?: string | null;
  requestedAt: string;
  status: IndentStatus;
  isUrgent: boolean;
  remarks?: string | null;
  lineCount: number;
  lines: IndentLineResponse[];
}

export interface IndentListQuery extends PagedQuery {
  admissionId?: string;
  status?: IndentStatus;
  openOnly?: boolean;
}
