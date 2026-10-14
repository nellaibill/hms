import type { StockStatus, StockSummaryFilter } from '../../enums';
import type { PagedQuery } from '../../types';

/**
 * Mirrors HMS.Modules.Pharmacy.Contracts.StockSummaryResponse — one row per product, its
 * on-hand stock across every batch split by whether it can actually be dispensed.
 */
export interface StockSummaryResponse {
  productId: string;
  productName: string;
  sku: string;
  genericName?: string | null;
  reorderLevel: number;
  /** On hand in active, unexpired batches — the only stock a dispense can draw on. */
  availableQuantity: number;
  /** The part of availableQuantity expiring within the expiring-soon window. */
  expiringSoonQuantity: number;
  expiredQuantity: number;
  /** On hand in unexpired batches that have been marked inactive. */
  onHoldQuantity: number;
  batchCount: number;
  nearestExpiry?: string | null;
  status: StockStatus;
}

/** Mirrors HMS.Modules.Pharmacy.Contracts.StockSummaryListQuery. */
export interface StockSummaryListQuery extends PagedQuery {
  filter?: StockSummaryFilter;
}

/**
 * Mirrors HMS.Modules.Pharmacy.Contracts.AvailableBatchResponse — a batch a dispense can draw
 * on right now, returned earliest-expiry-first.
 */
export interface AvailableBatchResponse {
  productBatchId: string;
  batchNo: string;
  expiryDate: string;
  quantityOnHand: number;
  isExpiringSoon: boolean;
}
