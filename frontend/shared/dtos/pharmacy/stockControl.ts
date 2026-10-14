import type { AdjustmentReason, DrugSchedule, StockCountStatus } from '../../enums';
import type { PagedQuery } from '../../types';

export interface StockAdjustmentLineRequest {
  productId: string;
  productBatchId: string;
  /** Units, signed: positive adds to stock, negative takes away. */
  quantity: number;
}

/** Mirrors HMS.Modules.Pharmacy.Contracts.CreateStockAdjustmentRequest. */
export interface CreateStockAdjustmentRequest {
  storeId?: string;
  reason: AdjustmentReason;
  remarks?: string;
  lines: StockAdjustmentLineRequest[];
}

export interface StockAdjustmentLineResponse {
  id: string;
  lineNo: number;
  productId: string;
  productName: string;
  productBatchId: string;
  batchNo: string;
  expiryDate: string;
  quantity: number;
  /** At cost. Null for a user who may not see cost. */
  value?: number | null;
}

export interface StockAdjustmentResponse {
  id: string;
  adjustmentNo: string;
  storeId: string;
  storeName: string;
  adjustmentDate: string;
  reason: AdjustmentReason;
  stockCountId?: string | null;
  remarks?: string | null;
  lineCount: number;
  /** Net value at cost; negative is a loss. */
  totalValue?: number | null;
  createdAt: string;
  lines: StockAdjustmentLineResponse[];
}

export interface StockAdjustmentListQuery extends PagedQuery {
  storeId?: string;
  reason?: AdjustmentReason;
  fromDate?: string;
  toDate?: string;
}

export interface StockTransferLineRequest {
  productId: string;
  productBatchId: string;
  /** Units. */
  quantity: number;
}

/** Mirrors HMS.Modules.Pharmacy.Contracts.CreateStockTransferRequest. */
export interface CreateStockTransferRequest {
  fromStoreId: string;
  toStoreId: string;
  remarks?: string;
  lines: StockTransferLineRequest[];
}

export interface StockTransferLineResponse {
  id: string;
  lineNo: number;
  productId: string;
  productName: string;
  productBatchId: string;
  batchNo: string;
  expiryDate: string;
  quantity: number;
}

export interface StockTransferResponse {
  id: string;
  transferNo: string;
  fromStoreId: string;
  fromStoreName: string;
  toStoreId: string;
  toStoreName: string;
  transferDate: string;
  remarks?: string | null;
  lineCount: number;
  createdAt: string;
  lines: StockTransferLineResponse[];
}

export interface StockTransferListQuery extends PagedQuery {
  storeId?: string;
  fromDate?: string;
  toDate?: string;
}

export interface StockCountLineResponse {
  id: string;
  productId: string;
  productName: string;
  rackLocation?: string | null;
  productBatchId: string;
  batchNo: string;
  expiryDate: string;
  /** What the books held when the count started. */
  bookQuantity: number;
  /** What the books hold now. */
  currentQuantity: number;
  countedQuantity?: number | null;
  /** Counted less current book. */
  variance?: number | null;
}

/** Mirrors HMS.Modules.Pharmacy.Contracts.StockCountResponse. `lines` is empty on list responses. */
export interface StockCountResponse {
  id: string;
  countNo: string;
  storeId: string;
  storeName: string;
  startedAt: string;
  status: StockCountStatus;
  postedAt?: string | null;
  adjustmentId?: string | null;
  remarks?: string | null;
  lineCount: number;
  countedLines: number;
  varianceLines: number;
  lines: StockCountLineResponse[];
}

export interface StockCountListQuery extends PagedQuery {
  storeId?: string;
  status?: StockCountStatus;
}

/** A batch on the shelf that has expired or will within the window. */
export interface ExpiringBatchResponse {
  storeId: string;
  storeName: string;
  productId: string;
  productName: string;
  rackLocation?: string | null;
  productBatchId: string;
  batchNo: string;
  expiryDate: string;
  /** Negative once it has passed. */
  daysLeft: number;
  quantityOnHand: number;
  value?: number | null;
  supplierId?: string | null;
  supplierName?: string | null;
}

/** One sale of a scheduled drug, as the statutory register records it. */
export interface ScheduleRegisterEntry {
  saleDate: string;
  saleId: string;
  billNo: string;
  patientName: string;
  patientUhid?: string | null;
  patientPhone?: string | null;
  doctorName?: string | null;
  productName: string;
  genericName?: string | null;
  schedule: DrugSchedule;
  batchNo: string;
  expiryDate: string;
  quantity: number;
  returnedQuantity: number;
}
