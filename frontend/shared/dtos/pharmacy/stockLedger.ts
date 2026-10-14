import type { StockSourceType, TransactionType } from '../../enums';
import type { PagedQuery } from '../../types';

/**
 * Mirrors HMS.Modules.Pharmacy.Contracts.StockTransactionResponse — denormalizes
 * ProductName/BatchNo/PatientName so list consumers don't need extra round-trips — same
 * pattern as IPD's AdmissionResponse. One row per PharmacyStockTransaction, covering both
 * Receipt and Dispense entries.
 */
export interface StockTransactionResponse {
  id: string;
  storeId: string;
  productId: string;
  productName: string;
  productBatchId: string;
  batchNo: string;
  transactionType: TransactionType;
  quantity: number;
  /** What one unit was sold for, on a dispense. */
  unitPrice?: number | null;
  balanceAfter: number;
  transactionDate: string;
  patientId?: string;
  patientName?: string;
  admissionId?: string;
  remarks?: string;
  /** The document this movement was posted from, when there is one. */
  sourceType?: StockSourceType | null;
  sourceId?: string | null;
  sourceNo?: string | null;
  createdAt: string;
}

/** Mirrors HMS.Modules.Pharmacy.Contracts.StockLedgerListQuery. */
export interface StockLedgerListQuery extends PagedQuery {
  productId?: string;
  productBatchId?: string;
  /** Limits the list to one store. Omitted, every store is included. */
  storeId?: string;
  transactionType?: TransactionType;
  patientId?: string;
  fromDate?: string;
  toDate?: string;
}
