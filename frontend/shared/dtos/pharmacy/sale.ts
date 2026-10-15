import type { DosageForm, DrugSchedule, SalePaymentMethod } from '../../enums';
import type { PagedQuery } from '../../types';

/** One batch of an item the counter can sell right now. Prices are per unit, tax included. */
export interface CounterBatchResponse {
  productBatchId: string;
  batchNo: string;
  expiryDate: string;
  quantityOnHand: number;
  unitPrice: number;
  unitMrp: number;
}

/** Mirrors HMS.Modules.Pharmacy.Contracts.CounterItemResponse — a product with its sellable
 * stock and prices in one store, earliest-expiring batch first. */
export interface CounterItemResponse {
  productId: string;
  productName: string;
  genericName?: string | null;
  strength?: string | null;
  dosageForm?: DosageForm | null;
  schedule: DrugSchedule;
  rackLocation?: string | null;
  hsnCode?: string | null;
  unitsPerPack: number;
  gstRatePercent: number;
  availableQuantity: number;
  unitPrice: number;
  batches: CounterBatchResponse[];
}

export interface SaleLineRequest {
  productId: string;
  /** Omitted, the earliest-expiring stock is used. */
  productBatchId?: string;
  /** Units (tablets, not strips). */
  quantity: number;
  /** Omitted, the bill's discount applies. */
  discountPercent?: number;
}

export interface SalePaymentRequest {
  method: SalePaymentMethod;
  /** As tendered — cash may exceed what is due. */
  amount: number;
  referenceNo?: string;
}

/** Mirrors HMS.Modules.Pharmacy.Contracts.CreateSaleRequest. */
export interface CreateSaleRequest {
  storeId?: string;
  /** Omitted for a walk-in sale. */
  patientId?: string;
  customerName?: string;
  customerPhone?: string;
  doctorName?: string;
  admissionId?: string;
  prescriptionId?: string;
  indentId?: string;
  discountPercent: number;
  lines: SaleLineRequest[];
  payments: SalePaymentRequest[];
  allowCredit: boolean;
  chargeToAdmission: boolean;
  heldBillId?: string;
  remarks?: string;
}

export interface SaleLineResponse {
  id: string;
  lineNo: number;
  productId: string;
  productName: string;
  genericName?: string | null;
  hsnCode?: string | null;
  schedule: DrugSchedule;
  productBatchId: string;
  batchNo: string;
  expiryDate: string;
  quantity: number;
  unitPrice: number;
  discountPercent: number;
  gstRatePercent: number;
  grossAmount: number;
  discountAmount: number;
  taxableAmount: number;
  taxAmount: number;
  lineTotal: number;
  returnedQuantity: number;
}

export interface SalePaymentResponse {
  id: string;
  method: SalePaymentMethod;
  amount: number;
  referenceNo?: string | null;
  paidAt: string;
}

/** Mirrors HMS.Modules.Pharmacy.Contracts.SaleResponse. `lines`/`payments` are empty on list responses. */
export interface SaleResponse {
  id: string;
  billNo: string;
  storeId: string;
  storeName: string;
  saleDate: string;
  patientId?: string | null;
  customerName: string;
  patientUhid?: string | null;
  customerPhone?: string | null;
  doctorName?: string | null;
  admissionId?: string | null;
  prescriptionId?: string | null;
  indentId?: string | null;
  grossAmount: number;
  discountAmount: number;
  taxableAmount: number;
  taxAmount: number;
  roundOff: number;
  netAmount: number;
  paidAmount: number;
  refundedAmount: number;
  returnedAmount: number;
  balanceDue: number;
  chargedToAdmission: boolean;
  /** Cash handed back when the bill was made. Only on the create response. */
  changeGiven: number;
  remarks?: string | null;
  lineCount: number;
  createdAt: string;
  lines: SaleLineResponse[];
  payments: SalePaymentResponse[];
}

export interface SaleListQuery extends PagedQuery {
  storeId?: string;
  patientId?: string;
  fromDate?: string;
  toDate?: string;
  hasDue?: boolean;
}

export interface CollectSalePaymentRequest {
  method: SalePaymentMethod;
  amount: number;
  referenceNo?: string;
}

export interface SaleReturnLineRequest {
  saleLineId: string;
  quantity: number;
}

export interface CreateSaleReturnRequest {
  lines: SaleReturnLineRequest[];
  refundMethod: SalePaymentMethod;
  /** False when the goods are not fit to sell again. */
  restock: boolean;
  reason?: string;
}

export interface SaleReturnLineResponse {
  id: string;
  saleLineId: string;
  productId: string;
  productName: string;
  batchNo: string;
  quantity: number;
  unitPrice: number;
  gstRatePercent: number;
  taxableAmount: number;
  taxAmount: number;
  lineTotal: number;
}

export interface SaleReturnResponse {
  id: string;
  returnNo: string;
  saleId: string;
  billNo: string;
  storeId: string;
  returnDate: string;
  customerName: string;
  taxableAmount: number;
  taxAmount: number;
  returnAmount: number;
  refundAmount: number;
  refundMethod: SalePaymentMethod;
  restocked: boolean;
  reason?: string | null;
  lines: SaleReturnLineResponse[];
}

/** A patient's pharmacy bills with their lines, and the returns against them (both newest first). */
export interface PatientPharmacyBillsResponse {
  bills: SaleResponse[];
  returns: SaleReturnResponse[];
}

export interface SaleReturnListQuery extends PagedQuery {
  storeId?: string;
  saleId?: string;
  fromDate?: string;
  toDate?: string;
}

export interface HoldBillRequest {
  storeId?: string;
  label: string;
  itemCount: number;
  amount: number;
  /** The counter's cart, serialized by the screen that reads it back. */
  payload: string;
}

export interface HeldBillResponse {
  id: string;
  storeId: string;
  label: string;
  itemCount: number;
  amount: number;
  payload: string;
  createdAt: string;
}
