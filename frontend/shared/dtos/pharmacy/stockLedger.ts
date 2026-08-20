import type { TransactionType } from '../../enums';
import type { PagedQuery } from '../../types';

/**
 * Mirrors HMS.Modules.Pharmacy.Contracts.StockTransactionResponse — denormalizes
 * ProductName/BatchNo/PatientName so list consumers don't need extra round-trips — same
 * pattern as IPD's AdmissionResponse. One row per PharmacyStockTransaction, covering both
 * Receipt and Dispense entries.
 */
export interface StockTransactionResponse {
  id: string;
  productId: string;
  productName: string;
  productBatchId: string;
  batchNo: string;
  transactionType: TransactionType;
  quantity: number;
  balanceAfter: number;
  transactionDate: string;
  patientId?: string;
  patientName?: string;
  admissionId?: string;
  remarks?: string;
  createdAt: string;
}

/** Mirrors HMS.Modules.Pharmacy.Contracts.StockLedgerListQuery. */
export interface StockLedgerListQuery extends PagedQuery {
  productId?: string;
  productBatchId?: string;
  transactionType?: TransactionType;
  patientId?: string;
  fromDate?: string;
  toDate?: string;
}
