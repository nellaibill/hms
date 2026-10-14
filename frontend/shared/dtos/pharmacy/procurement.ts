import type { PurchaseOrderStatus, PurchaseReturnReason, StockStatus, SupplierEntryType, SupplierPaymentMethod } from '../../enums';
import type { PagedQuery } from '../../types';

export interface PurchaseOrderLineRequest {
  productId: string;
  /** Packs. */
  quantity: number;
  /** Per pack, before tax. Omitted, the product's last cost is used. */
  expectedRate?: number;
}

/** Mirrors HMS.Modules.Pharmacy.Contracts.SavePurchaseOrderRequest. */
export interface SavePurchaseOrderRequest {
  storeId?: string;
  supplierId: string;
  expectedDate?: string | null;
  remarks?: string;
  lines: PurchaseOrderLineRequest[];
  /** Send it to the supplier straight away rather than leaving it a draft. */
  placeOrder: boolean;
}

export interface PurchaseOrderLineResponse {
  id: string;
  lineNo: number;
  productId: string;
  productName: string;
  quantity: number;
  receivedQuantity: number;
  pendingQuantity: number;
  expectedRate: number;
  gstRatePercent: number;
}

/** Mirrors HMS.Modules.Pharmacy.Contracts.PurchaseOrderResponse. `lines` is empty on list responses. */
export interface PurchaseOrderResponse {
  id: string;
  orderNo: string;
  storeId: string;
  storeName: string;
  supplierId: string;
  supplierName: string;
  orderDate: string;
  expectedDate?: string | null;
  status: PurchaseOrderStatus;
  estimatedAmount: number;
  remarks?: string | null;
  lineCount: number;
  createdAt: string;
  lines: PurchaseOrderLineResponse[];
}

export interface PurchaseOrderListQuery extends PagedQuery {
  supplierId?: string;
  status?: PurchaseOrderStatus;
  openOnly?: boolean;
}

/** A product that needs buying, with a suggested quantity in packs. */
export interface ReorderSuggestionResponse {
  productId: string;
  productName: string;
  genericName?: string | null;
  unitsPerPack: number;
  availableQuantity: number;
  reorderLevel: number;
  status: StockStatus;
  /** Packs on open purchase orders, not yet received. */
  onOrderQuantity: number;
  suggestedQuantity: number;
  lastSupplierId?: string | null;
  lastSupplierName?: string | null;
  /** Per pack. Null for a user who may not see cost. */
  lastPurchaseRate?: number | null;
  gstRatePercent: number;
}

export interface PurchaseReturnLineRequest {
  productId: string;
  productBatchId: string;
  /** Units. */
  quantity: number;
  /** Per unit, before tax. Omitted, the batch's own purchase rate is used. */
  unitRate?: number;
}

export interface CreatePurchaseReturnRequest {
  storeId?: string;
  supplierId: string;
  returnDate?: string | null;
  reason: PurchaseReturnReason;
  creditNoteNo?: string;
  remarks?: string;
  lines: PurchaseReturnLineRequest[];
}

export interface PurchaseReturnLineResponse {
  id: string;
  lineNo: number;
  productId: string;
  productName: string;
  productBatchId: string;
  batchNo: string;
  expiryDate: string;
  quantity: number;
  unitRate: number;
  gstRatePercent: number;
  taxableAmount: number;
  taxAmount: number;
  lineTotal: number;
}

export interface PurchaseReturnResponse {
  id: string;
  returnNo: string;
  storeId: string;
  storeName: string;
  supplierId: string;
  supplierName: string;
  returnDate: string;
  reason: PurchaseReturnReason;
  creditNoteNo?: string | null;
  taxableAmount: number;
  taxAmount: number;
  roundOff: number;
  netAmount: number;
  remarks?: string | null;
  lineCount: number;
  createdAt: string;
  lines: PurchaseReturnLineResponse[];
}

export interface PurchaseReturnListQuery extends PagedQuery {
  supplierId?: string;
  fromDate?: string;
  toDate?: string;
}

/** A batch the store holds that can be sent back — expired ones included. */
export interface ReturnableBatchResponse {
  productId: string;
  productName: string;
  productBatchId: string;
  batchNo: string;
  expiryDate: string;
  quantityOnHand: number;
  unitsPerPack: number;
  /** Per unit, before tax. Null for a user who may not see cost. */
  unitRate?: number | null;
  gstRatePercent: number;
}

export interface CreateSupplierPaymentRequest {
  paymentDate?: string | null;
  amount: number;
  method: SupplierPaymentMethod;
  referenceNo?: string;
  remarks?: string;
}

export interface SupplierPaymentResponse {
  id: string;
  paymentNo: string;
  supplierId: string;
  supplierName: string;
  paymentDate: string;
  amount: number;
  method: SupplierPaymentMethod;
  referenceNo?: string | null;
  remarks?: string | null;
  createdAt: string;
}

/** Where a supplier's account stands. */
export interface SupplierBalanceResponse {
  supplierId: string;
  supplierName: string;
  purchases: number;
  returns: number;
  payments: number;
  /** Negative means the supplier owes the pharmacy. */
  outstanding: number;
  lastPurchaseDate?: string | null;
}

export interface SupplierStatementEntry {
  date: string;
  type: SupplierEntryType;
  documentId: string;
  documentNo: string;
  reference?: string | null;
  /** Adds to what is owed (a purchase). */
  credit: number;
  /** Takes off what is owed (a return, a payment). */
  debit: number;
  balance: number;
}

export interface SupplierStatementResponse {
  supplierId: string;
  supplierName: string;
  fromDate?: string | null;
  toDate?: string | null;
  openingBalance: number;
  closingBalance: number;
  entries: SupplierStatementEntry[];
}
