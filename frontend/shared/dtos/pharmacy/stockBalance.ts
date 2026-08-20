import type { PagedQuery } from '../../types';

/**
 * Mirrors HMS.Modules.Pharmacy.Contracts.StockBalanceResponse — denormalizes
 * ProductName/BatchNo/ExpiryDate so list consumers don't need extra round-trips —
 * same pattern as IPD's AdmissionResponse.
 */
export interface StockBalanceResponse {
  id: string;
  productId: string;
  productName: string;
  productBatchId: string;
  batchNo: string;
  expiryDate: string;
  quantityOnHand: number;
  createdAt: string;
  updatedAt?: string | null;
}

/** Mirrors HMS.Modules.Pharmacy.Contracts.StockBalanceListQuery. */
export interface StockBalanceListQuery extends PagedQuery {
  productId?: string;
}
