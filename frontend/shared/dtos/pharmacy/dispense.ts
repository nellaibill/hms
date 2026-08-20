import type { PagedQuery } from '../../types';

/** Mirrors HMS.Modules.Pharmacy.Contracts.CreateDispenseRequest. */
export interface CreateDispenseRequest {
  productId: string;
  productBatchId: string;
  patientId: string;
  admissionId?: string;
  quantity: number;
  remarks?: string;
}

/**
 * Mirrors HMS.Modules.Pharmacy.Contracts.DispenseResponse — denormalizes
 * ProductName/BatchNo/PatientName so list/detail consumers don't need extra
 * round-trips — same pattern as IPD's AdmissionResponse.
 */
export interface DispenseResponse {
  id: string;
  productId: string;
  productName: string;
  productBatchId: string;
  batchNo: string;
  patientId: string;
  patientName: string;
  admissionId?: string;
  quantity: number;
  balanceAfter: number;
  transactionDate: string;
  remarks?: string;
  createdAt: string;
}

/** Mirrors HMS.Modules.Pharmacy.Contracts.DispenseListQuery. */
export interface DispenseListQuery extends PagedQuery {
  patientId?: string;
  productId?: string;
}
