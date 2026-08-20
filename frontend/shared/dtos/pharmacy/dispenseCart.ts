import type { DispenseResponse } from './dispense';

/** Mirrors HMS.Modules.Pharmacy.Contracts.DispenseCartLineRequest. */
export interface DispenseCartLineRequest {
  productId: string;
  productBatchId: string;
  quantity: number;
  remarks?: string;
}

/**
 * Mirrors HMS.Modules.Pharmacy.Contracts.CreateDispenseCartRequest — checks out several
 * products/batches for one patient in a single call, billed as ONE invoice with N line items.
 * admissionId stays cart-level (one optional IPD link for the whole checkout), same as the
 * single-item CreateDispenseRequest.
 */
export interface CreateDispenseCartRequest {
  patientId: string;
  admissionId?: string;
  lines: DispenseCartLineRequest[];
}

/**
 * Mirrors HMS.Modules.Pharmacy.Contracts.DispenseCartResponse — one DispenseResponse per cart
 * line, plus the billing outcome for the whole cart (billing applies once to the entire
 * checkout, not per line).
 */
export interface DispenseCartResponse {
  lines: DispenseResponse[];
  invoiceId?: string;
  invoiceNumber?: string;
  billingFailed?: boolean;
  billingError?: string;
  totalAmount: number;
}
