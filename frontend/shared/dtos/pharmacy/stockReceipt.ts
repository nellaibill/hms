import type { PagedQuery } from '../../types';

/** Mirrors HMS.Modules.Pharmacy.Contracts.CreateStockReceiptRequest. */
export interface CreateStockReceiptRequest {
  productId: string;
  productBatchId: string;
  quantity: number;
  remarks?: string;
}

/**
 * Mirrors HMS.Modules.Pharmacy.Contracts.StockReceiptResponse — denormalizes
 * ProductName/BatchNo so list/detail consumers don't need extra round-trips — same
 * pattern as IPD's AdmissionResponse.
 */
export interface StockReceiptResponse {
  id: string;
  productId: string;
  productName: string;
  productBatchId: string;
  batchNo: string;
  quantity: number;
  balanceAfter: number;
  transactionDate: string;
  remarks?: string;
  createdAt: string;
}

/** Mirrors HMS.Modules.Pharmacy.Contracts.StockReceiptListQuery. */
export interface StockReceiptListQuery extends PagedQuery {
  productId?: string;
}
