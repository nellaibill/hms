import type { PagedQuery } from '../../types';

/** Mirrors HMS.Modules.Pharmacy.Contracts.GoodsReceiptLineRequest. Quantities are packs; rates are per pack. */
export interface GoodsReceiptLineRequest {
  productId: string;
  batchNo: string;
  /** ISO date (yyyy-MM-dd). */
  expiryDate: string;
  manufactureDate?: string | null;
  quantity: number;
  freeQuantity: number;
  /** Before tax. */
  purchaseRate: number;
  mrp: number;
  sellingRate?: number | null;
  discountPercent: number;
  /** Omitted, the product's own GST rate applies. */
  gstRatePercent?: number | null;
}

/** Mirrors HMS.Modules.Pharmacy.Contracts.CreateGoodsReceiptRequest. */
export interface CreateGoodsReceiptRequest {
  storeId?: string;
  supplierId: string;
  supplierInvoiceNo: string;
  supplierInvoiceDate?: string | null;
  receivedDate?: string | null;
  isInterState: boolean;
  purchaseOrderId?: string | null;
  otherCharges: number;
  remarks?: string;
  lines: GoodsReceiptLineRequest[];
}

export interface GoodsReceiptLineResponse {
  id: string;
  lineNo: number;
  productId: string;
  productName: string;
  hsnCode?: string | null;
  productBatchId: string;
  batchNo: string;
  expiryDate: string;
  unitsPerPack: number;
  quantity: number;
  freeQuantity: number;
  purchaseRate: number;
  mrp: number;
  sellingRate?: number | null;
  discountPercent: number;
  gstRatePercent: number;
  taxableAmount: number;
  taxAmount: number;
  lineTotal: number;
  receivedUnits: number;
  returnedUnits: number;
}

/** Mirrors HMS.Modules.Pharmacy.Contracts.GoodsReceiptResponse. `lines` is empty on list responses. */
export interface GoodsReceiptResponse {
  id: string;
  receiptNo: string;
  storeId: string;
  storeName: string;
  supplierId: string;
  supplierName: string;
  supplierInvoiceNo: string;
  supplierInvoiceDate?: string | null;
  receivedDate: string;
  isInterState: boolean;
  purchaseOrderId?: string | null;
  grossAmount: number;
  discountAmount: number;
  taxableAmount: number;
  taxAmount: number;
  otherCharges: number;
  roundOff: number;
  netAmount: number;
  remarks?: string | null;
  lineCount: number;
  createdAt: string;
  lines: GoodsReceiptLineResponse[];
}

export interface GoodsReceiptListQuery extends PagedQuery {
  supplierId?: string;
  storeId?: string;
  fromDate?: string;
  toDate?: string;
}
