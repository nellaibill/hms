import type { BillingType, InvoicePaymentStatus, PaymentMethod } from '../../enums';

/** Mirrors HMS.Modules.Billing.Contracts.CreateInvoiceLineItemRequest. */
export interface CreateInvoiceLineItemRequest {
  billingType: BillingType;
  departmentId?: string | null;
  consultantId?: string | null;
  serviceId?: string | null;
  /** App-level reference into Masters' DiagnosticPackage — set for a package line, null for a
   * plain service line. */
  packageId?: string | null;
  quantity: number;
  unitPrice: number;
  discount: number;
  discountApproved: boolean;
  discountApprovedBy?: string | null;
}

/** Mirrors HMS.Modules.Billing.Contracts.CreateInvoicePaymentRequest — one payment-method row. */
export interface CreateInvoicePaymentRequest {
  method: PaymentMethod;
  /** How much was tendered via this method. */
  amount: number;
  referenceNumber?: string | null;
}

/** Mirrors HMS.Modules.Billing.Contracts.CreateInvoiceRequest. */
export interface CreateInvoiceRequest {
  patientId: string;
  visitId: string;
  patientName: string;
  patientUhid: string;
  items: CreateInvoiceLineItemRequest[];
  /** Optional — when supplied, the whole invoice is paid in full at creation time. Null/undefined/
   * empty saves Pending, exactly like before this field existed. One row pays in full with a
   * single method (may tender more than the net total, with the excess treated as change and
   * not stored); more than one row is a split payment across methods and must add up to exactly
   * the net total. See Contracts/InvoiceContracts.cs. */
  payments?: CreateInvoicePaymentRequest[] | null;
}

/** Mirrors HMS.Modules.Billing.Contracts.RecordPaymentRequest. */
export interface RecordPaymentRequest {
  method: PaymentMethod;
}

/** Mirrors HMS.Modules.Billing.Contracts.VoidInvoiceRequest. */
export interface VoidInvoiceRequest {
  reason: string;
}

/** Mirrors HMS.Modules.Billing.Contracts.InvoiceLineItemResponse. */
export interface InvoiceLineItemResponse {
  id: string;
  billingType: BillingType;
  departmentId?: string | null;
  consultantId?: string | null;
  serviceId?: string | null;
  packageId?: string | null;
  quantity: number;
  unitPrice: number;
  discount: number;
  discountApproved: boolean;
  discountApprovedBy?: string | null;
  paymentStatus: InvoicePaymentStatus;
  total: number;
}

/** Mirrors HMS.Modules.Billing.Contracts.InvoiceResponse. */
export interface InvoiceResponse {
  id: string;
  invoiceNumber: string;
  patientId: string;
  visitId: string;
  patientName: string;
  patientUhid: string;
  createdAt: string;
  items: InvoiceLineItemResponse[];
  grossAmount: number;
  totalDiscount: number;
  netAmount: number;
  paymentStatus: InvoicePaymentStatus;
  isVoided: boolean;
  voidedAt?: string | null;
  voidReason?: string | null;
}

/** Mirrors HMS.Modules.Billing.Contracts.InvoiceListQuery. */
export interface InvoiceListQuery {
  page?: number;
  pageSize?: number;
  sort?: string;
  search?: string;
  paymentStatus?: InvoicePaymentStatus;
}
