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

  /** Set when this dispense was successfully billed — null if never billed (either the
   * attempt failed, see billingFailed/billingError below, or this is a historical row where
   * billing detail isn't re-fetched — see the backend contract's own doc comment). */
  invoiceId?: string;
  /** Populated only on the response returned immediately by the create call — always
   * undefined on list/detail reads. */
  invoiceNumber?: string;
  /** True only on the immediate create response when the dispense itself succeeded but the
   * follow-up billing attempt did not — the dispense is NOT rolled back in that case. */
  billingFailed?: boolean;
  billingError?: string;
}

/** Mirrors HMS.Modules.Pharmacy.Contracts.DispenseListQuery. */
export interface DispenseListQuery extends PagedQuery {
  patientId?: string;
  productId?: string;
}
