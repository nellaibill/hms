import type { PurchaseReturnReason, SalePaymentMethod } from '../../enums/pharmacy';

// Pharmacy reports and dashboard. Every figure is summed from the documents; cost, margin and
// stock value are null for a user who may not see what the pharmacy pays.

/** One hospital-local day of counter business. */
export interface DailySalesRow {
  date: string;
  bills: number;
  grossAmount: number;
  discountAmount: number;
  taxableAmount: number;
  taxAmount: number;
  salesAmount: number;
  returns: number;
  returnAmount: number;
  netSales: number;
  /** Money taken less money refunded on the day — credit bills are sold today, paid later. */
  collected: number;
  cost?: number | null;
  margin?: number | null;
}

export interface SalesRegisterRow {
  saleId: string;
  billNo: string;
  saleDate: string;
  customerName: string;
  patientUhid?: string | null;
  /** "IP", "OP" or "Walk-in". */
  kind: string;
  doctorName?: string | null;
  grossAmount: number;
  discountAmount: number;
  taxableAmount: number;
  taxAmount: number;
  roundOff: number;
  netAmount: number;
  paidAmount: number;
  returnedAmount: number;
  balanceDue: number;
  chargedToAdmission: boolean;
}

export interface SalesReturnRegisterRow {
  returnId: string;
  returnNo: string;
  saleId: string;
  billNo: string;
  returnDate: string;
  customerName: string;
  taxableAmount: number;
  taxAmount: number;
  returnAmount: number;
  refundAmount: number;
  refundMethod: SalePaymentMethod;
}

export interface SalesRegisterResponse {
  bills: SalesRegisterRow[];
  returns: SalesReturnRegisterRow[];
}

/** What one product sold in a period, net of what came back. */
export interface ProductSalesRow {
  productId: string;
  productName: string;
  bills: number;
  quantitySold: number;
  quantityReturned: number;
  netQuantity: number;
  taxableAmount: number;
  taxAmount: number;
  amount: number;
  cost?: number | null;
  margin?: number | null;
  marginPercent?: number | null;
}

export interface CollectionRow {
  date: string;
  method: SalePaymentMethod;
  received: number;
  refunded: number;
  net: number;
}

export interface GstRateRow {
  hsnCode?: string | null;
  gstRatePercent: number;
  taxableAmount: number;
  cgst: number;
  sgst: number;
  igst: number;
  taxAmount: number;
}

export interface GstSummaryResponse {
  sales: GstRateRow[];
  saleReturns: GstRateRow[];
  purchases: GstRateRow[];
  purchaseReturns: GstRateRow[];
  outputTax: number;
  /** Null, with the purchase tables empty, without the right to see purchases. */
  inputTax?: number | null;
}

export interface PurchaseRegisterRow {
  receiptId: string;
  receiptNo: string;
  receivedDate: string;
  supplierName: string;
  supplierInvoiceNo: string;
  supplierInvoiceDate?: string | null;
  isInterState: boolean;
  grossAmount: number;
  discountAmount: number;
  taxableAmount: number;
  taxAmount: number;
  otherCharges: number;
  roundOff: number;
  netAmount: number;
}

export interface PurchaseReturnRegisterRow {
  returnId: string;
  returnNo: string;
  returnDate: string;
  supplierName: string;
  reason: PurchaseReturnReason;
  creditNoteNo?: string | null;
  taxableAmount: number;
  taxAmount: number;
  netAmount: number;
}

export interface PurchaseRegisterResponse {
  purchases: PurchaseRegisterRow[];
  returns: PurchaseReturnRegisterRow[];
}

export interface StockValuationRow {
  productId: string;
  productName: string;
  genericName?: string | null;
  rackLocation?: string | null;
  batches: number;
  quantity: number;
  costValue?: number | null;
  mrpValue: number;
  expiredQuantity: number;
  expiredCostValue?: number | null;
}

export interface StockValuationResponse {
  rows: StockValuationRow[];
  totalQuantity: number;
  totalCostValue?: number | null;
  totalMrpValue: number;
  expiredCostValue?: number | null;
}

export interface NonMovingStockRow {
  productId: string;
  productName: string;
  rackLocation?: string | null;
  quantity: number;
  costValue?: number | null;
  /** Null when the product has never been billed. */
  lastSoldOn?: string | null;
  stockedOn?: string | null;
  /** Days since the last sale — or, never sold, since it was stocked. */
  idleDays: number;
  nearestExpiry?: string | null;
}

export interface SalesFigures {
  bills: number;
  salesAmount: number;
  returnAmount: number;
  netSales: number;
  collected: number;
  margin?: number | null;
}

export interface PaymentModeTotal {
  method: SalePaymentMethod;
  amount: number;
}

export interface StockAlertFigures {
  productsInStock: number;
  lowStock: number;
  outOfStock: number;
  expiredBatches: number;
  expiringSoonBatches: number;
  expiryAlertDays: number;
  stockValue?: number | null;
  expiredValue?: number | null;
}

export interface PendingWorkFigures {
  pendingIndents: number;
  openPurchaseOrders: number;
  billsWithDues: number;
  customerDues: number;
  supplierPayable?: number | null;
}

/** The pharmacy at a glance. */
export interface PharmacyDashboardResponse {
  date: string;
  today: SalesFigures;
  yesterday: SalesFigures;
  monthToDate: SalesFigures;
  trend: DailySalesRow[];
  todayCollections: PaymentModeTotal[];
  stock: StockAlertFigures;
  pending: PendingWorkFigures;
  topProducts: ProductSalesRow[];
}

/** The filters every period report takes. */
export interface PharmacyReportPeriod {
  fromDate: string;
  toDate: string;
  storeId?: string;
}
